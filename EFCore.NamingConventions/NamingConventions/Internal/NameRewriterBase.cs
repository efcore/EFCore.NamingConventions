using System.Globalization;
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
            => entityTypeBuilder.ToTable(
                RewriteName(entityTypeBuilder.Metadata.GetTableName()),
                entityTypeBuilder.Metadata.GetSchema());

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(RewriteName(propertyBuilder.Metadata.GetColumnName()));

        public void ProcessForeignKeyOwnershipChanged(
            IConventionRelationshipBuilder relationshipBuilder,
            IConventionContext<IConventionRelationshipBuilder> context)
        {
            if (relationshipBuilder.Metadata.IsOwnership)
            {
                // Unset the table name which we've set when the entity type was added
                relationshipBuilder.Metadata.DeclaringEntityType.SetTableName(null);
            }
        }

        public void ProcessKeyAdded(IConventionKeyBuilder keyBuilder, IConventionContext<IConventionKeyBuilder> context)
            => keyBuilder.HasName(RewriteName(keyBuilder.Metadata.GetName()));

        public void ProcessForeignKeyAdded(
            IConventionRelationshipBuilder relationshipBuilder,
            IConventionContext<IConventionRelationshipBuilder> context)
            => relationshipBuilder.HasConstraintName(RewriteName(relationshipBuilder.Metadata.GetConstraintName()));

        public void ProcessIndexAdded(
            IConventionIndexBuilder indexBuilder,
            IConventionContext<IConventionIndexBuilder> context)
            => indexBuilder.HasName(RewriteName(indexBuilder.Metadata.GetName()));

        protected abstract string RewriteName(string name);
    }
}
