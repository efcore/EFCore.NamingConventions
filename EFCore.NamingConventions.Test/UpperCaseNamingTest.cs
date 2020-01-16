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
        public void Column_name_is_rewritten()
        {
            using var context = CreateContext();
            var entityType = context.Model.FindEntityType(typeof(SimpleBlog));
            Assert.Equal("ID", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("FULLNAME", entityType.FindProperty("FullName").GetColumnName());
        }
        
        TestContext CreateContext() => new TestContext(NamingConventionsExtensions.UseUpperCaseNamingConvention);
    }
}
