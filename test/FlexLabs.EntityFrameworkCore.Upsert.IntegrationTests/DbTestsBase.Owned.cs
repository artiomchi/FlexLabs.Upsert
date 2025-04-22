using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF
{
    public abstract partial class DbTestsBase
    {
        [SkippableFact]
        public virtual void Upsert_Owned_Entity()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newParent = new Parent
            {
                ID = 1,
                Child = new Child
                {
                    ChildName = "Someone else",
                    SubChild = new SubChild
                    {
                        SubChildName = "sub child",
                    }
                },
                Counter = 3,
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .Run();

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    Assert.Equal(newParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.Equal(newParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.Equal(newParent.Counter, parent.Counter);
                    Assert.Equal(3, parent.Counter);
                });
        }

        [SkippableFact]
        public virtual void Upsert_Owned_Entity_WhenMatched()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newParent = new Parent
            {
                ID = 1,
                Child = new Child
                {
                    ChildName = "Someone else",
                    SubChild = new SubChild
                    {
                        SubChildName = "sub child",
                    }
                },
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .WhenMatched((a, b) => new Parent
                {
                    Counter = b.Counter + 1,
                    //Child = new Child
                    //{
                    //    ChildName = "Not me",
                    //    // TODO expression not working:
                    //    SubChild = new SubChild {
                    //        SubChildName = "someone else"
                    //    }
                    //},
                    // TODO expression not working:
                    Child = new Child
                    {
                        ChildName = b.Child.ChildName,
                        SubChild = a.Child.SubChild,
                    }
                })
                .Run();

            // TODO test assign owned child with itself - should expand to all columns...
            // TODO test assign owned child with itself - should also expand nested owned children...
            // TODO test individual member mapping...

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    Assert.Equal(newParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.NotEqual(newParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.NotEqual(newParent.Counter, parent.Counter);
                    Assert.Equal(1, parent.Counter);
                    //Assert.Equal("Not me", parent.Child?.ChildName);
                    //Assert.Equal("someone else", parent.Child?.SubChild?.SubChildName);
                    Assert.Equal(_dbParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                });
        }

        [SkippableFact]
        public virtual void Upsert_Owned_Entity_NoUpdate()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newParent = new Parent
            {
                ID = 1,
                Child = new Child
                {
                    ChildName = "Someone else",
                    SubChild = new SubChild
                    {
                        SubChildName = "sub child",
                    }
                },
                Counter = 3,
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .NoUpdate()
                .Run();

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    Assert.NotEqual(newParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.NotEqual(newParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.NotEqual(newParent.Counter, parent.Counter);
                    Assert.Equal(0, parent.Counter);
                });
        }


        [SkippableFact]
        public virtual void Upsert_OwnedJson_Entity()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var company = new CompanyOwnedJson
            {
                Name = "Company 1",
                Meta = new CompanyMeta
                {
                    Required = "required-value",
                    JsonOverride = "col with [JsonPropertyName]",
                    ColumnOverride = "col with [Column(_name_)]",
                    Nested = new CompanyNestedMeta
                    {
                        Title = "I'm a nested json",
                    },
                    Properties = [
                        new CompanyMetaValue {
                            Key = "foo",
                            Value = "bar",
                        },
                        new CompanyMetaValue {
                            Key = "cat",
                            Value = "dog",
                        }
                    ],
                }
            };

            dbContext.CompanyOwnedJson.Upsert(company)
                .On(p => p.Id)
                .Run();

            Assert.Collection(dbContext.CompanyOwnedJson.OrderBy(p => p.Id),
                entity => {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    Assert.Equal(expected, actual);
                });
        }

        [SkippableFact]
        public virtual void Upsert_OwnedJson_Entity_WhenMatched()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            var company1 = new CompanyOwnedJson
            {
                Id = 1,
                Name = "Company Default",
                Meta = new CompanyMeta
                {
                    Required = "default-required-value",
                }
            };

            ResetDb(company1);

            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var company = new CompanyOwnedJson
            {
                Id = 1,
                Name = "Company 1",
                Meta = new CompanyMeta
                {
                    Required = "required-value",
                    JsonOverride = "col with [JsonPropertyName]",
                    ColumnOverride = "col with [Column(_name_)]",
                    Nested = new CompanyNestedMeta
                    {
                        Title = "I'm a nested json",
                    },
                    Properties = [
                        new CompanyMetaValue {
                            Key = "foo",
                            Value = "bar",
                        },
                        new CompanyMetaValue {
                            Key = "cat",
                            Value = "dog",
                        }
                    ],
                }
            };

            dbContext.CompanyOwnedJson.Upsert(company)
                .On(p => p.Id)
                .WhenMatched((a, b) => new CompanyOwnedJson
                {
                    Name = b.Name,
                    Meta = b.Meta,
                    // NOTE: expression not working: translating this to SQL is hard to get right.
                    //Meta = new CompanyMeta {
                    //    Required = b.Meta.Required,
                    //    Nested = new CompanyNestedMeta {
                    //        Title = a.Meta.Nested.Title,
                    //    }
                    //}
                })
                .Run();

            Assert.Collection(dbContext.CompanyOwnedJson.OrderBy(p => p.Id),
                entity =>
                {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    Assert.Equal(expected, actual);
                });
        }

        [SkippableFact]
        public virtual void Upsert_OwnedJson_Entity_NoUpdate()
        {
            Skip.If(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var company = new CompanyOwnedJson
            {
                Id = 1,
                Name = "Company 1",
                Meta = new CompanyMeta
                {
                    Required = "required-value",
                    JsonOverride = "col with [JsonPropertyName]",
                    ColumnOverride = "col with [Column(_name_)]",
                    Nested = new CompanyNestedMeta
                    {
                        Title = "I'm a nested json",
                    },
                    Properties = [
                        new CompanyMetaValue {
                            Key = "foo",
                            Value = "bar",
                        },
                        new CompanyMetaValue {
                            Key = "cat",
                            Value = "dog",
                        }
                    ],
                }
            };

            dbContext.CompanyOwnedJson.Upsert(company)
                .On(p => p.Id)
                .NoUpdate()
                .Run();

            Assert.Collection(dbContext.CompanyOwnedJson.OrderBy(p => p.Id),
                entity =>
                {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    Assert.Equal(expected, actual);
                });
        }
    }
}
