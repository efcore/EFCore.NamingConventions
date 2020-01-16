using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    class LowerCaseNameRewriter : NameRewriterBase
    {
        public override void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionContext<IConventionEntityTypeBuilder> context)
            => entityTypeBuilder.ToTable(
                entityTypeBuilder.Metadata.GetTableName().ToLowerInvariant(),
                entityTypeBuilder.Metadata.GetSchema());

        public override void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder,
            IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(
                propertyBuilder.Metadata.GetColumnName().ToLowerInvariant());
    }
}
