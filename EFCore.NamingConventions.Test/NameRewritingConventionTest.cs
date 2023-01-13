using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using EFCore.NamingConventions.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EFCore.NamingConventions.Test;

public class NameRewritingConventionTest
{
    [Fact]
    public void Table()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>());
        Assert.Equal("sample_entity", entityType.GetTableName());
    }

    [Fact]
    public void Column()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>());
        Assert.Equal("sample_entity_id", entityType.FindProperty(nameof(SampleEntity.SampleEntityId))!
            .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void Column_with_turkish_culture()
    {
        var entityType = BuildEntityType(
            b => b.Entity<SampleEntity>(),
            CultureInfo.CreateSpecificCulture("tr-TR"));
        Assert.Equal("sample_entity_ıd", entityType.FindProperty(nameof(SampleEntity.SampleEntityId))!
            .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void Column_with_invariant_culture()
    {
        var entityType = BuildEntityType(
            b => b.Entity<SampleEntity>(),
            CultureInfo.InvariantCulture);
        Assert.Equal("sample_entity_id", entityType.FindProperty(nameof(SampleEntity.SampleEntityId))!
            .GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void Column_on_view()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>(
            e =>
            {
                e.ToTable("SimpleBlogTable");
                e.ToView("SimpleBlogView");
                e.ToFunction("SimpleBlogFunction");
            }));

        foreach (var type in new[] { StoreObjectType.Table, StoreObjectType.View, StoreObjectType.Function })
        {
            Assert.Equal("sample_entity_id", entityType.FindProperty(nameof(SampleEntity.SampleEntityId))!
                .GetColumnName(StoreObjectIdentifier.Create(entityType, type)!.Value));
        }
    }

    [Fact]
    public void Primary_key()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>());
        Assert.Equal("pk_sample_entity", entityType.FindPrimaryKey()!.GetName());
    }

    [Fact]
    public void Primary_key_on_table_with_explicit_name()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>().ToTable("some_explicit_name"));

        Assert.Equal("pk_some_explicit_name", entityType.FindPrimaryKey()!.GetName());
    }

    [Fact]
    public void Alternative_key()
    {
        var entityType = BuildEntityType(
            b => b.Entity<SampleEntity>(
                e =>
                {
                    e.Property<int>("SomeAlternateKey");
                    e.HasAlternateKey("SomeAlternateKey");
                }));
        Assert.Equal("ak_sample_entity_some_alternate_key", entityType.GetKeys().Single(k => !k.IsPrimaryKey()).GetName());
    }

    [Fact]
    public void Foreign_key()
    {
        var model = BuildModel(b => b.Entity<Blog>());
        var entityType = model.FindEntityType(typeof(Post))!;
        Assert.Equal("fk_post_blog_blog_id", entityType.GetForeignKeys().Single().GetConstraintName());
    }

    [Fact]
    public void Index()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>().HasIndex(s => s.SomeProperty));
        Assert.Equal("ix_sample_entity_some_property", entityType.GetIndexes().Single().GetDatabaseName());
    }

    [Fact]
    public void TPH()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Parent>();
            b.Entity<Child>();
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(Child))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("parent", childEntityType.GetTableName());
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(Child.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        Assert.Same(parentEntityType.FindPrimaryKey(), childEntityType.FindPrimaryKey());
    }

    [Fact]
    public void TPT()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Parent>().ToTable("parent");
            b.Entity<Child>().ToTable("child");
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(Child))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("child", childEntityType.GetTableName());
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(Child.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        var primaryKey = parentEntityType.FindPrimaryKey()!;
        Assert.Same(primaryKey, childEntityType.FindPrimaryKey());

        Assert.Equal("PK_parent", primaryKey.GetName());

        // For the following, see #112
        var parentStoreObjectIdentifier = StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value;
        var childStoreObjectIdentifier = StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal("PK_parent", primaryKey.GetName(parentStoreObjectIdentifier));
        Assert.Equal("PK_child", primaryKey.GetName(childStoreObjectIdentifier));
    }

    [Fact]
    public void TPT_reversed_configuration()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Child>().ToTable("child");
            b.Entity<Parent>().ToTable("parent");
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(Child))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("child", childEntityType.GetTableName());
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(Child.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        var primaryKey = parentEntityType.FindPrimaryKey()!;
        Assert.Same(primaryKey, childEntityType.FindPrimaryKey());

        Assert.Equal("PK_parent", primaryKey.GetName());

        // For the following, see #112
        var parentStoreObjectIdentifier = StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value;
        var childStoreObjectIdentifier = StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal("PK_parent", primaryKey.GetName(parentStoreObjectIdentifier));
        Assert.Equal("PK_child", primaryKey.GetName(childStoreObjectIdentifier));
    }

    [Fact]
    public void TPH_with_owned()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Parent>();
            b.Entity<ChildWithOwned>().OwnsOne(c => c.Owned);
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(ChildWithOwned))!;
        var ownedEntityType = model.FindEntityType(typeof(Owned))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("parent", childEntityType.GetTableName());
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(Child.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        Assert.Same(parentEntityType.FindPrimaryKey(), childEntityType.FindPrimaryKey());

        Assert.Equal("parent", ownedEntityType.GetTableName());
        Assert.Equal("owned_owned_property", ownedEntityType.FindProperty(nameof(Owned.OwnedProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void TPT_with_owned()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Parent>().ToTable("parent");
            b.Entity<ChildWithOwned>(
                e =>
                {
                    e.ToTable("child");
                    e.OwnsOne(c => c.Owned);
                });
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(ChildWithOwned))!;
        var ownedEntityType = model.FindEntityType(typeof(Owned))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("child", childEntityType.GetTableName());
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(ChildWithOwned.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        var parentKey = parentEntityType.FindPrimaryKey()!;
        var childKey = childEntityType.FindPrimaryKey()!;

        Assert.Equal("PK_parent", parentKey.GetName());
        Assert.Equal("PK_parent", childKey.GetName());

        Assert.Equal("child", ownedEntityType.GetTableName());
        Assert.Equal("owned_owned_property", ownedEntityType.FindProperty(nameof(Owned.OwnedProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void TPC()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Parent>().UseTpcMappingStrategy();
            b.Entity<Child>();
        });

        var parentEntityType = model.FindEntityType(typeof(Parent))!;
        var childEntityType = model.FindEntityType(typeof(Child))!;

        Assert.Equal("parent", parentEntityType.GetTableName());
        Assert.Equal("id", parentEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", parentEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value));

        Assert.Equal("child", childEntityType.GetTableName());
        Assert.Equal("id", childEntityType.FindProperty(nameof(Parent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", childEntityType.FindProperty(nameof(Parent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(Child.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        var primaryKey = parentEntityType.FindPrimaryKey()!;
        Assert.Same(primaryKey, childEntityType.FindPrimaryKey());

        Assert.Equal("PK_parent", primaryKey.GetName());

        // For the following, see #112
        var parentStoreObjectIdentifier = StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table)!.Value;
        var childStoreObjectIdentifier = StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal("PK_parent", primaryKey.GetName(parentStoreObjectIdentifier));
        Assert.Equal("PK_child", primaryKey.GetName(childStoreObjectIdentifier));
    }

    [Fact]
    public void TPC_with_abstract_parent()
    {
        var model = BuildModel(b =>
        {
            b.Entity<AbstractParent>().UseTpcMappingStrategy();
            b.Entity<ChildOfAbstract>();
        });

        var parentEntityType = model.FindEntityType(typeof(AbstractParent))!;
        var childEntityType = model.FindEntityType(typeof(ChildOfAbstract))!;

        Assert.Null(parentEntityType.GetTableName());
        Assert.Null(StoreObjectIdentifier.Create(parentEntityType, StoreObjectType.Table));

        Assert.Equal("child_of_abstract", childEntityType.GetTableName());
        Assert.Equal("id", childEntityType.FindProperty(nameof(AbstractParent.Id))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("parent_property", childEntityType.FindProperty(nameof(AbstractParent.ParentProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));
        Assert.Equal("child_property", childEntityType.FindProperty(nameof(ChildOfAbstract.ChildProperty))!
            .GetColumnName(StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value));

        var primaryKey = parentEntityType.FindPrimaryKey()!;
        Assert.Same(primaryKey, childEntityType.FindPrimaryKey());

        Assert.Null(primaryKey.GetName());

        // For the following, see #112
        var childStoreObjectIdentifier = StoreObjectIdentifier.Create(childEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal("PK_child_of_abstract", primaryKey.GetName(childStoreObjectIdentifier));
    }

    [Fact]
    public void Table_splitting1()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Split1>(
                e =>
                {
                    e.ToTable("split_table");
                    e.HasOne(s1 => s1.S2).WithOne(s2 => s2.S1).HasForeignKey<Split2>(s2 => s2.Id);
                });

            b.Entity<Split2>().ToTable("split_table");
        });

        var split1EntityType = model.FindEntityType(typeof(Split1))!;
        var split2EntityType = model.FindEntityType(typeof(Split2))!;

        var table = StoreObjectIdentifier.Create(split1EntityType, StoreObjectType.Table)!.Value;
        Assert.Equal(table, StoreObjectIdentifier.Create(split2EntityType, StoreObjectType.Table));

        Assert.Equal("split_table", split1EntityType.GetTableName());
        Assert.Equal("one_prop", split1EntityType.FindProperty(nameof(Split1.OneProp))!.GetColumnName(table));

        Assert.Equal("split_table", split2EntityType.GetTableName());
        Assert.Equal("two_prop", split2EntityType.FindProperty(nameof(Split2.TwoProp))!.GetColumnName(table));

        Assert.Equal("common", split1EntityType.FindProperty(nameof(Split1.Common))!.GetColumnName(table));
        Assert.Equal("split2_common", split2EntityType.FindProperty(nameof(Split2.Common))!.GetColumnName(table));

        var foreignKey = split2EntityType.GetForeignKeys().Single();
        Assert.Same(split1EntityType.FindPrimaryKey(), foreignKey.PrincipalKey);
        Assert.Same(split2EntityType.FindPrimaryKey()!.Properties.Single(), foreignKey.Properties.Single());
        Assert.Equal(split1EntityType.FindPrimaryKey()!.GetName(), split2EntityType.FindPrimaryKey()!.GetName());
        Assert.Equal(
            foreignKey.PrincipalKey.Properties.Single().GetColumnName(table),
            foreignKey.Properties.Single().GetColumnName(table));
        Assert.Empty(split1EntityType.GetForeignKeys());
    }

    [Fact]
    public void Table_splitting_and_explicit_owner_table()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Split1>().HasOne(s1 => s1.S2).WithOne(s2 => s2.S1).HasForeignKey<Split2>(s2 => s2.Id);
            b.Entity<Split2>().ToTable("split_table");
            b.Entity<Split1>().ToTable("split_table");
        });

        var split1EntityType = model.FindEntityType(typeof(Split1))!;
        var split2EntityType = model.FindEntityType(typeof(Split2))!;

        var table = StoreObjectIdentifier.Create(split1EntityType, StoreObjectType.Table)!.Value;
        Assert.Equal(table, StoreObjectIdentifier.Create(split2EntityType, StoreObjectType.Table));

        Assert.Equal("split_table", split1EntityType.GetTableName());
        Assert.Equal("one_prop", split1EntityType.FindProperty(nameof(Split1.OneProp))!.GetColumnName(table));

        Assert.Equal("split_table", split2EntityType.GetTableName());
        Assert.Equal("two_prop", split2EntityType.FindProperty(nameof(Split2.TwoProp))!.GetColumnName(table));

        Assert.Equal("common", split1EntityType.FindProperty(nameof(Split1.Common))!.GetColumnName(table));
        Assert.Equal("split2_common", split2EntityType.FindProperty(nameof(Split2.Common))!.GetColumnName(table));

        var foreignKey = split2EntityType.GetForeignKeys().Single();
        Assert.Same(split1EntityType.FindPrimaryKey(), foreignKey.PrincipalKey);
        Assert.Same(split2EntityType.FindPrimaryKey()!.Properties.Single(), foreignKey.Properties.Single());
        Assert.Equal(split1EntityType.FindPrimaryKey()!.GetName(), split2EntityType.FindPrimaryKey()!.GetName());
        Assert.Equal(
            foreignKey.PrincipalKey.Properties.Single().GetColumnName(table),
            foreignKey.Properties.Single().GetColumnName(table));
        Assert.Empty(split1EntityType.GetForeignKeys());
    }

    [Fact]
    public void Owned_entity_with_table_splitting()
    {
        var model = BuildModel(b => b.Entity<Owner>().OwnsOne(o => o.Owned));

        var ownerEntityType = model.FindEntityType(typeof(Owner))!;
        var ownedEntityType = model.FindEntityType(typeof(Owned))!;

        Assert.Equal("owner", ownerEntityType.GetTableName());
        Assert.Equal("owner", ownedEntityType.GetTableName());
        var table = StoreObjectIdentifier.Create(ownerEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal(table, StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value);

        Assert.Equal("owned_owned_property", ownedEntityType.FindProperty(nameof(Owned.OwnedProperty))!.GetColumnName(table));

        var (ownerKey, ownedKey) = (ownerEntityType.FindPrimaryKey()!, ownedEntityType.FindPrimaryKey()!);
        Assert.Equal("pk_owner", ownerKey.GetName());
        Assert.Equal("pk_owner", ownedKey.GetName());
        Assert.Equal("id", ownerKey.Properties.Single().GetColumnName(table));
        Assert.Equal("id", ownedKey.Properties.Single().GetColumnName(table));
    }

    [Fact]
    public void Owned_entity_with_table_splitting_and_explicit_owner_table()
    {
        var model = BuildModel(
            b => b.Entity<Owner>(
                e =>
                {
                    e.OwnsOne(o => o.Owned);
                    e.ToTable("destination_table");
                }));

        var ownerEntityType = model.FindEntityType(typeof(Owner))!;
        var ownedEntityType = model.FindEntityType(typeof(Owned))!;

        Assert.Equal("destination_table", ownerEntityType.GetTableName());
        Assert.Equal("destination_table", ownedEntityType.GetTableName());
        var table = StoreObjectIdentifier.Create(ownerEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal(table, StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value);

        Assert.Equal("owned_owned_property", ownedEntityType.FindProperty(nameof(Owned.OwnedProperty))!.GetColumnName(table));

        var (ownerKey, ownedKey) = (ownerEntityType.FindPrimaryKey()!, ownedEntityType.FindPrimaryKey()!);
        Assert.Equal("pk_destination_table", ownerKey.GetName());
        Assert.Equal("pk_destination_table", ownedKey.GetName());
        Assert.Equal("id", ownerKey.Properties.Single().GetColumnName(table));
        Assert.Equal("id", ownedKey.Properties.Single().GetColumnName(table));
    }

    [Fact]
    public void Owned_entity_twice_with_table_splitting_and_explicit_owner_table()
    {
        var model = BuildModel(
            b => b.Entity<Owner>(
                e =>
                {
                    e.OwnsOne("owned1", o => o.Owned);
                    e.OwnsOne("owned2", o => o.Owned2);
                    e.ToTable("destination_table");
                }));

        var ownerEntityType = model.FindEntityType(typeof(Owner))!;
        var owned1EntityType = model.FindEntityType("owned1")!;
        var owned2EntityType = model.FindEntityType("owned2")!;

        Assert.Equal("destination_table", ownerEntityType.GetTableName());
        Assert.Equal("destination_table", owned1EntityType.GetTableName());
        Assert.Equal("destination_table", owned2EntityType.GetTableName());
        var table = StoreObjectIdentifier.Create(ownerEntityType, StoreObjectType.Table)!.Value;
        Assert.Equal(table, StoreObjectIdentifier.Create(owned1EntityType, StoreObjectType.Table)!.Value);
        Assert.Equal(table, StoreObjectIdentifier.Create(owned2EntityType, StoreObjectType.Table)!.Value);

        Assert.Equal("owned_owned_property", owned1EntityType.FindProperty(nameof(Owned.OwnedProperty))!.GetColumnName(table));
        Assert.Equal("owned2_owned_property", owned2EntityType.FindProperty(nameof(Owned.OwnedProperty))!.GetColumnName(table));

        var (ownerKey, owned1Key, owned2Key) =
            (ownerEntityType.FindPrimaryKey()!, owned1EntityType.FindPrimaryKey()!, owned1EntityType.FindPrimaryKey()!);
        Assert.Equal("pk_destination_table", ownerKey.GetName());
        Assert.Equal("pk_destination_table", owned1Key.GetName());
        Assert.Equal("pk_destination_table", owned2Key.GetName());
        Assert.Equal("id", ownerKey.Properties.Single().GetColumnName(table));
        Assert.Equal("id", owned1Key.Properties.Single().GetColumnName(table));
        Assert.Equal("id", owned2Key.Properties.Single().GetColumnName(table));
    }

    [Fact]
    public void Owned_entity_without_table_splitting()
    {
        var model = BuildModel(b =>
            b.Entity<Owner>().OwnsOne(o => o.Owned).ToTable("another_table"));

        var ownedEntityType = model.FindEntityType(typeof(Owned))!;

        Assert.Equal("pk_another_table", ownedEntityType.FindPrimaryKey()!.GetName());
        Assert.Equal("another_table", ownedEntityType.GetTableName());
        Assert.Equal("owned_property", ownedEntityType.FindProperty("OwnedProperty")!
            .GetColumnName(StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void Owned_entity_withs_OwnsMany()
    {
        var model = BuildModel(mb => mb.Entity<Blog>().OwnsMany(b => b.Posts));
        var ownedEntityType = model.FindEntityType(typeof(Post))!;

        Assert.Equal("post", ownedEntityType.GetTableName());
        Assert.Equal("pk_post", ownedEntityType.FindPrimaryKey()!.GetName());
        Assert.Equal("post_title", ownedEntityType.FindProperty("PostTitle")!
            .GetColumnName(StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table)!.Value));
    }

    [Fact]
    public void Not_mapped_to_table()
    {
        var entityType = BuildEntityType(b => b.Entity<SampleEntity>().ToSqlQuery("SELECT foobar"));

        Assert.Null(entityType.GetTableName());
    }

    [Fact]
    public void Foreign_key_without_name_because_over_view()
    {
        var model = BuildModel(b =>
        {
            b.Entity<Blog>();
            b.Entity<Post>().ToView("posts_view");
            b.Entity<Post>().HasOne(x => x.Blog).WithMany().HasForeignKey(x => x.BlogId);
        });

        var postEntityType = model.FindEntityType(typeof(Post))!;
        Assert.Null(postEntityType.GetTableName());
        Assert.Collection(
            postEntityType.GetForeignKeys().Select(fk => fk.GetConstraintName()),
            Assert.Null,
            Assert.Null);
    }

    private IEntityType BuildEntityType(Action<ModelBuilder> builderAction, CultureInfo? culture = null)
        => BuildModel(builderAction, culture).GetEntityTypes().Single();

    private IModel BuildModel(Action<ModelBuilder> builderAction, CultureInfo? culture = null)
    {
        var conventionSet = SqliteTestHelpers
            .Instance
            .CreateContextServices()
            .GetRequiredService<IConventionSetBuilder>()
            .CreateConventionSet();

        var optionsBuilder = new DbContextOptionsBuilder();
        SqliteTestHelpers.Instance.UseProviderOptions(optionsBuilder);
        optionsBuilder.UseSnakeCaseNamingConvention(culture);
        var plugin = new NamingConventionSetPlugin(optionsBuilder.Options);
        plugin.ModifyConventions(conventionSet);

        var modelBuilder = new ModelBuilder(conventionSet);
        builderAction(modelBuilder);
        return modelBuilder.FinalizeModel();
    }

    public class SampleEntity
    {
        public int SampleEntityId { get; set; }
        public int SomeProperty { get; set; }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public required List<Post> Posts { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public required Blog Blog { get; set; }
        public int BlogId { get; set; }
        public required string PostTitle { get; set; }
    }

    public class Parent
    {
        public int Id { get; set; }
        public int ParentProperty { get; set; }
    }

    public class Child : Parent
    {
        public int ChildProperty { get; set; }
    }

    public abstract class AbstractParent
    {
        public int Id { get; set; }
        public int ParentProperty { get; set; }
    }

    public class ChildOfAbstract : AbstractParent
    {
        public int ChildProperty { get; set; }
    }

    public class ChildWithOwned : Parent
    {
        public int ChildProperty { get; set; }
        public required Owned Owned { get; set; }
    }

    public class Split1
    {
        public int Id { get; set; }
        public int OneProp { get; set; }
        public int Common { get; set; }

        public required Split2 S2 { get; set; }
    }

    public class Split2
    {
        public int Id { get; set; }
        public int TwoProp { get; set; }
        public int Common { get; set; }

        public required Split1 S1 { get; set; }
    }

    public class Owner
    {
        public int Id { get; set; }
        public int OwnerProperty { get; set; }
        public required Owned Owned { get; set; }
        [NotMapped]
        public required Owned Owned2 { get; set; }
    }

    public class Owned
    {
        public int OwnedProperty { get; set; }
    }
}
