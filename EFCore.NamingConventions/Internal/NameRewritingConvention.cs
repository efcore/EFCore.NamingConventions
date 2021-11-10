using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    public class NameRewritingConvention :
        IEntityTypeAddedConvention,
        IEntityTypeAnnotationChangedConvention,
        IPropertyAddedConvention,
        IForeignKeyOwnershipChangedConvention,
        IKeyAddedConvention,
        IForeignKeyAddedConvention,
        IIndexAddedConvention,
        IEntityTypeBaseTypeChangedConvention,
        IModelFinalizingConvention
    {
        private static readonly StoreObjectType[] _storeObjectTypes
            = { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function, StoreObjectType.SqlQuery };

        private readonly INameRewriter _namingNameRewriter;

        public NameRewritingConvention(INameRewriter nameRewriter)
            => _namingNameRewriter = nameRewriter;

        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionContext<IConventionEntityTypeBuilder> context)
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
            => RewriteColumnName(propertyBuilder);

        public void ProcessForeignKeyOwnershipChanged(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
        {
            var foreignKey = relationshipBuilder.Metadata;
            var ownedEntityType = foreignKey.DeclaringEntityType;

            if (foreignKey.IsOwnership && ownedEntityType.GetTableNameConfigurationSource() != ConfigurationSource.Explicit)
            {
                // An entity type is becoming owned - this is complicated.

                // Reset the table name which we've set when the entity type was added
                // If table splitting was configured by explicitly setting the table name, the following
                // does nothing.
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

                ownedEntityType.FindPrimaryKey()?.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);

                // We've previously set rewritten column names when the entity was originally added (before becoming owned).
                // These need to be rewritten again to include the owner prefix.
                foreach (var property in ownedEntityType.GetProperties())
                {
                    RewriteColumnName(property.Builder);
                }
            }
        }

        public void ProcessEntityTypeAnnotationChanged(
            IConventionEntityTypeBuilder entityTypeBuilder,
            string name,
            IConventionAnnotation annotation,
            IConventionAnnotation oldAnnotation,
            IConventionContext<IConventionAnnotation> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            // If the View/SqlQuery/Function name is being set on the entity type, and its table name is set by convention, then we assume
            // we're the one who set the table name back when the entity type was originally added. We now undo this as the entity type
            // should only be mapped to the View/SqlQuery/Function.
            if (name is RelationalAnnotationNames.ViewName or RelationalAnnotationNames.SqlQuery or RelationalAnnotationNames.FunctionName
                && annotation.Value is not null
                && entityType.GetTableNameConfigurationSource() == ConfigurationSource.Convention)
            {
                entityType.SetTableName(null);
            }

            if (name != RelationalAnnotationNames.TableName
                || StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is not StoreObjectIdentifier tableIdentifier)
            {
                return;
            }

            // The table's name is changing - rewrite keys, index names

            if (entityType.FindPrimaryKey() is IConventionKey primaryKey)
            {
                // We need to rewrite the PK name.
                // However, this isn't yet supported with TPT, see https://github.com/dotnet/efcore/issues/23444.
                // So we need to check if the entity is within a TPT hierarchy, or is an owned entity within a TPT hierarchy.

                if (entityType.FindRowInternalForeignKeys(tableIdentifier).FirstOrDefault() is null
                    && (entityType.BaseType is null || entityType.GetTableName() == entityType.BaseType.GetTableName()))
                {
                    primaryKey.Builder.HasName(_namingNameRewriter.RewriteName(primaryKey.GetDefaultName()));
                }
                else
                {
                    primaryKey.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
                }
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.Builder.HasConstraintName(_namingNameRewriter.RewriteName(foreignKey.GetDefaultName()));
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.Builder.HasDatabaseName(_namingNameRewriter.RewriteName(index.GetDefaultDatabaseName()));
            }

            if (annotation?.Value is not null
                && entityType.FindOwnership() is IConventionForeignKey ownership
                && (string)annotation.Value != ownership.PrincipalEntityType.GetTableName())
            {
                // An owned entity's table is being set explicitly - this is the trigger to undo table splitting (which is the default).

                // When the entity became owned, we prefixed all of its properties - we must now undo that.
                foreach (var property in entityType.GetProperties()
                    .Except(entityType.FindPrimaryKey().Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    RewriteColumnName(property.Builder);
                }

                // We previously rewrote the owned entity's primary key name, when the owned entity was still in table splitting.
                // Now that its getting its own table, rewrite the primary key constraint name again.
                if (entityType.FindPrimaryKey() is IConventionKey key)
                {
                    key.Builder.HasName(_namingNameRewriter.RewriteName(key.GetDefaultName()));
                }
            }
        }

        public void ProcessForeignKeyAdded(
            IConventionForeignKeyBuilder relationshipBuilder,
            IConventionContext<IConventionForeignKeyBuilder> context)
            => relationshipBuilder.HasConstraintName(_namingNameRewriter.RewriteName(relationshipBuilder.Metadata.GetDefaultName()));

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
        {
            var entityType = keyBuilder.Metadata.DeclaringEntityType;

            if (entityType.FindOwnership() is null)
            {
                keyBuilder.HasName(_namingNameRewriter.RewriteName(keyBuilder.Metadata.GetDefaultName()));
            }
        }

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
                            _namingNameRewriter.RewriteName(entityType.ShortName()) + columnName.Substring(entityType.ShortName().Length));
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
                                    _namingNameRewriter.RewriteName(entityType.ShortName())
                                    + columnName.Substring(entityType.ShortName().Length));
                            }
                        }
                    }
                }
            }
        }

        private void RewriteColumnName(IConventionPropertyBuilder propertyBuilder)
        {
            var property = propertyBuilder.Metadata;
            var entityType = property.DeclaringEntityType;

            // Remove any previous setting of the column name we may have done, so we can get the default recalculated below.
            property.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);

            // TODO: The following is a temporary hack. We should probably just always set the relational override below,
            // but https://github.com/dotnet/efcore/pull/23834
            var baseColumnName = StoreObjectIdentifier.Create(property.DeclaringEntityType, StoreObjectType.Table) is { } tableIdentifier
                ? property.GetDefaultColumnName(tableIdentifier)
                : property.GetDefaultColumnBaseName();
            propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(baseColumnName));

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
    }
}
