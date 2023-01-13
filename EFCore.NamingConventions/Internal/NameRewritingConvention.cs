using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal;

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
        if (entityType.BaseType is null && !entityType.ClrType.IsAbstract)
        {
            if (entityType.GetTableName() is { } tableName)
            {
                entityTypeBuilder.ToTable(_namingNameRewriter.RewriteName(tableName), entityType.GetSchema());
            }

            if (entityType.GetViewNameConfigurationSource() == ConfigurationSource.Convention
                && entityType.GetViewName() is { } viewName)
            {
                entityTypeBuilder.ToView(_namingNameRewriter.RewriteName(viewName), entityType.GetViewSchema());
            }
        }
    }

    public void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
    {
        var entityType = entityTypeBuilder.Metadata;

        if (newBaseType is null || entityType.GetMappingStrategy() == RelationalAnnotationNames.TpcMappingStrategy)
        {
            // The entity is getting removed from a hierarchy. Set the (rewritten) TableName.
            if (entityType.GetTableName() is { } tableName && !entityType.ClrType.IsAbstract)
            {
                entityTypeBuilder.ToTable(_namingNameRewriter.RewriteName(tableName), entityType.GetSchema());
            }
        }
        else
        {
            // The entity is getting a new base type (e.g. joining a hierarchy).
            // If this is TPH, we remove the previously rewritten TableName (and non-rewritten Schema) which we set when the
            // entity type was first added to the model (see ProcessEntityTypeAdded).
            // If this is TPT, TableName and Schema are set explicitly, so the following will be ignored.
            // TPC is handled above (we need to rewrite just like with a normal table that isn't in an inheritance hierarchy)
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

        // An entity type is becoming owned - this is a bit complicated.
        // Unless it's a collection navigation, or the owned entity table name was explicitly set by the user, this triggers table
        // splitting, which means we need to undo rewriting which we've done previously.
        if (foreignKey.IsOwnership
            && !foreignKey.GetNavigation(false)!.IsCollection
            && ownedEntityType.GetTableNameConfigurationSource() != ConfigurationSource.Explicit)
        {
            // Reset the table name which we've set when the entity type was added.
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
        IConventionAnnotation? annotation,
        IConventionAnnotation? oldAnnotation,
        IConventionContext<IConventionAnnotation> context)
    {
        var entityType = entityTypeBuilder.Metadata;

        // If the View/SqlQuery/Function name is being set on the entity type, and its table name is set by convention, then we assume
        // we're the one who set the table name back when the entity type was originally added. We now undo this as the entity type
        // should only be mapped to the View/SqlQuery/Function.
        if (name is RelationalAnnotationNames.ViewName or RelationalAnnotationNames.SqlQuery or RelationalAnnotationNames.FunctionName
            && annotation?.Value is not null
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

            var rootType = entityType.GetRootType();
            var isTPT = rootType.GetDerivedTypes().FirstOrDefault() is { } derivedType
                && derivedType.GetTableName() != rootType.GetTableName();

            if (entityType.FindRowInternalForeignKeys(tableIdentifier).FirstOrDefault() is null && !isTPT)
            {
                if (primaryKey.GetDefaultName() is { } primaryKeyName)
                {
                    primaryKey.Builder.HasName(_namingNameRewriter.RewriteName(primaryKeyName));
                }
            }
            else
            {
                // This hierarchy is being transformed into TPT via the explicit setting of the table name.
                // We not only have to reset our own key name, but also the parents'. Otherwise, the parent's key name
                // is used as the child's (see RelationalKeyExtensions.GetName), and we get a "duplicate key name in database" error
                // since both parent and child have the same key name;
                foreach (var type in entityType.GetRootType().GetDerivedTypesInclusive())
                {
                    if (type.FindPrimaryKey() is IConventionKey pk)
                    {
                        pk.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
                    }
                }
            }
        }

        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            if (foreignKey.GetDefaultName() is { } foreignKeyName)
            {
                foreignKey.Builder.HasConstraintName(_namingNameRewriter.RewriteName(foreignKeyName));
            }
        }

        foreach (var index in entityType.GetIndexes())
        {
            if (index.GetDefaultDatabaseName() is { } indexName)
            {
                index.Builder.HasDatabaseName(_namingNameRewriter.RewriteName(indexName));
            }
        }

        if (annotation?.Value is not null
            && entityType.FindOwnership() is IConventionForeignKey ownership
            && (string)annotation.Value != ownership.PrincipalEntityType.GetTableName())
        {
            // An owned entity's table is being set explicitly - this is the trigger to undo table splitting (which is the default).

            // When the entity became owned, we prefixed all of its properties - we must now undo that.
            foreach (var property in entityType.GetProperties()
                         .Except(entityType.FindPrimaryKey()?.Properties ?? Array.Empty<IConventionProperty>())
                         .Where(p => p.Builder.CanSetColumnName(null)))
            {
                RewriteColumnName(property.Builder);
            }

            // We previously rewrote the owned entity's primary key name, when the owned entity was still in table splitting.
            // Now that its getting its own table, rewrite the primary key constraint name again.
            if (entityType.FindPrimaryKey() is IConventionKey key
                && key.GetDefaultName() is { } keyName)
            {
                key.Builder.HasName(_namingNameRewriter.RewriteName(keyName));
            }
        }
    }

    public void ProcessForeignKeyAdded(
        IConventionForeignKeyBuilder relationshipBuilder,
        IConventionContext<IConventionForeignKeyBuilder> context)
    {
        if (relationshipBuilder.Metadata.GetDefaultName() is { } constraintName)
        {
            relationshipBuilder.HasConstraintName(_namingNameRewriter.RewriteName(constraintName));
        }
    }

    public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
    {
        if (keyBuilder.Metadata.GetName() is { } keyName)
        {
            keyBuilder.HasName(_namingNameRewriter.RewriteName(keyName));
        }
    }

    public void ProcessIndexAdded(
        IConventionIndexBuilder indexBuilder,
        IConventionContext<IConventionIndexBuilder> context)
    {
        if (indexBuilder.Metadata.GetDefaultDatabaseName() is { } indexName)
        {
            indexBuilder.HasDatabaseName(_namingNameRewriter.RewriteName(indexName));
        }
    }

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
                var columnName = property.GetColumnName();
                if (columnName.StartsWith(entityType.ShortName() + '_', StringComparison.Ordinal))
                {
                    property.Builder.HasColumnName(
                        _namingNameRewriter.RewriteName(entityType.ShortName()) + columnName.Substring(entityType.ShortName().Length));
                }

                var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);
                if (storeObject is null)
                {
                    continue;
                }

                var shortName = entityType.ShortName();

                if (property.Builder.CanSetColumnName(null))
                {
                    columnName = property.GetColumnName();
                    if (columnName.StartsWith(shortName + '_', StringComparison.Ordinal))
                    {
                        property.Builder.HasColumnName(
                            _namingNameRewriter.RewriteName(shortName)
                            + columnName.Substring(shortName.Length));
                    }
                }

                if (property.Builder.CanSetColumnName(null, storeObject.Value))
                {
                    columnName = property.GetColumnName(storeObject.Value);
                    if (columnName is not null && columnName.StartsWith(shortName + '_', StringComparison.Ordinal))
                    {
                        property.Builder.HasColumnName(
                            _namingNameRewriter.RewriteName(shortName) + columnName.Substring(shortName.Length),
                            storeObject.Value);
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
            : property.GetDefaultColumnName();
        if (baseColumnName is not null)
        {
            propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(baseColumnName));
        }

        foreach (var storeObjectType in _storeObjectTypes)
        {
            var identifier = StoreObjectIdentifier.Create(entityType, storeObjectType);
            if (identifier is null)
            {
                continue;
            }

            if (property.GetColumnNameConfigurationSource(identifier.Value) == ConfigurationSource.Convention
                && property.GetColumnName(identifier.Value) is { } columnName)
            {
                propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(columnName), identifier.Value);
            }
        }
    }
}
