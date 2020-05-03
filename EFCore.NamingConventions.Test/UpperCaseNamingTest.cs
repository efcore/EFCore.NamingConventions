using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class UpperCaseNamingTest : RewriterTestBase
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SIMPLEBLOG", entityType.GetTableName());
        }

        [Fact]
        public void Table_name_is_rewritten_in_turkish()
        {
            using var context = CreateContext(CultureInfo.CreateSpecificCulture("tr_TR"));
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SİMPLEBLOG", entityType.GetTableName());
        }

        [Fact]
        public void Table_name_is_rewritten_in_invariant()
        {
            using var context = CreateContext(CultureInfo.InvariantCulture);
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("SIMPLEBLOG", entityType.GetTableName());
        }
        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ID", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("FULLNAME", entityType.FindProperty("FullName").GetColumnName());
        }

        [Fact]
        public void Primary_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("PK_SIMPLEBLOG", entityType.GetKeys().Single(k => k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Alternative_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("AK_SIMPLEBLOG_SOMEALTERNATIVEKEY", entityType.GetKeys().Single(k => !k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Foreign_key_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(Post));
            Assert.Equal("FK_POST_SIMPLEBLOG_BLOGID", entityType.GetForeignKeys().Single().GetConstraintName());
        }

        [Fact]
        public void Index_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("IX_SIMPLEBLOG_FULLNAME", entityType.GetIndexes().Single().GetName());
        }
        TestContext CreateContext(CultureInfo culture = null) => new TestContext((builder) => builder.UseUpperCaseNamingConvention(culture));
    }
}
