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

            var property = entityType.FindProperty(nameof(Blog.BlogProperty))!;
            Assert.Equal("blog_property", property.GetColumnName());

            var index = Assert.Single(entityType.GetIndexes());
            Assert.Equal("ix_blogs_blog_property", index.GetDatabaseName());
        }

        [Fact]
        public void Table_name_is_taken_from_DbSet_property_with_TPH()
        {
            using var context = new TphBlogContext();

            var blogEntityType = context.Model.FindEntityType(typeof(Blog))!;
            var specialBlogEntityType = context.Model.FindEntityType(typeof(SpecialBlog))!;

            Assert.Equal("blogs", blogEntityType.GetTableName());
            Assert.Equal("blogs", specialBlogEntityType.GetTableName());

            var blogProperty = blogEntityType.FindProperty(nameof(Blog.BlogProperty))!;
            Assert.Equal("blog_property", blogProperty.GetColumnName());
            var specialBlogProperty = specialBlogEntityType.FindProperty(nameof(SpecialBlog.SpecialBlogProperty))!;
            Assert.Equal("special_blog_property", specialBlogProperty.GetColumnName());

            var blogIndex = Assert.Single(blogEntityType.GetIndexes());
            Assert.Equal("ix_blogs_blog_property", blogIndex.GetDatabaseName());
            var specialBlogIndex = Assert.Single(specialBlogEntityType.GetDeclaredIndexes());
            Assert.Equal("ix_blogs_special_blog_property", specialBlogIndex.GetDatabaseName());
        }

        public class Blog
        {
            public int Id { get; set; }
            public string BlogProperty { get; set; }
        }

        public class SpecialBlog : Blog
        {
            public string SpecialBlogProperty { get; set; }
        }

        public class BlogContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; } = null!;

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer("foo").UseSnakeCaseNamingConvention();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<Blog>().HasIndex(b => b.BlogProperty);
        }

        public class TphBlogContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; } = null!;
            public DbSet<SpecialBlog> SpecialBlogs { get; set; } = null!;

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer("foo").UseSnakeCaseNamingConvention();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Blog>().HasIndex(b => b.BlogProperty);
                modelBuilder.Entity<SpecialBlog>().HasIndex(b => b.SpecialBlogProperty);
            }
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
