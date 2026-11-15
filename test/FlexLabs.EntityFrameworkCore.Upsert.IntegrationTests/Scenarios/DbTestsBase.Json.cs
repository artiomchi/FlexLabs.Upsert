using System.Text.Json;
using System.Text.Json.Nodes;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_JObjectData()
    {
        using var dbContext = Arrange();

        var newJson = new JObjectData
        {
            Data = new JsonObject { ["hello"] = "world" },
        };

        dbContext.JObjectDatas.Upsert(newJson)
            .Run();

        dbContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JsonNode.DeepEquals(newJson.Data, j.Data).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JObject_Update()
    {
        var existingJson = new JObjectData
        {
            Data = new JsonObject { ["hello"] = "world" },
        };

        using (var testContext = Arrange(existingJson))
        {
            testContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JsonNode.DeepEquals(existingJson.Data, j.Data).Should().BeTrue());
        }

        using var dbContext = NewContext();

        var updatedJson = new JObjectData
        {
            Data = new JsonObject { ["welcome"] = "world 2.0" },
        };

        dbContext.JObjectDatas.Upsert(updatedJson)
            .Run();

        dbContext.JObjectDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JsonNode.DeepEquals(updatedJson.Data, j.Data).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData()
    {
        using var dbContext = Arrange();

        var newJson = new JsonData
        {
            Data = JsonSerializer.Serialize(new { hello = "world" }),
        };

        dbContext.JsonDatas.Upsert(newJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JsonNode.DeepEquals(JsonNode.Parse(newJson.Data), JsonNode.Parse(j.Data)).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData_Update()
    {
        var existingJson = new JsonData
        {
            Data = JsonSerializer.Serialize(new { hello = "world" }),
        };

        using (var testContext = Arrange(existingJson))
        {
            testContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JsonNode.DeepEquals(JsonNode.Parse(existingJson.Data), JsonNode.Parse(j.Data)).Should().BeTrue());
        }

        using var dbContext = NewContext();

        var updatedJson = new JsonData
        {
            Data = JsonSerializer.Serialize(new { welcome = "world 2.0" }),
        };

        dbContext.JsonDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => JsonNode.DeepEquals(JsonNode.Parse(updatedJson.Data), JsonNode.Parse(j.Data)).Should().BeTrue());
    }

    [Fact]
    public void Upsert_JsonData_Update_ComplexObject()
    {
        Require(Capability.JsonbChildObject);

        var existingJson = new JsonData
        {
            Data = JsonSerializer.Serialize(new { hello = "world" }),
        };

        using (var testContext = Arrange(existingJson))
        {
            testContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => JsonNode.DeepEquals(JsonNode.Parse(existingJson.Data), JsonNode.Parse(j.Data)).Should().BeTrue());
        }

        using var dbContext = NewContext();

        var timestamp = new DateTime(2021, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var updatedJson = new JsonData
        {
            Child = new ChildObject { Value = "test", Time = timestamp }
        };

        dbContext.JsonDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => j.Child.Time.Should().Be(timestamp));
    }

    [Fact]
    public void Upsert_JsonDocumentData()
    {
        Require(Capability.OwnedJson);

        using var dbContext = Arrange();

        var newJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        dbContext.JsonDocumentDatas.Upsert(newJson)
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => System.Text.Json.JsonSerializer.Serialize(newJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
    }

    [Fact]
    public void Upsert_JsonDocumentData_Update()
    {
        Require(Capability.OwnedJson);

        var existingJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        using (var testContext = Arrange(existingJson))
        {
            testContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => System.Text.Json.JsonSerializer.Serialize(existingJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
        }

        using var dbContext = NewContext();

        var updatedJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { welcome = "world 2.0" }),
        };

        dbContext.JsonDocumentDatas.Upsert(updatedJson)
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => System.Text.Json.JsonSerializer.Serialize(updatedJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
    }

    [Fact]
    public void Upsert_JsonDocumentData_Update_ComplexObject()
    {
        Require(Capability.OwnedJson);

        var existingJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world" }),
        };

        using (var testContext = Arrange(existingJson))
        {
            testContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
                j => System.Text.Json.JsonSerializer.Serialize(existingJson.Data).Should().Be(System.Text.Json.JsonSerializer.Serialize(j.Data)));
        }

        using var dbContext = NewContext();

        var updatedJson = new JsonDocumentData
        {
            Data = System.Text.Json.JsonSerializer.SerializeToDocument(new { hello = "world 2.0" }),
        };

        dbContext.JsonDocumentDatas.Upsert(updatedJson)
            .WhenMatched((a, b) => new JsonDocumentData
            {
                Data = b.Data,
            })
            .Run();

        dbContext.JsonDocumentDatas.OrderBy(c => c.ID).Should().SatisfyRespectively(
            j => j.Data.RootElement.GetProperty("hello").GetString().Should().Be("world 2.0"));
    }
}
