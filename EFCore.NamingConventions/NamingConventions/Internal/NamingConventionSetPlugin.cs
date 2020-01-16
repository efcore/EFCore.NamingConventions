using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using JetBrains.Annotations;

namespace EFCore.NamingConventions.Internal
{
    public class NamingConventionSetPlugin : IConventionSetPlugin
    {
        readonly IDbContextOptions _options;

        public NamingConventionSetPlugin([NotNull] IDbContextOptions options) => _options = options;

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var namingStyle = _options.FindExtension<NamingConventionsOptionsExtension>().NamingConvention;

            if (namingStyle == NamingConvention.None)
                return conventionSet;

            NameRewriterBase nameRewriter = namingStyle switch
            {
                NamingConvention.SnakeCase => new SnakeCaseNameRewriter(),
                NamingConvention.AllLowerCase => new LowerCaseNameRewriter(),
                _ => throw new NotImplementedException("Unhandled enum value: " + namingStyle)
            };

            conventionSet.EntityTypeAddedConventions.Add(nameRewriter);
            conventionSet.PropertyAddedConventions.Add(nameRewriter);
            return conventionSet;
        }
    }
}
