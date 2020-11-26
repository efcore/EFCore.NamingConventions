using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EFCore.NamingConventions.Internal
{
    public class NameRewritingConvention :
        IEntityTypeAddedConvention, IEntityTypeAnnotationChangedConvention, IPropertyAddedConvention,
        IForeignKeyOwnershipChangedConvention, IKeyAddedConvention, IForeignKeyAddedConvention,
        IIndexAddedConvention, IEntityTypeBaseTypeChangedConvention
    {
        private static readonly StoreObjectType[] _storeObjectTypes
            = { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function, StoreObjectType.SqlQuery};

        private readonly INameRewriter _namingNameRewriter;

        public NameRewritingConvention(INameRewriter nameRewriter) => _namingNameRewriter = nameRewriter;

        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            // Note that the base type is null when the entity type is first added - a base type only gets added later
            // (see ProcessEntityTypeBaseTypeChanged). But we still have this check for safety.
            if (entityType.BaseType is null)
            {
                entityTypeBuilder.ToTable(_namingNameRewriter.RewriteName(entityType.GetTableName()), entityType.GetSchema());
            }
        }

        public void ProcessEntityTypeBaseTypeChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionEntityType newBaseType,
            IConventionEntityType oldBaseType,
            IConventionContext<IConventionEntityType> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            if (newBaseType is null)
            {
                // The entity is getting removed from a hierarchy. Set the (rewritten) TableName.
                entityTypeBuilder.ToTable(_namingNameRewriter.RewriteName(entityType.GetTableName()), entityType.GetSchema());
            }
            else
            {
                // The entity is getting a new base type (e.g. joining a hierarchy).
                // If this is TPH, we remove the previously rewritten TableName (and non-rewritten Schema) which we set when the
                // entity type was first added to the model (see ProcessEntityTypeAdded).
                // If this is TPT, TableName and Schema are set explicitly, so the following will be ignored.
                entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.Schema);
            }
        }

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder,
            IConventionContext<IConventionPropertyBuilder> context)
        {
            var entityType = propertyBuilder.Metadata.DeclaringEntityType;
            var property = propertyBuilder.Metadata;

            property.SetColumnName(_namingNameRewriter.RewriteName(property.GetColumnBaseName()));

            foreach (var storeObjectType in _storeObjectTypes)
            {
                var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
                if (identifier is null)
                    continue;

                if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                {
                    property.SetColumnName(
                        _namingNameRewriter.RewriteName(property.GetColumnName(identifier.Value)), identifier.Value);
                }
            }
        }

        public void ProcessForeignKeyOwnershipChanged(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
        {
            var foreignKey = relationshipBuilder.Metadata;
            var ownedEntityType = foreignKey.DeclaringEntityType;

            if (foreignKey.IsOwnership &&
                ownedEntityType.GetTableNameConfigurationSource() != ConfigurationSource.Explicit)
            {
                // An entity type is becoming owned - this is complicated.

                // Reset the table name which we've set when the entity type was added
                // If table splitting was configured by explicitly setting the table name, the following
                // does nothing.
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

                // Also need to reset all primary key properties
                foreach (var keyProperty in ownedEntityType.FindPrimaryKey().Properties)
                {
                    keyProperty.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);

                    foreach (var storeObjectType in _storeObjectTypes)
                    {
                        var identifier = StoreObjectIdentifier.Create(ownedEntityType, storeObjectType);
                        if (identifier is null)
                            continue;

                        if (keyProperty.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                        {
#pragma warning disable EF1001
                            // TODO: Using internal APIs to remove the override
                            var tableOverrides = (IDictionary<StoreObjectIdentifier, RelationalPropertyOverrides>)
                                keyProperty[RelationalAnnotationNames.RelationalOverrides];
                            tableOverrides.Remove(identifier.Value);
#pragma warning restore EF1001
                        }
                    }
                }

                // Finally, we need to apply the entity type prefix to all the owned properties, since
                // the convention that normally does this (SharedTableConvention) runs at model finalization
                // time, and will not overwrite our own rewritten column names.
                foreach (var property in ownedEntityType.GetProperties()
                    .Except(ownedEntityType.FindPrimaryKey().Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    var columnName = property.GetColumnBaseName();
                    var prefix = _namingNameRewriter.RewriteName(ownedEntityType.ShortName());
                    if (!columnName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        columnName = prefix + "_" + columnName;
                    }

                    // TODO: We should uniquify, but we don't know about all the entity types mapped
                    // to this table. SharedTableConvention does its thing during model finalization,
                    // so it has the full list of entities and can uniquify.
                    // columnName = Uniquifier.Uniquify(columnName, properties, maxLength);
                    property.Builder.HasColumnName(columnName);
                }
            }
        }

        public void ProcessEntityTypeAnnotationChanged(IConventionEntityTypeBuilder entityTypeBuilder, string name,
            IConventionAnnotation annotation, IConventionAnnotation oldAnnotation, IConventionContext<IConventionAnnotation> context)
        {
            if (name == RelationalAnnotationNames.TableName &&
                oldAnnotation?.Value is null &&
                annotation?.Value is not null &&
                entityTypeBuilder.Metadata.FindOwnership() is IConventionForeignKey ownership &&
                (string)annotation.Value != ownership.PrincipalEntityType.GetTableName())
            {
                // An owned entity's table is being set explicitly - this is the trigger to undo table splitting (which is the default).

                // When the entity became owned, we prefixed all of its properties - we must now undo that.
                foreach (var property in entityTypeBuilder.Metadata.GetProperties()
                    .Except(entityTypeBuilder.Metadata.FindPrimaryKey().Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    property.Builder.HasColumnName(_namingNameRewriter.RewriteName(property.GetDefaultColumnBaseName()));
                }

                // We previously rewrote the owned entity's primary key name, when the owned entity was still in table splitting.
                // Now that its getting its own table, rewrite the primary key constraint name again.
                if (entityTypeBuilder.Metadata.FindPrimaryKey() is IConventionKey key)
                {
                    key.Builder.HasName(_namingNameRewriter.RewriteName(key.GetDefaultName()));
                }
            }
        }

        public void ProcessForeignKeyAdded(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<IConventionForeignKeyBuilder> context)
            => relationshipBuilder.HasConstraintName(_namingNameRewriter.RewriteName(relationshipBuilder.Metadata.GetConstraintName()));

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
            => keyBuilder.HasName(_namingNameRewriter.RewriteName(keyBuilder.Metadata.GetName()));

        public void ProcessIndexAdded(
            IConventionIndexBuilder indexBuilder,
            IConventionContext<IConventionIndexBuilder> context)
            => indexBuilder.HasDatabaseName(_namingNameRewriter.RewriteName(indexBuilder.Metadata.GetDatabaseName()));
    }
}
