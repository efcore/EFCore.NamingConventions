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
        IIndexAddedConvention, IEntityTypeBaseTypeChangedConvention, IModelFinalizingConvention
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

                if (entityType.GetViewNameConfigurationSource() == ConfigurationSource.Convention)
                {
                    entityTypeBuilder.ToView(_namingNameRewriter.RewriteName(entityType.GetViewName()), entityType.GetViewSchema());
                }
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

            propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(property.GetColumnBaseName()));

            foreach (var storeObjectType in _storeObjectTypes)
            {
                var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
                if (identifier is null)
                    continue;

                if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                {
                    propertyBuilder.HasColumnName(
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
            }
        }

        public void ProcessEntityTypeAnnotationChanged(IConventionEntityTypeBuilder entityTypeBuilder, string name,
            IConventionAnnotation annotation, IConventionAnnotation oldAnnotation, IConventionContext<IConventionAnnotation> context)
        {
            if (name != RelationalAnnotationNames.TableName)
            {
                return;
            }

            // The table's name is changing - rewrite keys, index names
            if (entityTypeBuilder.Metadata.FindPrimaryKey() is IConventionKey primaryKey)
            {
                primaryKey.Builder.HasName(_namingNameRewriter.RewriteName(primaryKey.GetDefaultName()));
            }

            foreach (var foreignKey in entityTypeBuilder.Metadata.GetForeignKeys())
            {
                foreignKey.Builder.HasConstraintName(_namingNameRewriter.RewriteName(foreignKey.GetDefaultName()));
            }

            foreach (var index in entityTypeBuilder.Metadata.GetIndexes())
            {
                index.Builder.HasDatabaseName(_namingNameRewriter.RewriteName(index.GetDefaultDatabaseName()));
            }

            if (oldAnnotation?.Value is null &&
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
            => relationshipBuilder.HasConstraintName(_namingNameRewriter.RewriteName(relationshipBuilder.Metadata.GetDefaultName()));

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
            => keyBuilder.HasName(_namingNameRewriter.RewriteName(keyBuilder.Metadata.GetDefaultName()));

        public void ProcessIndexAdded(
            IConventionIndexBuilder indexBuilder,
            IConventionContext<IConventionIndexBuilder> context)
            => indexBuilder.HasDatabaseName(_namingNameRewriter.RewriteName(indexBuilder.Metadata.GetDefaultDatabaseName()));

        /// <summary>
        /// EF Core's <see cref="SharedTableConvention" /> runs at model finalization time, and adds entity type prefixes to
        /// clashing columns. These prefixes also needs to be rewritten by us, so we run after that convention to do that.
        /// </summary>
        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.GetColumnBaseName();
                    if (columnName.StartsWith(entityType.ShortName() + '_', StringComparison.Ordinal))
                    {
                        property.Builder.HasColumnName(
                            _namingNameRewriter.RewriteName(entityType.ShortName()) +
                            columnName.Substring(entityType.ShortName().Length));
                    }

                    foreach (var storeObjectType in _storeObjectTypes)
                    {
                        var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
                        if (identifier is null)
                            continue;

                        if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention)
                        {
                            columnName = property.GetColumnName(identifier.Value);
                            if (columnName.StartsWith(entityType.ShortName() + '_', StringComparison.Ordinal))
                            {
                                property.Builder.HasColumnName(
                                    _namingNameRewriter.RewriteName(entityType.ShortName()) +
                                    columnName.Substring(entityType.ShortName().Length));
                            }
                        }
                    }
                }
            }
        }
    }
}
