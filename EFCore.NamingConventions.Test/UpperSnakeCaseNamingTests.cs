using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class UpperSnakeCaseNamingTests : RewriterTestBase
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SIMPLE_BLOG", entityType.GetTableName());
        }

        [Fact]
        public void Table_name_is_rewritten_in_turkish()
        {
            using var context = CreateContext(CultureInfo.CreateSpecificCulture("tr-TR"));
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SİMPLE_BLOG", entityType.GetTableName());
        }

        [Fact]
        public void Table_name_is_rewritten_in_invariant()
        {
            using var context = CreateContext(CultureInfo.InvariantCulture);
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SIMPLE_BLOG", entityType.GetTableName());
        }

        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ID", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("FULL_NAME", entityType.FindProperty("FullName").GetColumnName());
        }

        [Fact]
        public void Owned_entity_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(OwnedStatistics1));
            Assert.Equal("SIMPLE_BLOG", entityType.GetTableName());
            var property = entityType.GetProperty(nameof(OwnedStatistics1.SomeStatistic));
            Assert.Equal("OWNED_STATISTICS1_SOME_STATISTIC", property.GetColumnName());
        }

        [Fact]
        public void Owned_entity_split_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(OwnedStatistics2));
            Assert.Equal("OwnedStatisticsSplit", entityType.GetTableName());
            var property = entityType.GetProperty(nameof(OwnedStatistics2.SomeStatistic));
            Assert.Equal("SOME_STATISTIC", property.GetColumnName());
        }

        [Fact]
        public void Primary_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("PK_SIMPLE_BLOG", entityType.GetKeys().Single(k => k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Alternative_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("AK_SIMPLE_BLOG_SOME_ALTERNATIVE_KEY", entityType.GetKeys().Single(k => !k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Foreign_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(Post));
            Assert.Equal("FK_POST_SIMPLE_BLOG_BLOG_ID", entityType.GetForeignKeys().Single().GetConstraintName());
        }

        [Fact]
        public void Index_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("IX_SIMPLE_BLOG_FULL_NAME", entityType.GetIndexes().Single().GetDatabaseName());
        }

        TestContext CreateContext(CultureInfo culture = null) => new TestContext(builder => builder.UseUpperSnakeCaseNamingConvention(culture));
    }
}
