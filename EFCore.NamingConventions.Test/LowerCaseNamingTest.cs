using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class LowerCaseNamingTest : RewriterTestBase
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("simpleblog", entityType.GetTableName());
        }

        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("id", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("fullname", entityType.FindProperty("FullName").GetColumnName());
        }
        
        TestContext CreateContext() => new TestContext(NamingConventionsExtensions.UseLowerCaseNamingConvention);
    }
}
