// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class EndToEndTest : IClassFixture<EndToEndTest.EndToEndTestFixture>
    {
        public EndToEndTest(EndToEndTestFixture fixture)
            => Fixture = fixture;

        [Fact]
        public void Table_splitting()
        {
            using var context = CreateContext();

            var split1EntityType = context.Model.FindEntityType(typeof(Split1));
            var split2EntityType = context.Model.FindEntityType(typeof(Split2));

            var table = StoreObjectIdentifier.Create(split1EntityType, StoreObjectType.Table)!.Value;
            Assert.Equal(table, StoreObjectIdentifier.Create(split2EntityType, StoreObjectType.Table));

            Assert.Equal("common", split1EntityType.FindProperty("Common").GetColumnName(table));
            Assert.Equal("split2_common", split2EntityType.FindProperty("Common").GetColumnName(table));

            var split1 = context.Set<Split1>().Include(s1 => s1.Split2).Single();
            Assert.Equal(100, split1.Common);
            Assert.Equal(101, split1.Split2.Common);
        }

        TestContext CreateContext() => Fixture.CreateContext();

        readonly EndToEndTestFixture Fixture;

        public class TestContext : DbContext
        {
            public TestContext(SqliteConnection connection)
                : base(new DbContextOptionsBuilder<TestContext>()
                    .UseSqlite(connection)
                    .UseSnakeCaseNamingConvention()
                    .Options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Split1>(e =>
                {
                    e.ToTable("split");
                    e.HasOne(s1 => s1.Split2).WithOne(s2 => s2.Split1).HasForeignKey<Split2>(s2 => s2.Id);
                    e.HasData(new Split1 { Id = 1, OneProp = 1, Common = 100 });
                });

                modelBuilder.Entity<Split2>(e =>
                {
                    e.ToTable("split");
                    e.HasData(new Split2 { Id = 1, TwoProp = 2, Common = 101 });
                });
            }
        }

        public class Split1
        {
            public int Id { get; set; }
            public int OneProp { get; set; }
            public int Common { get; set; }

            public Split2 Split2 { get; set; }
        }

        public class Split2
        {
            public int Id { get; set; }
            public int TwoProp { get; set; }
            public int Common { get; set; }

            public Split1 Split1 { get; set; }
        }

        public class EndToEndTestFixture : IDisposable
        {
            private readonly SqliteConnection _connection;

            public TestContext CreateContext() => new(_connection);

            public EndToEndTestFixture()
            {
                _connection = new SqliteConnection("Filename=:memory:");
                _connection.Open();
                using var context = new TestContext(_connection);
                context.Database.EnsureCreated();
            }

            public void Dispose() => _connection.Dispose();
        }
    }
}
