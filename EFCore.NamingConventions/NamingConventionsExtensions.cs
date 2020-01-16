using Microsoft.EntityFrameworkCore.Infrastructure;
using JetBrains.Annotations;
using EFCore.NamingConventions.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class NamingConventionsExtensions
    {
        public static DbContextOptionsBuilder UseSnakeCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithSnakeCaseNamingConvention();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseSnakeCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSnakeCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder);

        public static DbContextOptionsBuilder UseLowerCaseNamingConvention([NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithLowerCaseNamingConvention();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseLowerCaseNamingConvention<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseLowerCaseNamingConvention((DbContextOptionsBuilder)optionsBuilder);
    }
}
