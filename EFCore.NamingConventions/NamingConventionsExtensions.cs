using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using JetBrains.Annotations;
using EFCore.NamingConventions.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class NamingConventionsExtensions
    {
        public static DbContextOptionsBuilder UseSnakeCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder , CultureInfo cultureInfo = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithSnakeCaseNamingConvention(cultureInfo);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseSnakeCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder , CultureInfo cultureInfo = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSnakeCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder,cultureInfo);

        public static DbContextOptionsBuilder UseLowerCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder, CultureInfo cultureInfo = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithLowerCaseNamingConvention(cultureInfo);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseLowerCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, CultureInfo cultureInfo = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseLowerCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder,cultureInfo);

        public static DbContextOptionsBuilder UseUpperCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder, CultureInfo cultureInfo = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithUpperCaseNamingConvention(cultureInfo);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseUpperCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, CultureInfo cultureInfo = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseUpperCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder,cultureInfo);
    }
}
