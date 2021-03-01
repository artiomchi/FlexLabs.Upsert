using System;
using System.Linq;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests;
using FlexLabs.EntityFrameworkCore.Upsert.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.EF
{
    public abstract class BasicTest
    {
        private readonly DatabaseInitializerFixture _fixture;

        public BasicTest(DatabaseInitializerFixture fixture)
        {
            _fixture = fixture;
        }

        readonly Country _dbCountry = new Country
        {
            Name = "...loading...",
            ISO = "AU",
            Created = new DateTime(1970, 1, 1),
        };
        readonly PageVisit _dbVisitOld = new PageVisit
        {
            UserID = 1,
            Date = DateTime.Today.AddDays(-1),
            Visits = 10,
            FirstVisit = new DateTime(1970, 1, 1),
            LastVisit = new DateTime(1970, 1, 1),
        };
        readonly PageVisit _dbVisit = new PageVisit
        {
            UserID = 1,
            Date = DateTime.Today,
            Visits = 12,
            FirstVisit = new DateTime(1970, 1, 1),
            LastVisit = new DateTime(1970, 1, 1),
        };
        readonly Status _dbStatus = new Status
        {
            ID = 1,
            Name = "Created",
            LastChecked = new DateTime(1970, 1, 1),
        };
        readonly Book _dbBook = new Book
        {
            Name = "The Fellowship of the Ring",
            Genres = new[] { "Fantasy" },
        };
        readonly NullableCompositeKey _nullableKey1 = new NullableCompositeKey
        {
            ID1 = 1,
            ID2 = 2,
            Value = "First",
        };
        readonly NullableCompositeKey _nullableKey2 = new NullableCompositeKey
        {
            ID1 = 1,
            ID2 = null,
            Value = "Second",
        };
        readonly DateTime _now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        readonly int _increment = 8;

        private void ResetDb()
        {
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.RemoveRange(dbContext.Books);
            dbContext.RemoveRange(dbContext.Countries);
            dbContext.RemoveRange(dbContext.DashTable);
            dbContext.RemoveRange(dbContext.GuidKeys);
            dbContext.RemoveRange(dbContext.GuidKeysAutoGen);
            dbContext.RemoveRange(dbContext.JObjectDatas);
            dbContext.RemoveRange(dbContext.JsonDatas);
            dbContext.RemoveRange(dbContext.KeyOnlies);
            dbContext.RemoveRange(dbContext.NullableCompositeKeys);
            dbContext.RemoveRange(dbContext.NullableRequireds);
            dbContext.RemoveRange(dbContext.PageVisits);
            dbContext.RemoveRange(dbContext.SchemaTable);
            dbContext.RemoveRange(dbContext.Statuses);
            dbContext.RemoveRange(dbContext.StringKeys);
            dbContext.RemoveRange(dbContext.StringKeysAutoGen);
            dbContext.RemoveRange(dbContext.TestEntities);

            dbContext.Add(_dbCountry);
            dbContext.Add(_dbVisitOld);
            dbContext.Add(_dbVisit);
            dbContext.Add(_dbStatus);
            dbContext.Add(_dbBook);
            dbContext.Add(_nullableKey1);
            dbContext.Add(_nullableKey2);
            dbContext.SaveChanges();
        }

        private void ResetDb<TEntity>(params TEntity[] seedValue)
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.AddRange(seedValue.Cast<object>());
            dbContext.SaveChanges();
        }

        private static void AssertEqual(PageVisit expected, PageVisit actual)
        {
            Assert.Equal(expected.UserID, actual.UserID);
            Assert.Equal(expected.Date, actual.Date);
            Assert.Equal(expected.Visits, actual.Visits);
            Assert.Equal(expected.FirstVisit, actual.FirstVisit);
            Assert.Equal(expected.LastVisit, actual.LastVisit);
        }

        private static void AssertEqual(Book expected, Book actual)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Genres.Length, actual.Genres.Length);
            for (var i = 0; i < expected.Genres.Length; i++)
            {
                Assert.Equal(expected.Genres[i], actual.Genres[i]);
            }
        }

        [Fact]
        public void Upsert_InitialDbState()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            Assert.Empty(dbContext.SchemaTable);
            Assert.Empty(dbContext.DashTable);
            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID), c => Assert.Equal("AU", c.ISO));
            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                pv => Assert.Equal((_dbVisitOld.UserID, _dbVisitOld.Date), (pv.UserID, pv.Date)),
                pv => Assert.Equal((_dbVisit.UserID, _dbVisit.Date), (pv.UserID, pv.Date))
            );
        }

        [Fact]
        public void Upsert_EFCore_KeyAutoGen()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.GuidKeysAutoGen.Add(new GuidKeyAutoGen { Name = "test" });
            dbContext.StringKeysAutoGen.Add(new StringKeyAutoGen { Name = "test" });
            dbContext.SaveChanges();

            // Ensuring EFCore generates empty values for Guid and string keys
            Assert.Collection(dbContext.GuidKeysAutoGen,
                e => Assert.NotEqual(Guid.Empty, e.ID));
            Assert.Collection(dbContext.StringKeysAutoGen,
                e => Assert.NotEmpty(e.ID));
        }

        [Fact]
        public void Upsert_IdentityKey_NoOn_InvalidMatchColumn()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Germany",
                ISO = "DE",
                Created = _now,
                Updated = _now,
            };

            Assert.Throws<InvalidMatchColumnsException>(() =>
            {
                dbContext.Countries.Upsert(newCountry).Run();
            });
        }

        [Fact]
        public void Upsert_IdentityKey_ExplicitOn_InvalidMatchColumn()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Germany",
                ISO = "DE",
                Created = _now,
                Updated = _now,
            };

            Assert.Throws<InvalidMatchColumnsException>(() =>
            {
                dbContext.Countries.Upsert(newCountry)
                    .On(c => c.ID)
                    .Run();
            });
        }

        [Fact]
        public void Upsert_IdentityKey_NoOn_AllowWithOverride()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Germany",
                ISO = "DE",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .AllowIdentityMatch()
                .Run();

            Assert.Equal(2, dbContext.Countries.Count());
        }

        [Fact]
        public void Upsert_IdentityKey_ExplicitOn_AllowWithOverride()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Germany",
                ISO = "DE",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ID)
                .AllowIdentityMatch()
                .Run();

            Assert.Equal(2, dbContext.Countries.Count());
        }

        [Fact]
        public void Upsert_Country_Update_On()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Australia",
                ISO = "AU",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ISO)
                .Run();

            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID),
                country => Assert.Equal(
                    (newCountry.ISO, newCountry.Name, newCountry.Created, newCountry.Updated),
                    (country.ISO, country.Name, country.Created, country.Updated)));
        }

        [Fact]
        public void Upsert_Country_Update_On_NoUpdate()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Australia",
                ISO = "AU",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ISO)
                .NoUpdate()
                .Run();

            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID),
                country =>
                {
                    Assert.Equal(newCountry.ISO, country.ISO);
                    Assert.NotEqual(newCountry.Name, country.Name);
                    Assert.NotEqual(newCountry.Created, country.Created);
                    Assert.NotEqual(newCountry.Updated, country.Updated);
                    Assert.Equal(_dbCountry.Name, country.Name);
                    Assert.Equal(_dbCountry.Created, country.Created);
                    Assert.Equal(_dbCountry.Updated, country.Updated);
                });
        }

        [Fact]
        public void Upsert_Country_Update_On_WhenMatched_Values()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Australia",
                ISO = "AU",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ISO)
                .WhenMatched(c => new Country
                {
                    Name = newCountry.Name,
                    Updated = newCountry.Updated,
                })
                .Run();

            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID),
                country =>
                {
                    Assert.Equal(newCountry.ISO, country.ISO);
                    Assert.Equal(newCountry.Name, country.Name);
                    Assert.NotEqual(newCountry.Created, country.Created);
                    Assert.Equal(_dbCountry.Created, country.Created);
                    Assert.Equal(newCountry.Updated, country.Updated);
                });
        }

        [Fact]
        public void Upsert_Country_Update_On_WhenMatched_Constants()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "Australia",
                ISO = "AU",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ISO)
                .WhenMatched(c => new Country
                {
                    Name = "Australia",
                    Updated = _now,
                })
                .Run();

            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID),
                country =>
                {
                    Assert.Equal(newCountry.ISO, country.ISO);
                    Assert.Equal(newCountry.Name, country.Name);
                    Assert.NotEqual(newCountry.Created, country.Created);
                    Assert.Equal(_dbCountry.Created, country.Created);
                    Assert.Equal(newCountry.Updated, country.Updated);
                });
        }

        [Fact]
        public void Upsert_Country_Insert_On_WhenMatched()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newCountry = new Country
            {
                Name = "United Kingdon",
                ISO = "GB",
                Created = _now,
                Updated = _now,
            };

            dbContext.Countries.Upsert(newCountry)
                .On(c => c.ISO)
                .WhenMatched(c => new Country
                {
                    Name = newCountry.Name,
                    Updated = newCountry.Updated,
                })
                .Run();

            Assert.Collection(dbContext.Countries.OrderBy(c => c.ID),
                country => Assert.Equal(
                    (_dbCountry.ISO, _dbCountry.Name, _dbCountry.Created, _dbCountry.Updated),
                    (country.ISO, country.Name, country.Created, country.Updated)),
                country => Assert.Equal(
                    (newCountry.ISO, newCountry.Name, newCountry.Created, newCountry.Updated),
                    (country.ISO, country.Name, country.Created, country.Updated)));
        }

        [Fact]
        public void Upsert_PageVisit_PreComputedOn()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(PageVisit.MatchKey)
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit => AssertEqual(newVisit, visit));
        }

        [Fact]
        public void Upsert_PageVisit_Update_On()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit => AssertEqual(newVisit, visit));
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_FromVar()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };
            var increment = 7;

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + increment,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + increment, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_FromField()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + _increment,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + _increment, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_FromSource_ValueAdd()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched((pv, pvi) => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = pvi.LastVisit,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_FromSource_ColumnAdd()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 5,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched((pv, pvi) => new PageVisit
                {
                    Visits = pv.Visits + pvi.Visits,
                    LastVisit = pvi.LastVisit,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + newVisit.Visits, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueAdd_Reversed()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = 1 + pv.Visits,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueSubtract()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits - 2,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits - 2, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueMultiply()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits * 3,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits * 3, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueBitwiseOr()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits | 3,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits | 3, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueBitwiseAnd()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits & 3,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit.UserID, visit.UserID);
                    Assert.Equal(newVisit.Date, visit.Date);
                    Assert.NotEqual(newVisit.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits & 3, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueDivide()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits / 4,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(_dbVisit.Visits / 4, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_PageVisit_Update_On_WhenMatched_ValueModulo()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit = new PageVisit
            {
                UserID = 1,
                Date = DateTime.Today,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.Upsert(newVisit)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits % 4,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(_dbVisit.Visits % 4, visit.Visits);
                    Assert.NotEqual(newVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void UpsertRange_PageVisit_Update_On_WhenMatched()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit1 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = _dbVisit.Date,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };
            var newVisit2 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = _dbVisit.Date.AddDays(1),
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit =>
                {
                    Assert.Equal(newVisit1.UserID, visit.UserID);
                    Assert.Equal(newVisit1.Date, visit.Date);
                    Assert.NotEqual(newVisit1.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit1.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit1.LastVisit, visit.LastVisit);
                },
                visit => AssertEqual(newVisit2, visit));
        }

        [Fact]
        public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleInsert()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit1 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = _dbVisit.Date.AddDays(1),
                Visits = 5,
                FirstVisit = _now,
                LastVisit = _now,
            };
            var newVisit2 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = newVisit1.Date.AddDays(2),
                Visits = newVisit1.Visits + 1,
                FirstVisit = newVisit1.FirstVisit.AddDays(1),
                LastVisit = newVisit1.LastVisit.AddDays(1),
            };

            dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit => AssertEqual(_dbVisitOld, visit),
                visit => AssertEqual(_dbVisit, visit),
                visit => AssertEqual(newVisit1, visit),
                visit => AssertEqual(newVisit2, visit));
        }

        [Fact]
        public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleUpdate()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit1 = new PageVisit
            {
                UserID = _dbVisitOld.UserID,
                Date = _dbVisitOld.Date,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };
            var newVisit2 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = _dbVisit.Date,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched(pv => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = _now,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit =>
                {
                    Assert.Equal(newVisit1.UserID, visit.UserID);
                    Assert.Equal(newVisit1.Date, visit.Date);
                    Assert.NotEqual(newVisit1.Visits, visit.Visits);
                    Assert.Equal(_dbVisitOld.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit1.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisitOld.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit1.LastVisit, visit.LastVisit);
                },
                visit =>
                {
                    Assert.Equal(newVisit2.UserID, visit.UserID);
                    Assert.Equal(newVisit2.Date, visit.Date);
                    Assert.NotEqual(newVisit2.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit2.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit2.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void UpsertRange_PageVisit_Update_On_WhenMatched_MultipleUpdate_FromSource()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newVisit1 = new PageVisit
            {
                UserID = _dbVisitOld.UserID,
                Date = _dbVisitOld.Date,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };
            var newVisit2 = new PageVisit
            {
                UserID = _dbVisit.UserID,
                Date = _dbVisit.Date,
                Visits = 1,
                FirstVisit = _now,
                LastVisit = _now,
            };

            dbContext.PageVisits.UpsertRange(newVisit1, newVisit2)
                .On(pv => new { pv.UserID, pv.Date })
                .WhenMatched((pv, pvi) => new PageVisit
                {
                    Visits = pv.Visits + 1,
                    LastVisit = pvi.LastVisit,
                })
                .Run();

            Assert.Collection(dbContext.PageVisits.OrderBy(c => c.Date),
                visit =>
                {
                    Assert.Equal(newVisit1.UserID, visit.UserID);
                    Assert.Equal(newVisit1.Date, visit.Date);
                    Assert.NotEqual(newVisit1.Visits, visit.Visits);
                    Assert.Equal(_dbVisitOld.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit1.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisitOld.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit1.LastVisit, visit.LastVisit);
                },
                visit =>
                {
                    Assert.Equal(newVisit2.UserID, visit.UserID);
                    Assert.Equal(newVisit2.Date, visit.Date);
                    Assert.NotEqual(newVisit2.Visits, visit.Visits);
                    Assert.Equal(_dbVisit.Visits + 1, visit.Visits);
                    Assert.NotEqual(newVisit2.FirstVisit, visit.FirstVisit);
                    Assert.Equal(_dbVisit.FirstVisit, visit.FirstVisit);
                    Assert.Equal(newVisit2.LastVisit, visit.LastVisit);
                });
        }

        [Fact]
        public void Upsert_Status_Update_AutoMatched_New()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newStatus = new Status
            {
                ID = 2,
                Name = "Updated",
                LastChecked = _now,
            };

            dbContext.Statuses.Upsert(newStatus).Run();

            Assert.Collection(dbContext.Statuses.OrderBy(s => s.ID),
                status => Assert.Equal(
                    (_dbStatus.ID, _dbStatus.Name, _dbStatus.LastChecked),
                    (status.ID, status.Name, status.LastChecked)),
                status => Assert.Equal(
                    (newStatus.ID, newStatus.Name, newStatus.LastChecked),
                    (status.ID, status.Name, status.LastChecked)));
        }

        [Fact]
        public void Upsert_Status_Update_AutoMatched_Existing()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newStatus = new Status
            {
                ID = _dbStatus.ID,
                Name = "Updated",
                LastChecked = _now,
            };

            dbContext.Statuses.Upsert(newStatus).Run();

            Assert.Collection(dbContext.Statuses,
                status => Assert.Equal((newStatus.Name, newStatus.LastChecked), (status.Name, status.LastChecked)));
        }

        [Fact]
        public void Upsert_DashedTable()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.DashTable.Upsert(new DashTable
            {
                DataSet = "test",
                Updated = _now,
            })
            .On(x => x.DataSet)
            .Run();

            Assert.Collection(dbContext.DashTable.OrderBy(t => t.ID),
                dt => Assert.Equal("test", dt.DataSet));
        }

        [Fact]
        public void Upsert_SchemaTable()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.SchemaTable.Upsert(new SchemaTable
            {
                Name = 1,
                Updated = _now,
            })
.On(x => x.Name)
.Run();

            Assert.Collection(dbContext.SchemaTable.OrderBy(t => t.ID),
                st => Assert.Equal(1, st.Name));
        }

        [Fact]
        public void Upsert_Book_On_Update()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newBook = new Book
            {
                Name = _dbBook.Name,
                Genres = new[] { "Fantasy", "Adventure" },
            };

            dbContext.Books.Upsert(newBook)
                .On(b => b.Name)
                .Run();

            Assert.Collection(dbContext.Books,
                b => AssertEqual(newBook, b));
        }

        [Fact]
        public void Upsert_Book_On_Insert()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newBook = new Book
            {
                Name = "The Two Towers",
                Genres = new[] { "Fantasy", "Adventure" },
            };

            dbContext.Books.Upsert(newBook)
                .On(p => p.Name)
                .Run();

            Assert.Collection(dbContext.Books.OrderBy(b => b.ID),
                b => AssertEqual(_dbBook, b),
                b => AssertEqual(newBook, b));
        }

        [Fact]
        public void Upsert_JObjectData()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newJson = new JObjectData
            {
                Data = new JObject(new JProperty("hello", "world")),
            };

            dbContext.JObjectDatas.Upsert(newJson)
                .Run();

            var dbDatas = dbContext.JObjectDatas.OrderBy(j => j.ID).ToArray();
            Assert.Collection(dbContext.JObjectDatas.OrderBy(j => j.ID),
                j => Assert.True(JToken.DeepEquals(newJson.Data, j.Data)));
        }

        [Fact]
        public void Upsert_JObject_Update()
        {
            var existingJson = new JObjectData
            {
                Data = new JObject(new JProperty("hello", "world")),
            };

            ResetDb(existingJson);
            using (var testContext = new TestDbContext(_fixture.DataContextOptions))
            {
                Assert.Collection(testContext.JObjectDatas.OrderBy(j => j.ID),
                    j => Assert.True(JToken.DeepEquals(existingJson.Data, j.Data)));
            }

            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var updatedJson = new JObjectData
            {
                Data = new JObject(new JProperty("welcome", "world 2.0")),
            };

            dbContext.JObjectDatas.Upsert(updatedJson)
                .Run();

            Assert.Collection(dbContext.JObjectDatas.OrderBy(j => j.ID),
                j => Assert.True(JToken.DeepEquals(updatedJson.Data, j.Data)));
        }

        [Fact]
        public void Upsert_JsonData()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newJson = new JsonData
            {
                Data = JsonConvert.SerializeObject(new { hello = "world" }),
            };

            dbContext.JsonDatas.Upsert(newJson)
                .Run();

            Assert.Collection(dbContext.JsonDatas.OrderBy(j => j.ID),
                j => Assert.True(JToken.DeepEquals(JObject.Parse(newJson.Data), JObject.Parse(j.Data))));
        }

        [Fact]
        public void Upsert_JsonData_Update()
        {
            var existingJson = new JsonData
            {
                Data = JsonConvert.SerializeObject(new { hello = "world" }),
            };

            ResetDb(existingJson);
            using (var testContext = new TestDbContext(_fixture.DataContextOptions))
            {
                Assert.Collection(testContext.JsonDatas.OrderBy(j => j.ID),
                    j => Assert.True(JToken.DeepEquals(JObject.Parse(existingJson.Data), JObject.Parse(j.Data))));
            }

            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var updatedJson = new JsonData
            {
                Data = JsonConvert.SerializeObject(new { welcome = "world 2.0" }),
            };

            dbContext.JsonDatas.Upsert(updatedJson)
                .Run();

            Assert.Collection(dbContext.JsonDatas.OrderBy(j => j.ID),
                j => Assert.True(JToken.DeepEquals(JObject.Parse(updatedJson.Data), JObject.Parse(j.Data))));
        }

        [Fact]
        public void Upsert_GuidKey_AutoGenThrows()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            Assert.Throws<InvalidMatchColumnsException>(delegate
            {
                var newItem = new GuidKeyAutoGen
                {
                    ID = Guid.NewGuid(),
                    Name = "test",
                };

                dbContext.GuidKeysAutoGen.Upsert(newItem)
                .Run();
            });
        }

        [Fact]
        public void Upsert_StringKey_AutoGenThrows()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            Assert.Throws<InvalidMatchColumnsException>(delegate
            {
                var newItem = new StringKeyAutoGen
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = "test",
                };

                dbContext.StringKeysAutoGen.Upsert(newItem)
                .Run();
            });
        }

        [Fact]
        public void Upsert_GuidKey()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new GuidKey
            {
                ID = Guid.NewGuid(),
                Name = "test",
            };

            dbContext.GuidKeys.Upsert(newItem)
                .Run();

            Assert.Collection(dbContext.GuidKeys.OrderBy(j => j.ID),
                j => Assert.Equal(newItem.ID, j.ID));
        }

        [Fact]
        public void Upsert_StringKey()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new StringKey
            {
                ID = Guid.NewGuid().ToString(),
                Name = "test",
            };

            dbContext.StringKeys.Upsert(newItem)
                .Run();

            Assert.Collection(dbContext.StringKeys.OrderBy(j => j.ID),
                j => Assert.Equal(newItem.ID, j.ID));
        }

        [Fact]
        public void Upsert_KeyOnly()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new KeyOnly
            {
                ID1 = 123,
                ID2 = 456,
            };

            dbContext.KeyOnlies.Upsert(newItem)
                .Run();

            Assert.Collection(dbContext.KeyOnlies.OrderBy(j => j.ID1),
                j => Assert.Equal((newItem.ID1, newItem.ID2), (j.ID1, j.ID2)));
        }

        [Fact]
        public void Upsert_NullableKeys()
        {
            if (_fixture.DbDriver == DbDriver.MySQL || _fixture.DbDriver == DbDriver.Postgres || _fixture.DbDriver == DbDriver.Sqlite)
                return;

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newItem1 = new NullableCompositeKey
            {
                ID1 = 1,
                ID2 = 3,
                Value = "Third",
            };
            var newItem2 = new NullableCompositeKey
            {
                ID1 = 1,
                ID2 = null,
                Value = "Fourth",
            };

            dbContext.NullableCompositeKeys.UpsertRange(newItem1, newItem2)
                .On(j => new { j.ID1, j.ID2 })
                .Run();

            var dbValues = dbContext.NullableCompositeKeys.ToArray();
            Assert.Collection(dbContext.NullableCompositeKeys.OrderBy(j => j.ID1).ThenBy(j => j.ID2),
                j => Assert.Equal((1, null, "Fourth"), (j.ID1, j.ID2, j.Value)),
                j => Assert.Equal((1, 2, "First"), (j.ID1, j.ID2, j.Value)),
                j => Assert.Equal((1, 3, "Third"), (j.ID1, j.ID2, j.Value))
            );
        }

        [Fact]
        public void Upsert_CompositeExpression_New()
        {
            ResetDb();
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((je, jn) => new TestEntity
                {
                    Num2 = je.Num2 * 2 + jn.Num2,
                })
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal((newItem.Num1, newItem.Num2, newItem.Text1, newItem.Text2), (e.Num1, e.Num2, e.Text1, e.Text2)));
        }

        [Fact]
        public void Upsert_CompositeExpression_Update()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((je, jn) => new TestEntity
                {
                    Num2 = je.Num2 * 2 + jn.Num2,
                })
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    dbItem.Num2 * 2 + newItem.Num2,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_ConditionalExpression_New()
        {
            ResetDb();
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((je, jn) => new TestEntity
                {
                    Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
                })
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal((newItem.Num1, newItem.Num2, newItem.Text1, newItem.Text2), (e.Num1, e.Num2, e.Text1, e.Text2)));
        }

        [Fact]
        public void Upsert_ConditionalExpression_UpdateTrue()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((je, jn) => new TestEntity
                {
                    Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
                })
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    dbItem.Num2 - newItem.Num2,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_ConditionalExpression_UpdateFalse()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 22,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((je, jn) => new TestEntity
                {
                    Num2 = je.Num2 - jn.Num2 > 0 ? je.Num2 - jn.Num2 : 0,
                })
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    0,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_UpdateCondition_Constant()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((e1, e2) => new TestEntity
                {
                    Num2 = e2.Num2,
                })
                .UpdateIf((ed, en) => en.Num2 == 2)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    newItem.Num2,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_UpdateCondition_New()
        {
            ResetDb();
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((e1, e2) => new TestEntity
                {
                    Num2 = e2.Num2,
                })
                .UpdateIf((ed, en) => ed.Num2 != en.Num2)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal((newItem.Num1, newItem.Num2, newItem.Text1, newItem.Text2), (e.Num1, e.Num2, e.Text1, e.Text2)));
        }

        [Fact]
        public void Upsert_UpdateCondition_New_AutoUpdate()
        {
            ResetDb();
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .UpdateIf((ed, en) => ed.Num2 != en.Num2)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal((newItem.Num1, newItem.Num2, newItem.Text1, newItem.Text2), (e.Num1, e.Num2, e.Text1, e.Text2)));
        }

        [Fact]
        public void Upsert_UpdateCondition_Update()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .WhenMatched((e1, e2) => new TestEntity
                {
                    Num2 = e2.Num2,
                })
                .UpdateIf((ed, en) => ed.Num2 != en.Num2 || ed.Text1 != en.Text1)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    newItem.Num2,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_UpdateCondition_AutoUpdate()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .UpdateIf((ed, en) => ed.Num2 != en.Num2)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    newItem.Num1,
                    newItem.Num2,
                    newItem.Text1,
                    newItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_UpdateCondition_NoUpdate()
        {
            var dbItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "hello",
                Text2 = "world",
            };

            ResetDb(dbItem);
            using var dbContex = new TestDbContext(_fixture.DataContextOptions);

            var newItem = new TestEntity
            {
                Num1 = 1,
                Num2 = 7,
                Text1 = "who",
                Text2 = "where",
            };

            dbContex.TestEntities.Upsert(newItem)
                .On(j => j.Num1)
                .UpdateIf((ed, en) => ed.Num2 != en.Num2)
                .Run();

            Assert.Collection(dbContex.TestEntities,
                e => Assert.Equal(
                    (
                    dbItem.Num1,
                    dbItem.Num2,
                    dbItem.Text1,
                    dbItem.Text2
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1,
                    e.Text2
                    )));
        }

        [Fact]
        public void Upsert_UpdateCondition_NullCheck()
        {
            var dbItem1 = new TestEntity
            {
                Num1 = 1,
                Num2 = 2,
                Text1 = "hello",
            };
            var dbItem2 = new TestEntity
            {
                Num1 = 2,
                Num2 = 3,
                Text1 = null
            };

            ResetDb(dbItem1, dbItem2);
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            dbContext.TestEntities.UpsertRange(dbItem1, dbItem2)
                .On(j => j.Num1)
                .WhenMatched(j => new TestEntity
                {
                    Num2 = j.Num2 + 1,
                })
                .UpdateIf(j => j.Text1 != null)
                .Run();

            Assert.Collection(dbContext.TestEntities.OrderBy(e => e.Num1).ToArray(),
                e => Assert.Equal(
                    (
                    dbItem1.Num1,
                    dbItem1.Num2 + 1,
                    dbItem1.Text1
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1
                    )),
                e => Assert.Equal(
                    (
                    dbItem2.Num1,
                    dbItem2.Num2,
                    dbItem2.Text1
                    ), (
                    e.Num1,
                    e.Num2,
                    e.Text1
                    )));
        }

        [Fact]
        public void Upsert_NullableRequired_Insert()
        {
            if (_fixture.DbDriver == DbDriver.MySQL)
                return; // Default values on text columns not supported in MySQL

            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newEntity = new NullableRequired
            {
                ID = 1
            };

            dbContext.NullableRequireds.Upsert(newEntity)
                .On(c => c.ID)
                .Run();

            Assert.Collection(dbContext.NullableRequireds.OrderBy(c => c.ID),
                entity => Assert.Equal(
                    ("B"),
                    (entity.Text)));
        }

        [Fact]
        public void Upsert_100k_Insert_MultiQuery()
        {
            ResetDb();
            using var dbContext = new TestDbContext(_fixture.DataContextOptions);

            var newEntities = Enumerable.Range(1, 100_000)
                .Select(i => new NullableRequired
                {
                    ID = i,
                    Text = i.ToString(),
                });
            dbContext.NullableRequireds.UpsertRange(newEntities)
                .On(c => c.ID)
                .Run();

            Assert.Equal(100_000, dbContext.NullableRequireds.Count());
        }
    }
}
