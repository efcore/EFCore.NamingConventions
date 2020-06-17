using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class SnakeCaseNamingTest : RewriterTestBase
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
            Assert.Equal("ix_simple_blog_full_name", entityType.GetIndexes().Single().GetName());
        }

        TestContext CreateContext(CultureInfo culture = null) => new TestContext(builder => builder.UseSnakeCaseNamingConvention(culture));
    }
}
