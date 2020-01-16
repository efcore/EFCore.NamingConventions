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

        TestContext CreateContext() => new TestContext(NamingConventionsExtensions.UseSnakeCaseNamingConvention);
    }
}
