using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace EFCore.NamingConventions.Test
{
    public abstract class RewriterTestBase
    {
        public class TestContext : DbContext
        {
            readonly Func<DbContextOptionsBuilder, CultureInfo, DbContextOptionsBuilder> _useNamingConvention;
            readonly CultureInfo _culture;
            public TestContext(Func<DbContextOptionsBuilder, CultureInfo, DbContextOptionsBuilder> useNamingConvention, CultureInfo culture = null)
            {
                _useNamingConvention = useNamingConvention;
                _culture = culture ?? CultureInfo.InvariantCulture;
            }

            public DbSet<SimpleBlog> Blog { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<SimpleBlog>(e =>
                {
                    e.HasIndex(b => b.FullName);
                    e.OwnsOne(b => b.OwnedStatistics);
                    e.HasAlternateKey(b => b.SomeAlternativeKey);
                });

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => _useNamingConvention(optionsBuilder.UseInMemoryDatabase("test"), _culture);
        }

        public class SimpleBlog
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public int SomeAlternativeKey { get; set; }

            public List<Post> Posts { get; set; }

            public OwnedStatistics OwnedStatistics { get; set; }
        }

        public class Post
        {
            public int Id { get; set; }
            public string FullName { get; set; }

            public int BlogId { get; set; }
            public SimpleBlog Blog { get; set; }
        }

        public class OwnedStatistics
        {
            public int SomeStatistic { get; set; }
        }
    }
}
