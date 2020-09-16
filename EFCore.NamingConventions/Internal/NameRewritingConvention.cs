using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    public class NameRewritingConvention :
        IEntityTypeAddedConvention, IEntityTypeAnnotationChangedConvention, IPropertyAddedConvention,
        IForeignKeyOwnershipChangedConvention, IKeyAddedConvention, IForeignKeyAddedConvention,
        IIndexAddedConvention
    {
        private readonly INameRewriter _namingNameRewriter;

        public NameRewritingConvention(INameRewriter nameRewriter) => _namingNameRewriter = nameRewriter;

        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            // Only touch root entities for now (TPH). Revisit for TPT/TPC.
            if (entityType.BaseType == null)
            {
                entityTypeBuilder.ToTable(_namingNameRewriter.RewriteName(entityType.GetTableName()), entityType.GetSchema());
            }
        }

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(_namingNameRewriter.RewriteName(propertyBuilder.Metadata.GetColumnName()));


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
                ownedEntityType.SetTableName(ownedEntityType.GetDefaultTableName());

                // Also need to reset all primary key properties
                foreach (var keyProperty in ownedEntityType.FindPrimaryKey().Properties)
                {
                    keyProperty.SetColumnName(keyProperty.GetDefaultColumnName());
                }

                // Finally, we need to apply the entity type prefix to all the owned properties, since
                // the convention that normally does this (SharedTableConvention) runs at model finalization
                // time, and will not overwrite our own rewritten column names.
                foreach (var property in ownedEntityType.GetProperties()
                    .Except(ownedEntityType.FindPrimaryKey().Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    var columnName = property.GetColumnName();
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
                annotation?.GetConfigurationSource() == ConfigurationSource.Explicit &&
                entityTypeBuilder.Metadata.FindOwnership() != null)
            {
                // An owned entity's table is being set explicitly - this is the trigger to do table
                // splitting. When the entity became owned, we prefixed all of its properties - we
                // must now undo that.
                foreach (var property in entityTypeBuilder.Metadata.GetProperties()
                    .Except(entityTypeBuilder.Metadata.FindPrimaryKey().Properties)
                    .Where(p => p.Builder.CanSetColumnName(null)))
                {
                    property.Builder.HasColumnName(_namingNameRewriter.RewriteName(property.GetDefaultColumnName()));
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
