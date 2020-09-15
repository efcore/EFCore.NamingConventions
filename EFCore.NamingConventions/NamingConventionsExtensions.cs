using System.Globalization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using JetBrains.Annotations;
using EFCore.NamingConventions.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class NamingConventionsExtensions
    {
        public static DbContextOptionsBuilder UseSnakeCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder , CultureInfo culture = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithSnakeCaseNamingConvention(culture);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseSnakeCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder , CultureInfo culture = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSnakeCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder, culture);

        public static DbContextOptionsBuilder UseLowerCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder, CultureInfo culture = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithLowerCaseNamingConvention(culture);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseLowerCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, CultureInfo culture = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseLowerCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder ,culture);

        public static DbContextOptionsBuilder UseUpperCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder, CultureInfo culture = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithUpperCaseNamingConvention(culture);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseUpperCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, CultureInfo culture = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseUpperCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder, culture);

        public static DbContextOptionsBuilder UseUpperSnakeCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder, CultureInfo culture = null)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithUpperSnakeCaseNamingConvention(culture);

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseUpperSnakeCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder, CultureInfo culture = null)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseUpperSnakeCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder, culture);
    }
}
