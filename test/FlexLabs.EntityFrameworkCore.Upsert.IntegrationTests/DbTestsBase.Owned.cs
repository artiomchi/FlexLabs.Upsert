using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF
{
    public abstract partial class DbTestsBase
    {
        readonly Parent _dbParent = new()
        {
            ID = 1,
            Child = new Child
            {
                ChildName = "Child",
                Age = 1,
                SubChild = new SubChild
                {
                    SubChildName = "SubChild",
                    Age = 1,
                }
            },
        };

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
                        SubChildName = "SubChild foobar",
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
        public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Direct_Mapping()
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
                    Age = 10,
                    SubChild = new SubChild
                    {
                        SubChildName = "SubChild foobar",
                        Age = 10,
                    }
                },
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .WhenMatched((a, b) => new Parent
                {
                    Counter = b.Counter + 1,
                    Child = b.Child, // owned direct mapping - should expand to all columns including sub nested.
                })
                .Run();

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    // child props are updated:
                    Assert.Equal(newParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.Equal(newParent.Child.Age, parent.Child?.Age);
                    // nested child props are updated:
                    Assert.Equal(newParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.Equal(newParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    // nested child props now differ form default:
                    Assert.NotEqual(_dbParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.NotEqual(_dbParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    Assert.Equal(1, parent.Counter);
                });
        }

        [SkippableFact]
        public virtual void Upsert_Owned_Entity_WhenMatched_Nested_Owned_Direct_Mapping()
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
                    Age = 10,
                    SubChild = new SubChild
                    {
                        SubChildName = "SubChild foobar",
                        Age = 10,
                    }
                },
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .WhenMatched((a, b) => new Parent
                {
                    Counter = b.Counter + 1,
                    Child = new Child
                    {
                        SubChild = b.Child.SubChild, // nested owned direct mapping - should expand to all columns.
                    }
                })
                .Run();

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    // child props are NOT updated:
                    Assert.Equal(_dbParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.Equal(_dbParent.Child.Age, parent.Child?.Age);
                    // nested child props are updated:
                    Assert.Equal(newParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.Equal(newParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    // nested child props now differ form default:
                    Assert.NotEqual(_dbParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.NotEqual(_dbParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    Assert.Equal(1, parent.Counter);
                });
        }

        [SkippableFact]
        public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Partial_Mapping()
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
                    Age = 10,
                    SubChild = new SubChild
                    {
                        SubChildName = "SubChild foobar",
                        Age = 10,
                    }
                },
            };

            dbContext.Parents.Upsert(newParent)
                .On(p => p.ID)
                .WhenMatched((a, b) => new Parent
                {
                    Counter = b.Counter + 1,
                    Child = new Child
                    {
                        ChildName = b.Child.ChildName,
                        SubChild = new SubChild {
                            Age = b.Child.SubChild.Age,
                        },
                    }
                })
                .Run();

            Assert.Collection(dbContext.Parents.OrderBy(p => p.ID),
                parent =>
                {
                    Assert.Equal(newParent.ID, parent.ID);
                    // child props: only name is updated:
                    Assert.Equal(newParent.Child.ChildName, parent.Child?.ChildName);
                    Assert.Equal(_dbParent.Child.Age, parent.Child?.Age);
                    // nested child props: only age is updated:
                    Assert.Equal(_dbParent.Child.SubChild.SubChildName, parent.Child?.SubChild?.SubChildName);
                    Assert.Equal(newParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    // nested child age now differ form default:
                    Assert.NotEqual(_dbParent.Child.SubChild.Age, parent.Child?.SubChild?.Age);
                    Assert.Equal(1, parent.Counter);
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
                        SubChildName = "SubChild foobar",
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
