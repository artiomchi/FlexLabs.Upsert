using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF;

public abstract partial class DbTestsBase
{
    [Fact]
    public void Upsert_Status_Update_AutoMatched_New()
    {
        using var dbContext = Arrange();

        var newStatus = new Status
        {
            ID = 2,
            Name = "Updated",
            LastChecked = _now,
        };

        dbContext.Statuses.Upsert(newStatus).Run();

        dbContext.Statuses.OrderBy(c => c.ID).Should().SatisfyRespectively(
            status => status.Should().MatchModel(_dbStatus),
            status => status.Should().MatchModel(newStatus));
    }

    [Fact]
    public void Upsert_Status_Update_AutoMatched_Existing()
    {
        using var dbContext = Arrange();

        var newStatus = new Status
        {
            ID = _dbStatus.ID,
            Name = "Updated",
            LastChecked = _now,
        };

        dbContext.Statuses.Upsert(newStatus).Run();

        dbContext.Statuses.OrderBy(c => c.ID).Should().SatisfyRespectively(
            status => status.Should().MatchModel(newStatus));
    }

    [Fact]
    public void Upsert_DashedTable()
    {
        using var dbContext = Arrange();

        var newRecord = new DashTable
        {
            DataSet = "test",
            Updated = _now,
        };

        dbContext.DashTable.Upsert(newRecord)
            .On(x => x.DataSet)
            .Run();

        dbContext.DashTable.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r => r.DataSet.Should().Be(newRecord.DataSet));
    }

    [Fact]
    public void Upsert_SchemaTable()
    {
        using var dbContext = Arrange();

        var newRecord = new SchemaTable
        {
            Name = 1,
            Updated = _now,
        };

        dbContext.SchemaTable.Upsert(newRecord)
            .On(x => x.Name)
            .Run();

        dbContext.SchemaTable.OrderBy(c => c.ID).Should().SatisfyRespectively(
            r => r.Name.Should().Be(newRecord.Name));
    }

    [Fact]
    public void Upsert_Book_On_Update()
    {
        using var dbContext = Arrange();

        var newBook = new Book
        {
            Name = _dbBook.Name,
            Genres = new[] { "Fantasy", "Adventure" },
        };

        dbContext.Books.Upsert(newBook)
            .On(b => b.Name)
            .Run();

        dbContext.Books.OrderBy(c => c.ID).Should().SatisfyRespectively(
            book => book.Should().MatchModel(newBook));
    }

    [Fact]
    public void Upsert_Book_On_Insert()
    {
        using var dbContext = Arrange();

        var newBook = new Book
        {
            Name = "The Two Towers",
            Genres = new[] { "Fantasy", "Adventure" },
        };

        dbContext.Books.Upsert(newBook)
            .On(p => p.Name)
            .Run();

        dbContext.Books.OrderBy(c => c.ID).Should().SatisfyRespectively(
            book => book.Should().MatchModel(_dbBook),
            book => book.Should().MatchModel(newBook));
    }
}
