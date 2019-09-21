using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.Naming.Test
{
    public class NamingTest
    {
        [Fact]
        public void Table_name_is_rewritten()
        {
            using var context = new TestContext();
            var entityType = context.Model.FindEntityType(typeof(Blog));
            Assert.Equal("blog", entityType.GetTableName());
        }

        [Fact]
        public void Column_name_is_rewritten()
        {
            using var context = new TestContext();
            var entityType = context.Model.FindEntityType(typeof(Blog));
            Assert.Equal("id", entityType.FindProperty("Id").GetColumnName());
            Assert.Equal("full_name", entityType.FindProperty("FullName").GetColumnName());
        }

        public class TestContext : DbContext
        {
            public DbSet<Blog> Blog { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder
                    .UseInMemoryDatabase("test")
                    .UseSnakeCaseNamingConvention();
        }

        public class Blog
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }
    }
}
