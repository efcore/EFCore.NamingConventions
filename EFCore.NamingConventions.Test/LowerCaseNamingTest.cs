using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.Naming.Test
{
    public class LowerCaseNamingTest
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = new TestContext();
            var entityType = context.Model.FindEntityType(typeof(BlogTable));
            Assert.Equal("blogtable", entityType.GetTableName());
        }

        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = new TestContext();
            var entityType = context.Model.FindEntityType(typeof(BlogTable));
            Assert.Equal("id", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("fullname", entityType.FindProperty("FullName").GetColumnName());
        }

        public class TestContext : DbContext
        {
            public DbSet<BlogTable> BlogTable { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder
                    .UseInMemoryDatabase("test")
                    .UseLowerCaseNamingConvention();
        }

        public class BlogTable
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }
    }
}
