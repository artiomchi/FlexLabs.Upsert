using System.Text.Json;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

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

    readonly ParentComplex _dbParentComplex = new()
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

    /// <summary>A fully populated JSON document (required, renamed, nested and list members) used as the "incoming" value.</summary>
    private static CompanyMeta SampleCompanyMeta() => new()
    {
        Required = "required-value",
        JsonOverride = "col with [JsonPropertyName]",
        Nested = new CompanyNestedMeta
        {
            Title = "I'm a nested json",
        },
        Properties =
        [
            new CompanyMetaValue { Key = "foo", Value = "bar" },
            new CompanyMetaValue { Key = "cat", Value = "dog" },
        ],
    };

    #region Owned Entities

    [Fact]
    public virtual void Upsert_Owned_Entity()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

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
            parent => parent.Should().BeEquivalentTo(newParent));
    }

    [Fact]
    public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Direct_Mapping()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

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
            parent => parent.Should().BeEquivalentTo(new Parent
            {
                ID = newParent.ID,
                Counter = 1,
                Child = newParent.Child, // everything under Child comes from the incoming row
            }));
    }

    [Fact]
    public virtual void Upsert_Owned_Entity_WhenMatched_Nested_Owned_Direct_Mapping()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

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
            parent => parent.Should().BeEquivalentTo(new Parent
            {
                ID = newParent.ID,
                Counter = 1,
                Child = new Child
                {
                    ChildName = _dbParent.Child.ChildName, // not updated
                    Age = _dbParent.Child.Age, // not updated
                    SubChild = newParent.Child.SubChild, // updated
                },
            }));
    }

    [Fact]
    public virtual void Upsert_Owned_Entity_WhenMatched_Owned_Partial_Mapping()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

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
                    SubChild = new SubChild
                    {
                        Age = b.Child.SubChild.Age,
                    },
                }
            })
            .Run();

        dbContext.Parents.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(new Parent
            {
                ID = newParent.ID,
                Counter = 1,
                Child = new Child
                {
                    ChildName = newParent.Child.ChildName, // updated
                    Age = _dbParent.Child.Age, // not updated
                    SubChild = new SubChild
                    {
                        SubChildName = _dbParent.Child.SubChild.SubChildName, // not updated
                        Age = newParent.Child.SubChild.Age, // updated
                    },
                },
            }));
    }

    [Fact]
    public virtual void Upsert_Owned_Entity_NoUpdate()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

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
            parent => parent.Should().BeEquivalentTo(_dbParent));
    }

    #endregion

    #region Owned Json Entities

    [Fact]
    public virtual void Upsert_OwnedJson_Entity()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

        var company = new CompanyOwnedJson
        {
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        dbContext.CompanyOwnedJson.Upsert(company)
            .On(p => p.Id)
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
    public virtual void Upsert_OwnedJson_Entity_WhenMatched()
    {
        Require(Capability.OwnedEntities);

        var company1 = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
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
        Require(Capability.OwnedEntities);

        var company1 = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
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
        Require(Capability.OwnedEntities);

        var company1 = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
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
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

        var company = new CompanyOwnedJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
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

    #endregion

    #region Complex Properties

    [Fact]
    public virtual void Upsert_Complex_Property()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
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

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(newParent));
    }

    [Fact]
    public virtual void Upsert_Complex_Property_Null()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
        {
            ID = 1,
            Child = null,
            Counter = 3,
        };

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(newParent));
    }

    [Fact]
    public virtual void Upsert_Complex_Property_WhenMatched_Complex_Direct_Mapping()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
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

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .WhenMatched((a, b) => new ParentComplex
            {
                Counter = b.Counter + 1,
                Child = b.Child, // complex direct mapping - should expand to all columns including sub nested.
            })
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(new ParentComplex
            {
                ID = newParent.ID,
                Counter = 1,
                Child = newParent.Child, // everything under Child comes from the incoming row
            }));
    }

    [Fact]
    public virtual void Upsert_Complex_Property_WhenMatched_Nested_Complex_Direct_Mapping()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
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

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .WhenMatched((a, b) => new ParentComplex
            {
                Counter = b.Counter + 1,
                Child = new Child
                {
                    SubChild = b.Child.SubChild, // nested complex direct mapping - should expand to all columns.
                }
            })
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(new ParentComplex
            {
                ID = newParent.ID,
                Counter = 1,
                Child = new Child
                {
                    ChildName = _dbParent.Child.ChildName, // not updated
                    Age = _dbParent.Child.Age, // not updated
                    SubChild = newParent.Child.SubChild, // updated
                },
            }));
    }

    [Fact]
    public virtual void Upsert_Complex_Property_WhenMatched_Complex_Partial_Mapping()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
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

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .WhenMatched((a, b) => new ParentComplex
            {
                Counter = b.Counter + 1,
                Child = new Child
                {
                    ChildName = b.Child.ChildName,
                    SubChild = new SubChild
                    {
                        Age = b.Child.SubChild.Age,
                    },
                }
            })
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(new ParentComplex
            {
                ID = newParent.ID,
                Counter = 1,
                Child = new Child
                {
                    ChildName = newParent.Child.ChildName, // updated
                    Age = _dbParent.Child.Age, // not updated
                    SubChild = new SubChild
                    {
                        SubChildName = _dbParent.Child.SubChild.SubChildName, // not updated
                        Age = newParent.Child.SubChild.Age, // updated
                    },
                },
            }));
    }

    [Fact]
    public virtual void Upsert_Complex_Property_NoUpdate()
    {
        Require(Capability.ComplexProperties);

        using var dbContext = Arrange();

        var newParent = new ParentComplex
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

        dbContext.ParentComplexes.Upsert(newParent)
            .On(p => p.ID)
            .NoUpdate()
            .Run();

        dbContext.ParentComplexes.OrderBy(p => p.ID).Should().SatisfyRespectively(
            parent => parent.Should().BeEquivalentTo(_dbParentComplex));
    }

    #endregion

    #region Complex Json Properties

    [Fact]
    public virtual void Upsert_ComplexJson_Entity()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

        var company = new CompanyComplexJson
        {
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        dbContext.CompanyComplexJson.Upsert(company)
            .On(p => p.Id)
            .Run();

        dbContext.CompanyComplexJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
            entity =>
            {
                var expected = JsonSerializer.Serialize(company);
                var actual = JsonSerializer.Serialize(entity);
                actual.Should().Be(expected);
            });
    }

    [Fact]
    public virtual void Upsert_ComplexJson_Entity_WhenMatched()
    {
        Require(Capability.OwnedEntities);

        var company1 = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        dbContext.CompanyComplexJson.Upsert(company)
            .On(p => p.Id)
            .WhenMatched((a, b) => new CompanyComplexJson
            {
                Name = b.Name,
                Meta = b.Meta, // assigning a JSON is supported.
            })
            .Run();

        dbContext.CompanyComplexJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
            entity =>
            {
                var expected = JsonSerializer.Serialize(company);
                var actual = JsonSerializer.Serialize(entity);
                actual.Should().Be(expected);
            });
    }

    [Fact]
    public virtual void Upsert_ComplexJson_Entity_WhenMatched_Json_Member_Access_Error()
    {
        Require(Capability.OwnedEntities);

        var company1 = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        var action = void () => dbContext.CompanyComplexJson.Upsert(company)
            .On(p => p.Id)
            .WhenMatched((a, b) => new CompanyComplexJson
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
    public virtual void Upsert_ComplexJson_Entity_WhenMatched_Json_Member_Bind_Error()
    {
        Require(Capability.OwnedEntities);

        var company1 = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company Default",
            Meta = new CompanyMeta
            {
                Required = "default-required-value",
            }
        };

        using var dbContext = Arrange(company1);

        var company = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        var action = void () => dbContext.CompanyComplexJson.Upsert(company)
            .On(p => p.Id)
            .WhenMatched((a, b) => new CompanyComplexJson
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
    public virtual void Upsert_ComplexJson_Entity_NoUpdate()
    {
        Require(Capability.OwnedEntities);

        using var dbContext = Arrange();

        var company = new CompanyComplexJson
        {
            Id = 1,
            Name = "Company 1",
            Meta = SampleCompanyMeta(),
        };

        dbContext.CompanyComplexJson.Upsert(company)
            .On(p => p.Id)
            .NoUpdate()
            .Run();

        dbContext.CompanyComplexJson.OrderBy(p => p.Id).Should().SatisfyRespectively(
            entity =>
            {
                var expected = JsonSerializer.Serialize(company);
                var actual = JsonSerializer.Serialize(entity);
                actual.Should().Be(expected);
            });
    }

    #endregion
}
