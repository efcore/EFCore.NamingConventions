using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace EFCore.NamingConventions.Internal;

public class NameRewritingConvention :
    IEntityTypeAddedConvention,
    IEntityTypeAnnotationChangedConvention,
    IPropertyAddedConvention,
    IForeignKeyOwnershipChangedConvention,
    IKeyAddedConvention,
    IForeignKeyAddedConvention,
    IForeignKeyPropertiesChangedConvention,
    IIndexAddedConvention,
    IEntityTypeBaseTypeChangedConvention,
    IModelFinalizingConvention
{
    private static readonly StoreObjectType[] _storeObjectTypes
        = { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function, StoreObjectType.SqlQuery };

    private readonly IDictionary<Type, string> _sets;
    private readonly INameRewriter _namingNameRewriter;

    public NameRewritingConvention(ProviderConventionSetBuilderDependencies dependencies, INameRewriter nameRewriter)
    {
        _namingNameRewriter = nameRewriter;

        // Copied from TableNameFromDbSetConvention
        _sets = new Dictionary<Type, string>();
        List<Type>? ambiguousTypes = null;
        foreach (var set in dependencies.SetFinder.FindSets(dependencies.ContextType))
        {
            if (!_sets.ContainsKey(set.Type))
            {
                _sets.Add(set.Type, set.Name);
            }
            else
            {
                ambiguousTypes ??= new List<Type>();

                ambiguousTypes.Add(set.Type);
            }
        }

        if (ambiguousTypes != null)
        {
            foreach (var type in ambiguousTypes)
            {
                _sets.Remove(type);
            }
        }
    }

    public virtual void ProcessEntityTypeAdded(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionContext<IConventionEntityTypeBuilder> context)
    {
        var entityType = entityTypeBuilder.Metadata;

        // Note that the table name returned here may be the result of TableNameFromDbSetConvention which ran before us.
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

    public void ProcessEntityTypeBaseTypeChanged(
        IConventionEntityTypeBuilder entityTypeBuilder,
        IConventionEntityType? newBaseType,
        IConventionEntityType? oldBaseType,
        IConventionContext<IConventionEntityType> context)
        => ProcessHierarchyChange(entityTypeBuilder);

    private void ProcessHierarchyChange(IConventionEntityTypeBuilder entityTypeBuilder)
    {
        var newMappingStrategy = entityTypeBuilder.Metadata.GetRootType().GetMappingStrategy();

        // When a new type enters a TPC hierarchy, we must clear out the names of all indexes on parent entity types.
        // Indexes on parent indexes get applied for each concrete child (and the parent, if concrete); in other
        // words, the same index metadata is shared across multiple entity types mapped to different tables.
        // However, EF doesn't yet support configuring different names for the same index when it's applied to different tables (children),
        // so we can't rewrite.
        // So instead, we clear any previously rewritten names (e.g. from the default TPH setup, assuming we're transitioning from TPH to
        // TPC), from all indexes on parent entity types, to ensure that the index at least gets different names when applied to each table
        // (otherwise the same name is used, leading to a conflict).
        // For TPH and TPT this isn't a problem, since the same index never gets shared across different tables.
        // See #245.
        if (newMappingStrategy == RelationalAnnotationNames.TpcMappingStrategy)
        {
            foreach (var index in entityTypeBuilder.Metadata.GetIndexes())
            {
                index.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
            }

            foreach (var foreignKey in entityTypeBuilder.Metadata.GetForeignKeys())
            {
                foreignKey.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
            }
        }

        foreach (var entityType in entityTypeBuilder.Metadata.GetDerivedTypesInclusive())
        {
            entityTypeBuilder = entityType.Builder;

            // First, reset any rewritten name we previously set (e.g. when changing from TPH to TPT), and then rewrite the default name.
            // The one exception is for abstract types in a TPC hierarchy, where we should not set the name at all, since they don't map
            // to any table.
            entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.TableName);
            entityTypeBuilder.HasNoAnnotation(RelationalAnnotationNames.Schema);

            if (!(newMappingStrategy == RelationalAnnotationNames.TpcMappingStrategy && entityType.ClrType.IsAbstract))
            {
                if (GetDefaultTableName(entityType) is { } tableName)
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

        string? GetDefaultTableName(IConventionEntityType entityType)
            => !entityType.HasSharedClrType && _sets.TryGetValue(entityType.ClrType, out var setName)
                ? setName
                : entityType.GetTableName();
    }

    public virtual void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        IConventionContext<IConventionPropertyBuilder> context)
        => RewriteColumnName(propertyBuilder);

    public void ProcessForeignKeyOwnershipChanged(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
        => ProcessOwnershipChange(relationshipBuilder.Metadata, context);

    private void ProcessOwnershipChange(IConventionForeignKey foreignKey, IConventionContext context)
    {
        var ownedEntityType = foreignKey.DeclaringEntityType;

        // An entity type is becoming owned - this is a bit complicated.
        // This is a trigger for table splitting - unless the foreign key is non-unique (collection navigation), it's JSON ownership,
        // or the owned entity table name was explicitly set by the user.
        // If this is table splitting, we need to undo rewriting which we've done previously.

        // TODO: Un-own?
        if (foreignKey.IsOwnership)
        {
            // Reset the table name which we've set when the entity type was added.
            // If table splitting was configured by explicitly setting the table name, the following
            // does nothing.
            ownedEntityType.FindPrimaryKey()?.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);

            if (ownedEntityType.IsMappedToJson())
            {
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

                if (ownedEntityType.GetContainerColumnName() is string containerColumnName)
                {
                    ownedEntityType.SetContainerColumnName(_namingNameRewriter.RewriteName(containerColumnName));
                }

                // TODO: Note that we do not rewrite names of JSON properties (which aren't relational columns).
                // TODO: We could introduce an option for doing so, though that's probably not usually what people want when doing JSON
                foreach (var property in ownedEntityType.GetProperties())
                {
                    property.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);
                }
            }
            else
            {
                // 1. If the foreign key is unique (non-collection), reset any previously-rewritten name:
                //   * The EF default for these is table sharing, so we need to remove the table name to allow the owned to have null
                //     (use its owner's table name)
                //   * If the user explicitly specified a table name (to disable table splitting), this does nothing (convention doesn't
                //     override explicit).
                // 2. Otherwise, if the foreign key represents a collection, EF maps to a separate table. Making this owned doesn't change
                //    anything naming-wise.
                if (foreignKey.IsUnique)
                {
                    ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                    ownedEntityType.Builder.HasNoAnnotation(RelationalAnnotationNames.Schema);

                    // We've previously set rewritten column names when the entity was originally added (before becoming owned).
                    // These need to be rewritten again to include the owner prefix.
                    foreach (var property in ownedEntityType.GetProperties())
                    {
                        RewriteColumnName(property.Builder);
                    }
                }
            }

            context.StopProcessing();
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

        switch (name)
        {
            // The inheritance strategy is changing, e.g. the entity type is being made part of (or is leaving) a TPH/TPT/TPC hierarchy.
            case RelationalAnnotationNames.MappingStrategy:
            {
                ProcessHierarchyChange(entityTypeBuilder);
                return;
            }

            case RelationalAnnotationNames.ContainerColumnName:
            {
                // TODO: Support de-JSON-ification?
                var foreignKey = entityTypeBuilder.Metadata.FindOwnership();
                Debug.Assert(foreignKey is not null, "ContainerColumnName annotation changed but FindOwnership returned null");
                ProcessOwnershipChange(foreignKey, context);
                return;
            }

            // If the View/SqlQuery/Function name is being set on the entity type, and its table name is set by convention, then we assume
            // we're the one who set the table name back when the entity type was originally added. We now undo this as the entity type
            // should only be mapped to the View/SqlQuery/Function.
            case RelationalAnnotationNames.ViewName or RelationalAnnotationNames.SqlQuery or RelationalAnnotationNames.FunctionName
                when annotation?.Value is not null
                && entityType.GetTableNameConfigurationSource() == ConfigurationSource.Convention:
            {
                entityType.SetTableName(null);
                return;
            }

            // The table's name is changing - rewrite keys, index names
            case RelationalAnnotationNames.TableName
                when StoreObjectIdentifier.Create(entityType, StoreObjectType.Table) is StoreObjectIdentifier tableIdentifier:
            {
                var mappingStrategy = entityType.GetMappingStrategy();

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

                foreach (var foreignKey in entityType.GetDeclaredForeignKeys())
                {
                    // See note in ProcessHierarchyChange on indexes and foreign keys in TPC hierarchies
                    if (mappingStrategy == RelationalAnnotationNames.TpcMappingStrategy && entityType.GetDerivedTypes().Any())
                    {
                        foreignKey.Builder.HasNoAnnotation(RelationalAnnotationNames.Name);
                    }
                    else if (foreignKey.GetDefaultName() is { } foreignKeyName)
                    {
                        foreignKey.Builder.HasConstraintName(_namingNameRewriter.RewriteName(foreignKeyName));
                    }
                }

                foreach (var index in entityType.GetDeclaredIndexes())
                {
                    // See note in ProcessHierarchyChange on indexes and foreign keys in TPC hierarchies
                    if (mappingStrategy == RelationalAnnotationNames.TpcMappingStrategy && entityType.GetDerivedTypes().Any())
                    {
                        index.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                    }
                    else if (index.GetDefaultDatabaseName() is { } indexName)
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

                return;
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

    public void ProcessForeignKeyPropertiesChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IReadOnlyList<IConventionProperty> oldDependentProperties,
        IConventionKey oldPrincipalKey,
        IConventionContext<IReadOnlyList<IConventionProperty>> context)
    {
        if (relationshipBuilder.Metadata.GetDefaultName() is { } constraintName && relationshipBuilder.Metadata.IsInModel)
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
            // Copied from TableNameFromDbSetConvention
            if (entityType.GetTableName() != null
                && _sets.ContainsKey(entityType.ClrType))
            {
                // if (entityType.GetViewNameConfigurationSource() != null)
                // {
                //     // Undo the convention change if the entity type is mapped to a view
                //     entityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                // }

                switch (entityType.GetMappingStrategy())
                {
                    // Undo the convention change if the entity type is mapped using TPC
                    case RelationalAnnotationNames.TpcMappingStrategy when entityType.IsAbstract():
                    // Undo the convention change if the hierarchy ultimately ends up TPH
                    case RelationalAnnotationNames.TphMappingStrategy when entityType.BaseType is not null:
                        entityType.Builder.HasNoAnnotation(RelationalAnnotationNames.TableName);
                        break;
                }
            }

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
        var structuralType = property.DeclaringType;

        // Remove any previous setting of the column name we may have done, so we can get the default recalculated below.
        property.Builder.HasNoAnnotation(RelationalAnnotationNames.ColumnName);

        // TODO: The following is a temporary hack. We should probably just always set the relational override below,
        // but https://github.com/dotnet/efcore/pull/23834
        var baseColumnName = StoreObjectIdentifier.Create(structuralType, StoreObjectType.Table) is { } tableIdentifier
            ? property.GetDefaultColumnName(tableIdentifier)
            : property.GetDefaultColumnName();
        if (baseColumnName is not null)
        {
            propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(baseColumnName));
        }

        foreach (var storeObjectType in _storeObjectTypes)
        {
            var identifier = StoreObjectIdentifier.Create(structuralType, storeObjectType);
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
