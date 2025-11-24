using System.Linq;
using System.Text.Json;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

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

        [Fact]
        public virtual void Upsert_Owned_Entity()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
                parent =>
                {
                    parent.ID.Should().Be(newParent.ID);
                    parent.Child?.ChildName.Should().Be(newParent.Child.ChildName);
                    parent.Child?.SubChild?.SubChildName.Should().Be(newParent.Child.SubChild.SubChildName);
                    parent.Counter.Should().Be(newParent.Counter);
                    parent.Counter.Should().Be(3);
                });
        }

        [Fact]
        public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Direct_Mapping()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
                parent =>
                {
                    parent.ID.Should().Be(newParent.ID);
                    // child props are updated:
                    parent.Child?.ChildName.Should().Be(newParent.Child.ChildName);
                    parent.Child?.Age.Should().Be(newParent.Child.Age);
                    // nested child props are updated:
                    parent.Child?.SubChild?.SubChildName.Should().Be(newParent.Child.SubChild.SubChildName);
                    parent.Child?.SubChild?.Age.Should().Be(newParent.Child.SubChild.Age);
                    // nested child props now differ form default:
                    parent.Child?.SubChild?.SubChildName.Should().NotBe(_dbParent.Child.SubChild.SubChildName);
                    parent.Child?.SubChild?.Age.Should().NotBe(_dbParent.Child.SubChild.Age);
                    parent.Counter.Should().Be(1);
                });
        }

        [Fact]
        public virtual void Upsert_Owned_Entity_WhenMatched_Nested_Owned_Direct_Mapping()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
                parent =>
                {
                    parent.ID.Should().Be(newParent.ID);
                    // child props are NOT updated:
                    parent.Child?.ChildName.Should().Be(_dbParent.Child.ChildName);
                    parent.Child?.Age.Should().Be(_dbParent.Child.Age);
                    // nested child props are updated:
                    parent.Child?.SubChild?.SubChildName.Should().Be(newParent.Child.SubChild.SubChildName);
                    parent.Child?.SubChild?.Age.Should().Be(newParent.Child.SubChild.Age);
                    // nested child props now differ form default:
                    parent.Child?.SubChild?.SubChildName.Should().NotBe(_dbParent.Child.SubChild.SubChildName);
                    parent.Child?.SubChild?.Age.Should().NotBe(_dbParent.Child.SubChild.Age);
                    parent.Counter.Should().Be(1);
                });
        }

        [Fact]
        public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Partial_Mapping()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
                parent =>
                {
                    parent.ID.Should().Be(newParent.ID);
                    // child props: only name is updated:
                    parent.Child?.ChildName.Should().Be(newParent.Child.ChildName);
                    parent.Child?.Age.Should().Be(_dbParent.Child.Age);
                    // nested child props: only age is updated:
                    parent.Child?.SubChild?.SubChildName.Should().Be(_dbParent.Child.SubChild.SubChildName);
                    parent.Child?.SubChild?.Age.Should().Be(newParent.Child.SubChild.Age);
                    // nested child age now differ form default:
                    parent.Child?.SubChild?.Age.Should().NotBe(_dbParent.Child.SubChild.Age);
                    parent.Counter.Should().Be(1);
                });
        }

        [Fact]
        public virtual void Upsert_Owned_Entity_NoUpdate()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
                parent =>
                {
                    parent.ID.Should().Be(newParent.ID);
                    parent.Child?.ChildName.Should().NotBe(newParent.Child.ChildName);
                    parent.Child?.SubChild?.SubChildName.Should().NotBe(newParent.Child.SubChild.SubChildName);
                    parent.Counter.Should().NotBe(newParent.Counter);
                    parent.Counter.Should().Be(0);
                });
        }


        [Fact]
        public virtual void Upsert_OwnedJson_Entity()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var company = new CompanyOwnedJson
            {
                Name = "Company 1",
                Meta = new CompanyMeta
                {
                    Required = "required-value",
                    JsonOverride = "col with [JsonPropertyName]",
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

            dbContext.CompanyOwnedJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
                entity => {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    actual.Should().Be(expected);
                });
        }

        [Fact]
        public virtual void Upsert_OwnedJson_Entity_WhenMatched()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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
                    Meta = b.Meta, // assigning a JSON is supported.
                })
                .Run();

            dbContext.CompanyOwnedJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
                entity =>
                {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    actual.Should().Be(expected);
                });
        }

        [Fact]
        public virtual void Upsert_OwnedJson_Entity_WhenMatched_Json_Member_Access_Error()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            var action = void () => dbContext.CompanyOwnedJson.Upsert(company)
                .On(p => p.Id)
                .WhenMatched((a, b) => new CompanyOwnedJson
                {
                    Name = b.Name,
                    // NOTE: expression not working: translating this to SQL is hard to get right.
                    Meta = new CompanyMeta
                    {
                        Required = b.Meta.Required, // Accessing deep JSON properties is not supported!
                        Nested = new CompanyNestedMeta
                        {
                            Title = a.Meta.Nested.Title,
                        }
                    }
                })
                .Run();

            action.Should().Throw<UnsupportedExpressionException>()
                .WithMessage("Reading JSON members is not supported. Unsupported Access Expression: b.Meta.Required");
        }

        [Fact]
        public virtual void Upsert_OwnedJson_Entity_WhenMatched_Json_Member_Bind_Error()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            var action = void () => dbContext.CompanyOwnedJson.Upsert(company)
                .On(p => p.Id)
                .WhenMatched((a, b) => new CompanyOwnedJson
                {
                    Name = b.Name,
                    // NOTE: expression not working: translating this to SQL is hard to get right.
                    Meta = new CompanyMeta
                    {
                        Required = "Some Text", // assigning JSON deep properties is not supported!
                        Nested = new CompanyNestedMeta
                        {
                            Title = "Some Title", // assigning JSON deep properties is not supported!
                        }
                    }
                })
                .Run();

            action.Should().Throw<UnsupportedExpressionException>()
                .WithMessage("Modifying JSON members is not supported. Unsupported Expression: new CompanyMeta() {Required = \"Some Text\", Nested = new CompanyNestedMeta() {Title = \"Some Title\"}}");
        }

        [Fact]
        public virtual void Upsert_OwnedJson_Entity_NoUpdate()
        {
            Assert.SkipWhen(_fixture.DbDriver is DbDriver.InMemory, "db doesn't support sql owned entities");

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

            dbContext.CompanyOwnedJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
                entity =>
                {
                    var expected = JsonSerializer.Serialize(company);
                    var actual = JsonSerializer.Serialize(entity);
                    actual.Should().Be(expected);
                });
        }
    }
}
