using Microsoft.EntityFrameworkCore.Infrastructure;
using JetBrains.Annotations;
using EFCore.NamingConventions.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class NamingConventionsExtensions
    {
        public static DbContextOptionsBuilder UseSnakeCaseNamingConventions([NotNull] this DbContextOptionsBuilder optionsBuilder)
        {
            Check.NotNull(optionsBuilder, nameof(optionsBuilder));

            var extension = (optionsBuilder.Options.FindExtension<NamingConventionsOptionsExtension>() ?? new NamingConventionsOptionsExtension())
                .WithSnakeCaseNaming();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        public static DbContextOptionsBuilder<TContext> UseSnakeCaseNamingConventions<TContext>([NotNull] this DbContextOptionsBuilder<TContext> optionsBuilder)
            where TContext : DbContext
            => (DbContextOptionsBuilder<TContext>)UseSnakeCaseNamingConventions((DbContextOptionsBuilder)optionsBuilder);
    }
}
