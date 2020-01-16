using System;
using Microsoft.EntityFrameworkCore;

namespace EFCore.NamingConventions.Test
{
    public abstract class RewriterTestBase
    {
        public class TestContext : DbContext
        {
            readonly Func<DbContextOptionsBuilder, DbContextOptionsBuilder> _useNamingConvention;
            
            public TestContext(Func<DbContextOptionsBuilder, DbContextOptionsBuilder> useNamingConvention)
                => _useNamingConvention = useNamingConvention;

            public DbSet<SimpleBlog> Blog { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<SimpleBlog>().OwnsOne(p => p.OwnedStatistics);

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => _useNamingConvention(optionsBuilder.UseInMemoryDatabase("test"));
        }

        public class SimpleBlog
        {
            public int Id { get; set; }
            public string FullName { get; set; }

            public OwnedStatistics OwnedStatistics { get; set; }
        }

        public class OwnedStatistics
        {
            public int SomeStatistic { get; set; }
        }
    }
}
