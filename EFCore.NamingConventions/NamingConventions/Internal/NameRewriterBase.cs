using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    /// <summary>
    /// This class only required so we can have common superclass for all name rewriters
    /// </summary>
    internal abstract class NameRewriterBase : IEntityTypeAddedConvention, IPropertyAddedConvention
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

        protected abstract string RewriteName(string name);
    }
}
