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
        readonly IDbContextOptions _options;
        public NamingConventionSetPlugin([NotNull] IDbContextOptions options) => _options = options;

        public ConventionSet ModifyConventions(ConventionSet conventionSet)
        {
            var extension = _options.FindExtension<NamingConventionsOptionsExtension>();
            var namingStyle = extension.NamingConvention;
            var culture = extension.Culture;
            if (namingStyle == NamingConvention.None)
                return conventionSet;
            NameRewriterBase nameRewriter = namingStyle switch
            {
                NamingConvention.SnakeCase => new SnakeCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.LowerCase => new LowerCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.UpperCase => new UpperCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                NamingConvention.UpperSnakeCase => new UpperSnakeCaseNameRewriter(culture ?? CultureInfo.InvariantCulture),
                _ => throw new NotImplementedException("Unhandled enum value: " + namingStyle)
            };

            conventionSet.EntityTypeAddedConventions.Add(nameRewriter);
            conventionSet.EntityTypeAnnotationChangedConventions.Add(nameRewriter);
            conventionSet.PropertyAddedConventions.Add(nameRewriter);
            conventionSet.ForeignKeyOwnershipChangedConventions.Add(nameRewriter);
            conventionSet.KeyAddedConventions.Add(nameRewriter);
            conventionSet.ForeignKeyAddedConventions.Add(nameRewriter);
            conventionSet.IndexAddedConventions.Add(nameRewriter);

            return conventionSet;
        }
    }
}
