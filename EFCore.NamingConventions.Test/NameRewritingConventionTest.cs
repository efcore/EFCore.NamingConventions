using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class NameRewritingConventionTest
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("simple_blog", entityType.GetTableName());
        }

        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("id", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("full_name", entityType.FindProperty("FullName").GetColumnName());
        }

        [Fact]
        public void Column_name_is_rewritten_in_turkish()
        {
            using var context = CreateContext(CultureInfo.CreateSpecificCulture("tr-TR"));
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ıd", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("full_name", entityType.FindProperty("FullName").GetColumnName());
        }

        [Fact]
        public void Column_name_is_rewritten_in_invariant()
        {
            using var context = CreateContext(CultureInfo.InvariantCulture);
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("id", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("full_name", entityType.FindProperty("FullName").GetColumnName());
        }

        [Fact]
        public void Owned_entity_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(OwnedStatistics1));
            Assert.Equal("simple_blog", entityType.GetTableName());
            var property = entityType.GetProperty(nameof(OwnedStatistics1.SomeStatistic));
            Assert.Equal("owned_statistics1_some_statistic", property.GetColumnName());
        }

        [Fact]
        public void Owned_entity_split_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(OwnedStatistics2));
            Assert.Equal("OwnedStatisticsSplit", entityType.GetTableName());
            var property = entityType.GetProperty(nameof(OwnedStatistics2.SomeStatistic));
            Assert.Equal("some_statistic", property.GetColumnName());
        }

        [Fact]
        public void Primary_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("pk_simple_blog", entityType.GetKeys().Single(k => k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Alternative_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ak_simple_blog_some_alternative_key", entityType.GetKeys().Single(k => !k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Foreign_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(Post));
            Assert.Equal("fk_post_simple_blog_blog_id", entityType.GetForeignKeys().Single().GetConstraintName());
        }

        [Fact]
        public void Index_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ix_simple_blog_full_name", entityType.GetIndexes().Single().GetDatabaseName());
        }

        #region Support

        TestContext CreateContext(CultureInfo culture = null) => new TestContext(builder => builder.UseSnakeCaseNamingConvention(culture));

        public class TestContext : DbContext
        {
            readonly Func<DbContextOptionsBuilder, DbContextOptionsBuilder> _useNamingConvention;
            public TestContext(Func<DbContextOptionsBuilder, DbContextOptionsBuilder> useNamingConvention)
                => _useNamingConvention = useNamingConvention;

            public DbSet<SimpleBlog> Blog { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<SimpleBlog>(e =>
                {
                    e.HasIndex(b => b.FullName);
                    e.OwnsOne(b => b.OwnedStatistics1);
                    e.OwnsOne(b => b.OwnedStatistics2, s => s.ToTable("OwnedStatisticsSplit"));
                    e.HasAlternateKey(b => b.SomeAlternativeKey);
                });

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => _useNamingConvention(optionsBuilder.UseInMemoryDatabase("test"));
        }

        public class SimpleBlog
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public int SomeAlternativeKey { get; set; }

            public List<Post> Posts { get; set; }

            public OwnedStatistics1 OwnedStatistics1 { get; set; }
            public OwnedStatistics2 OwnedStatistics2 { get; set; }
        }

        public class Post
        {
            public int Id { get; set; }
            public string FullName { get; set; }

            public int BlogId { get; set; }
            public SimpleBlog Blog { get; set; }
        }

        public class OwnedStatistics1
        {
            public int SomeStatistic { get; set; }
        }

        public class OwnedStatistics2
        {
            public int SomeStatistic { get; set; }
        }

        #endregion
    }
}
