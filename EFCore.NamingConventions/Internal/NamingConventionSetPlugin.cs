using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using JetBrains.Annotations;

namespace EFCore.NamingConventions.Internal
{
    public class NamingConventionSetPlugin : IConventionSetPlugin
    {
        private readonly IDbContextOptions _options;
        public NamingConventionSetPlugin([NotNull] IDbContextOptions options) => _options = options;

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<NamingConventionsOptionsExtension>();
            var namingStyle = extension.NamingConvention;
            var culture = extension.Culture;
            if (namingStyle == NamingConvention.None)
            {
                return conventionSet;
            }

            var convention = new NameRewritingConvention(namingStyle switch
            {
                NamingConvention.SnakeCase => new SnakeCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.LowerCase => new LowerCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.UpperCase => new UpperCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.UpperSnakeCase => new UpperSnakeCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException("Unhandled enum value: " + namingStyle)
            });

            conventionSet.EntityTypeAddedConventions.Add(convention);
            conventionSet.EntityTypeAnnotationChangedConventions.Add(convention);
            conventionSet.PropertyAddedConventions.Add(convention);
            conventionSet.ForeignKeyOwnershipChangedConventions.Add(convention);
            conventionSet.KeyAddedConventions.Add(convention);
            conventionSet.ForeignKeyAddedConventions.Add(convention);
            conventionSet.IndexAddedConventions.Add(convention);

            return conventionSet;
        }
    }
}
