using System;
using System.Collections.Generic;
using System.Globalization;
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
        public void ModifyConventions_without_extesnions_not_throw_exception()
        {
            var conventionSet = SqliteTestHelpers
                .Instance
                .CreateContextServices()
                .GetRequiredService<IConventionSetBuilder>()
                .CreateConventionSet();

            var optionsBuilder = new DbContextOptionsBuilder();
            SqliteTestHelpers.Instance.UseProviderOptions(optionsBuilder);
            var plugin = new NamingConventionSetPlugin(optionsBuilder.Options);

            var exception = Record.Exception(() => plugin.ModifyConventions(conventionSet));

            Assert.Null(exception);
        }

    }
}
