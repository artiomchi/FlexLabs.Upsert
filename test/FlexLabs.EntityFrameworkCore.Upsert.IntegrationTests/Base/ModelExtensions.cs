using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base
{
    public static class ModelExtensions
    {
        public static AndWhichConstraint<ObjectAssertions, Book> MatchModel(this ObjectAssertions assertions, Book expected)
        {
            using var _ = new AssertionScope();
            var result = assertions.BeOfType<Book>();
            result.Subject.Name.Should().Be(expected.Name);
            result.Subject.Genres.Should().Equal(expected.Genres);
            return result;
        }

        public static AndWhichConstraint<ObjectAssertions, Country> MatchModel(this ObjectAssertions assertions, Country expected, bool compareCreated = true, bool compareUpdated = true)
        {
            using var _ = new AssertionScope();
            var result = assertions.BeOfType<Country>();
            result.Subject.ISO.Should().Be(expected.ISO);
            result.Subject.Name.Should().Be(expected.Name);
            if (compareCreated)
                result.Subject.Created.Should().Be(expected.Created);
            if (compareUpdated)
                result.Subject.Updated.Should().Be(expected.Updated);
            return result;
        }

        public static AndWhichConstraint<ObjectAssertions, PageVisit> MatchModel(this ObjectAssertions assertions, PageVisit expected, bool compareFirstVisit = true, int? expectedVisits = null)
        {
            using var _ = new AssertionScope();
            var result = assertions.BeOfType<PageVisit>();
            result.Subject.UserID.Should().Be(expected.UserID);
            result.Subject.Date.Should().Be(expected.Date);
            result.Subject.Visits.Should().Be(expectedVisits ?? expected.Visits);
            if (compareFirstVisit)
                result.Subject.FirstVisit.Should().Be(expected.FirstVisit);
            result.Subject.LastVisit.Should().Be(expected.LastVisit);
            return result;
        }

        public static AndWhichConstraint<ObjectAssertions, Status> MatchModel(this ObjectAssertions assertions, Status expected)
        {
            using var _ = new AssertionScope();
            var result = assertions.BeOfType<Status>();
            result.Subject.Name.Should().Be(expected.Name);
            result.Subject.LastChecked.Should().Be(expected.LastChecked);
            return result;
        }

        public static AndWhichConstraint<ObjectAssertions, TestEntity> MatchModel(this ObjectAssertions assertions, TestEntity expected, int? num1 = null, int? num2 = null, int? numNullable = null, string text1 = null, string text2 = null)
        {
            using var _ = new AssertionScope();
            var result = assertions.BeOfType<TestEntity>();
            result.Subject.Num1.Should().Be(num1 ?? expected.Num1);
            result.Subject.Num2.Should().Be(num2 ?? expected.Num2);
            result.Subject.NumNullable1.Should().Be(numNullable ?? expected.NumNullable1);
            result.Subject.Text1.Should().Be(text1 ?? expected.Text1);
            result.Subject.Text2.Should().Be(text2 ?? expected.Text2);
            return result;
        }
    }
}
