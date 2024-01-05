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
        #region Table_name_is_taken_from_DbSet_property

        [Fact]
        public void Table_name_is_taken_from_DbSet_property()
        {
            using var context = new BlogContext();
            var entityType = context.Model.FindEntityType(typeof(Blog))!;
            Assert.Equal("blogs", entityType.GetTableName());
        }

        [Fact]
        public void Table_name_is_taken_from_DbSet_property_with_TPH()
        {
            using var context = new TphBlogContext();
            Assert.Equal("blogs", context.Model.FindEntityType(typeof(Blog))!.GetTableName());
            Assert.Equal("blogs", context.Model.FindEntityType(typeof(SpecialBlog))!.GetTableName());
        }

        public class Blog
        {
            public int Id { get; set; }
        }

        public class SpecialBlog : Blog
        {
            public string SpecialProperty { get; set; }
        }

        public class BlogContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; } = null!;

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer("foo").UseSnakeCaseNamingConvention();
        }

        public class TphBlogContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; } = null!;
            public DbSet<SpecialBlog> SpecialBlogs { get; set; } = null!;

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer("foo").UseSnakeCaseNamingConvention();
        }

        #endregion Table_name_is_taken_from_DbSet_property

        #region Multiple_DbContexts_with_external_di_does_not_throw

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

        #endregion Multiple_DbContexts_with_external_di_does_not_throw
    }
}
