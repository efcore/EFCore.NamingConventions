using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    /// <summary>
    /// This class only required so we can have common superclass for all name rewriters
    /// </summary>
    internal abstract class NameRewriterBase :
        IEntityTypeAddedConvention, IPropertyAddedConvention, IForeignKeyOwnershipChangedConvention,
        IKeyAddedConvention, IForeignKeyAddedConvention,
        IIndexAddedConvention
    {
        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
        {
            var entityType = entityTypeBuilder.Metadata;

            // Only touch root entities for now (TPH). Revisit for TPT/TPC.
            if (entityType.BaseType == null)
            {
                entityTypeBuilder.ToTable(RewriteName(entityType.GetTableName()), entityType.GetSchema());
            }
        }

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(RewriteName(propertyBuilder.Metadata.GetColumnName()));


        public void ProcessForeignKeyOwnershipChanged(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<bool?> context)
        {
            var foreignKey = relationshipBuilder.Metadata;

            if (foreignKey.IsOwnership)
            {
                // Reset the table name which we've set when the entity type was added
                // If table splitting was configured by explicitly setting the table name, the following
                // does nothing.
                foreignKey.DeclaringEntityType.SetTableName(foreignKey.DeclaringEntityType.GetDefaultTableName());

                // Also need to reset all primary key properties
                foreach (var keyProperty in foreignKey.DeclaringEntityType.FindPrimaryKey().Properties)
                {
                    keyProperty.SetColumnName(keyProperty.GetDefaultColumnName());
                }
            }
        }

        public void ProcessForeignKeyAdded(IConventionForeignKeyBuilder relationshipBuilder, IConventionContext<IConventionForeignKeyBuilder> context)
            => relationshipBuilder.HasConstraintName(RewriteName(relationshipBuilder.Metadata.GetConstraintName()));

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
            => keyBuilder.HasName(RewriteName(keyBuilder.Metadata.GetName()));

        public void ProcessIndexAdded(
            IConventionIndexBuilder indexBuilder,
            IConventionContext<IConventionIndexBuilder> context)
            => indexBuilder.HasName(RewriteName(indexBuilder.Metadata.GetName()));

        protected abstract string RewriteName(string name);
    }
}
