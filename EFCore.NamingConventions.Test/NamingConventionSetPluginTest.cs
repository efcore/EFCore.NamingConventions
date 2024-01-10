using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class NamingConventionSetPluginTest
    {
        [Fact]
        public void ModifyConventions_without_extensions_not_throw_exception()
        {
            var services = SqlServerTestHelpers
                .Instance
                .CreateContextServices();

            var conventionSet = services
                .GetRequiredService<IConventionSetBuilder>()
                .CreateConventionSet();

            var dependencies = services.GetRequiredService<ProviderConventionSetBuilderDependencies>();
            var optionsBuilder = new DbContextOptionsBuilder();
            SqlServerTestHelpers.Instance.UseProviderOptions(optionsBuilder);
            var plugin = new NamingConventionSetPlugin(dependencies, optionsBuilder.Options);

            var exception = Record.Exception(() => plugin.ModifyConventions(conventionSet));

            Assert.Null(exception);
        }

    }
}
