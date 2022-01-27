using System;
using System.Collections.Generic;
using System.Globalization;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class IntegrationTest
    {
        [Fact]
        public void multi_dbcontext_with_external_di_not_throw_exception()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkNamingConventions();
            services.AddTransient<ITestService,TestService>();
            var provider = SqliteTestHelpers.Instance.AddProviderServices(services)
                .AddDbContextPool<Test1DBContext>((serviceProvider, options) =>
                {
                    SqliteTestHelpers.Instance.UseProviderOptions(options);
                    options.LogTo(Console.WriteLine).UseInternalServiceProvider(serviceProvider);
                })
                .AddDbContextPool<Test2DBContext>((serviceProvider, options) =>
                {
                    SqliteTestHelpers.Instance.UseProviderOptions(options);
                    options.UseSnakeCaseNamingConvention().UseInternalServiceProvider(serviceProvider);
                }).BuildServiceProvider();

            var exception = Record.Exception(() => {
                var context1 = provider.GetRequiredService<Test1DBContext>();
                var context2 = provider.GetRequiredService<Test2DBContext>();
            });

            Assert.Null(exception);
        }


        public class Test1DBContext : DbContext
        {
            public Test1DBContext(DbContextOptions<Test1DBContext> options) : base(options)
            {
                this.GetInfrastructure().GetRequiredService<ITestService>();
            }
        }

        public class Test2DBContext : DbContext
        {
            public Test2DBContext(DbContextOptions<Test2DBContext> options) : base(options)
            {
                this.GetInfrastructure().GetRequiredService<ITestService>();
            }
        }

        public interface ITestService { }

        public class TestService : ITestService { }
    }
}
