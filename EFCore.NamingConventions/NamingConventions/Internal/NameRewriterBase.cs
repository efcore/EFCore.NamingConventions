using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    /// <summary>
    /// This class only required so we can have common superclass for all name rewriters
    /// </summary>
    internal abstract class NameRewriterBase : IEntityTypeAddedConvention, IPropertyAddedConvention,
        IForeignKeyOwnershipChangedConvention
    {
        public virtual void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
            => entityTypeBuilder.ToTable(
                RewriteName(entityTypeBuilder.Metadata.GetTableName()),
                entityTypeBuilder.Metadata.GetSchema());

        public virtual void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(
                RewriteName(propertyBuilder.Metadata.GetColumnName()));

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

        protected abstract string RewriteName(string name);
    }
}
