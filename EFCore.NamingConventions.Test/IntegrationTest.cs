using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class IntegrationTest
    {
        [Fact]
        public void Multiple_DbContexts_with_external_di_does_not_throw()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkNamingConventions();
            services.AddTransient<ITestService,TestService>();
            var provider = SqlServerTestHelpers.Instance.AddProviderServices(services)
                .AddDbContextPool<Test1DbContext>((serviceProvider, options) =>
                {
                    SqlServerTestHelpers.Instance.UseProviderOptions(options);
                    options.LogTo(Console.WriteLine).UseInternalServiceProvider(serviceProvider);
                })
                .AddDbContextPool<Test2DbContext>((serviceProvider, options) =>
                {
                    SqlServerTestHelpers.Instance.UseProviderOptions(options);
                    options.UseSnakeCaseNamingConvention().UseInternalServiceProvider(serviceProvider);
                }).BuildServiceProvider();

            var exception = Record.Exception(() => {
                _ = provider.GetRequiredService<Test1DbContext>();
                _ = provider.GetRequiredService<Test2DbContext>();
            });

            Assert.Null(exception);
        }

        public class Test1DbContext : DbContext
        {
            public Test1DbContext(DbContextOptions<Test1DbContext> options) : base(options)
            {
                this.GetInfrastructure().GetRequiredService<ITestService>();
            }
        }

        public class Test2DbContext : DbContext
        {
            public Test2DbContext(DbContextOptions<Test2DbContext> options) : base(options)
            {
                this.GetInfrastructure().GetRequiredService<ITestService>();
            }
        }

        public interface ITestService;

        public class TestService : ITestService;
    }
}
