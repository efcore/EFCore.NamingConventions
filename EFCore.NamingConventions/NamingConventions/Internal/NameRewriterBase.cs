using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    /// <summary>
    /// This class only required so we can have common superclass for all name rewriters
    /// </summary>
    internal abstract class NameRewriterBase : IEntityTypeAddedConvention, IPropertyAddedConvention
    {
        public abstract void ProcessEntityTypeAdded(IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context);

        public abstract void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context);
    }
}
