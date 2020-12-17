using System;
using System.Globalization;
using System.Linq;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;
// ReSharper disable UnusedMember.Global

namespace EFCore.NamingConventions.Test
{
    public class NameRewritingConventionTest
    {
        [Fact]
        public void Table_name()
        {
            var entityType = BuildEntityType("SimpleBlog", _ => {});
            Assert.Equal("simple_blog", entityType.GetTableName());
        }

        [Fact]
        public void Column_name()
        {
            var entityType = BuildEntityType("SimpleBlog", e => e.Property<int>("SimpleBlogId"));

            Assert.Equal("simple_blog_id", entityType.FindProperty("SimpleBlogId")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void Column_name_on_view()
        {
            var entityType = BuildEntityType("SimpleBlog", e =>
            {
                e.ToTable("SimpleBlogTable");
                e.ToView("SimpleBlogView");
                e.ToFunction("SimpleBlogFunction");
                e.Property<int>("SimpleBlogId");
            });

            foreach (var type in new[] { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function })
            {
                Assert.Equal("simple_blog_id", entityType.FindProperty("SimpleBlogId")
                    .GetColumnName(StoreObjectIdentifier.Create(entityType, type)!.Value));
            }
        }

        [Fact]
        public void Column_name_turkish_culture()
        {
            var entityType = BuildEntityType(
                "SimpleBlog",
                e => e.Property<int>("SimpleBlogId"),
                CultureInfo.CreateSpecificCulture("tr-TR"));

            Assert.Equal("simple_blog_ıd", entityType.FindProperty("SimpleBlogId")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void Column_name_invariant_culture()
        {
            var entityType = BuildEntityType(
                "SimpleBlog",
                e => e.Property<int>("SimpleBlogId"),
                CultureInfo.InvariantCulture);

            Assert.Equal("simple_blog_id", entityType.FindProperty("SimpleBlogId")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void Primary_key_name()
        {
            var entityType = BuildEntityType("SimpleBlog", e =>
            {
                e.Property<int>("SimpleBlogId");
                e.HasKey("SimpleBlogId");
            });

            Assert.Equal("pk_simple_blog", entityType.GetKeys().Single(k => k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Alternative_key_name()
        {
            var entityType = BuildEntityType("SimpleBlog", e =>
            {
                e.Property<int>("SimpleBlogId");
                e.Property<int>("SomeAlternativeKey");
                e.HasKey("SimpleBlogId");
                e.HasAlternateKey("SomeAlternativeKey");
            });

            Assert.Equal("ak_simple_blog_some_alternative_key", entityType.GetKeys().Single(k => !k.IsPrimaryKey()).GetName());
        }

        [Fact]
        public void Foreign_key_name()
        {
            var model = BuildModel(b =>
            {
                b.Entity("Blog", e =>
                {
                    e.Property<int>("BlogId");
                    e.HasKey("BlogId");
                    e.HasMany("Post").WithOne("Blog");
                });
                b.Entity("Post", e =>
                {
                    e.Property<int>("PostId");
                    e.Property<int>("BlogId");
                    e.HasKey("PostId");
                });
            });
            var entityType = model.FindEntityType("Post");

            Assert.Equal("fk_post_blog_blog_id", entityType.GetForeignKeys().Single().GetConstraintName());
        }

        [Fact]
        public void Index_name()
        {
            var entityType = BuildEntityType("SimpleBlog", e =>
            {
                e.Property<int>("IndexedProperty");
                e.HasIndex("IndexedProperty");
            });

            Assert.Equal("ix_simple_blog_indexed_property", entityType.GetIndexes().Single().GetDatabaseName());
        }

        [Fact]
        public void Table_splitting()
        {
            var model = BuildModel(b =>
            {
                b.Entity("One", e =>
                {
                    e.ToTable("table");
                    e.Property<int>("Id");
                    e.Property<int>("OneProp");
                    e.Property<int>("Common");

                    e.HasOne("Two").WithOne().HasForeignKey("Two", "Id");
                });

                b.Entity("Two", e =>
                {
                    e.ToTable("table");
                    e.Property<int>("Id");
                    e.Property<int>("TwoProp");
                    e.Property<int>("Common");
                });
            });

            var oneEntityType = model.FindEntityType("One");
            var twoEntityType = model.FindEntityType("Two");

            var table = StoreObjectIdentifier.Create(oneEntityType, StoreObjectType.Table)!.Value;
            Assert.Equal(table, StoreObjectIdentifier.Create(twoEntityType, StoreObjectType.Table));

            Assert.Equal("table", oneEntityType.GetTableName());
            Assert.Equal("one_prop", oneEntityType.FindProperty("OneProp").GetColumnName(table));

            Assert.Equal("table", twoEntityType.GetTableName());
            Assert.Equal("two_prop", twoEntityType.FindProperty("TwoProp").GetColumnName(table));

            var foreignKey = twoEntityType.GetForeignKeys().Single();
            Assert.Same(oneEntityType.FindPrimaryKey(), foreignKey.PrincipalKey);
            Assert.Same(twoEntityType.FindPrimaryKey().Properties.Single(), foreignKey.Properties.Single());
            Assert.Equal(oneEntityType.FindPrimaryKey().GetName(), twoEntityType.FindPrimaryKey().GetName());

            Assert.Equal(
                foreignKey.PrincipalKey.Properties.Single().GetColumnName(table),
                foreignKey.Properties.Single().GetColumnName(table));

            Assert.Empty(oneEntityType.GetForeignKeys());
        }

        #region Owned entities

        [Fact]
        public void Owned_entity_with_table_splitting()
        {
            var model = BuildModel(b =>
            {
                b.Entity("SimpleBlog", e =>
                {
                    e.OwnsOne("OwnedEntity", "Nav", o => o.Property<int>("OwnedProperty"));
                });
            });

            var entityType = model.FindEntityType("OwnedEntity");
            Assert.Equal("pk_simple_blog", entityType.FindPrimaryKey().GetName());
            Assert.Equal("simple_blog", entityType.GetTableName());
            Assert.Equal("owned_property", entityType.FindProperty("OwnedProperty")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void Owned_entity_without_table_splitting()
        {
            var model = BuildModel(b =>
            {
                b.Entity("SimpleBlog", e =>
                {
                    e.Property<int>("SimpleBlogId");
                    e.HasKey("SimpleBlogId");
                    e.OwnsOne("OwnedEntity", "Nav", o =>
                    {
                        o.ToTable("another_table");
                        o.Property<int>("OwnedProperty");
                    });
                });
            });
            var entityType = model.FindEntityType("OwnedEntity");

            Assert.Equal("pk_another_table", entityType.FindPrimaryKey().GetName());
            Assert.Equal("another_table", entityType.GetTableName());
            Assert.Equal("owned_property", entityType.FindProperty("OwnedProperty")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void Owned_entity_with_view_without_table_splitting()
        {
            var model = BuildModel(b =>
            {
                b.Entity("OwnedEntity", e =>
                {
                    e.ToTable("OwnedEntityTable");
                    e.ToView("OwnedEntityView");
                    e.Property<int>("OwnedProperty");
                });
                b.Entity("SimpleBlog", e => e.OwnsOne("OwnedEntity", "Nav"));
            });
            var entityType = model.FindEntityType("OwnedEntity");

            Assert.Equal("OwnedEntityTable", entityType.GetTableName());
            Assert.Equal("OwnedEntityView", entityType.GetViewName());
            Assert.Equal("owned_property", entityType.FindProperty("OwnedProperty")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
            Assert.Equal("owned_property", entityType.FindProperty("OwnedProperty")
                .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.View)!.Value));
        }

        #endregion Owned entities

        #region Inheritance

        [Fact]
        public void TPH()
        {
            var model = BuildModel(b =>
            {
                b.Entity("SimpleBlog", e =>
                {
                    e.Property<int>("SimpleBlogId");
                    e.HasKey("SimpleBlogId");
                });
                b.Entity("FancyBlog", e =>
                {
                    e.HasBaseType("SimpleBlog");
                    e.Property<int>("FancyProperty");
                });
            });

            var simpleBlogEntityType = model.FindEntityType("SimpleBlog");
            Assert.Equal("simple_blog", simpleBlogEntityType.GetTableName());
            Assert.Equal("simple_blog_id", simpleBlogEntityType.FindProperty("SimpleBlogId")
                .GetColumnName(StoreObjectIdentifier.Create(simpleBlogEntityType, StoreObjectType.Table)!.Value));

            var fancyBlogEntityType = model.FindEntityType("FancyBlog");
            Assert.Equal("simple_blog", fancyBlogEntityType.GetTableName());
            Assert.Equal("fancy_property", fancyBlogEntityType.FindProperty("FancyProperty")
                .GetColumnName(StoreObjectIdentifier.Create(fancyBlogEntityType, StoreObjectType.Table)!.Value));
        }

        [Fact]
        public void TPT()
        {
            var model = BuildModel(b =>
            {
                b.Entity("SimpleBlog", e =>
                {
                    e.Property<int>("SimpleBlogId");
                    e.HasKey("SimpleBlogId");
                });
                b.Entity("FancyBlog", e =>
                {
                    e.HasBaseType("SimpleBlog");
                    e.ToTable("fancy_blog");
                    e.Property<int>("FancyProperty");
                });
            });

            var simpleBlogEntityType = model.FindEntityType("SimpleBlog");
            Assert.Equal("simple_blog", simpleBlogEntityType.GetTableName());
            Assert.Equal("simple_blog_id", simpleBlogEntityType.FindProperty("SimpleBlogId")
                .GetColumnName(StoreObjectIdentifier.Create(simpleBlogEntityType, StoreObjectType.Table)!.Value));

            var fancyBlogEntityType = model.FindEntityType("FancyBlog");
            Assert.Equal("fancy_blog", fancyBlogEntityType.GetTableName());
            Assert.Equal("fancy_property", fancyBlogEntityType.FindProperty("FancyProperty")
                .GetColumnName(StoreObjectIdentifier.Create(fancyBlogEntityType, StoreObjectType.Table)!.Value));
        }

        #endregion Inheritance

        #region Support

        private IModel BuildModel(Action<ModelBuilder> buildAction, CultureInfo cultureInfo = null)
        {
            var conventionSet = InMemoryTestHelpers.Instance.CreateConventionSetBuilder().CreateConventionSet();
            ConventionSet.Remove(conventionSet.ModelFinalizedConventions, typeof(ValidatingConvention));

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSnakeCaseNamingConvention(cultureInfo);
            new NamingConventionSetPlugin(optionsBuilder.Options).ModifyConventions(conventionSet);

            var builder = new ModelBuilder(conventionSet);
            buildAction(builder);
            return builder.FinalizeModel();
        }

        private IEntityType BuildEntityType(string entityTypeName, Action<EntityTypeBuilder> buildAction, CultureInfo cultureInfo = null)
            => BuildModel(b => buildAction(b.Entity(entityTypeName)), cultureInfo).GetEntityTypes().Single();

        #endregion
    }
}
