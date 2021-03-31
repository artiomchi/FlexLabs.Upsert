using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FlexLabs.EntityFrameworkCore5.Upsert.Tests
{
    public class Tests
    {
        public class Context : DbContext
        {
            public Context(DbContextOptions<Context> options)
                : base(options)
            {
            }

            public DbSet<Tag> Tags { get; set; }
            public DbSet<Parent> Parents { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<Tag>(
                    entitBuilder =>
                    {
                        entitBuilder.HasKey(x => x.Id);
                        entitBuilder.Property(x => x.Id)
                            .ValueGeneratedOnAdd();
                        entitBuilder.Property(x => x.Name)
                            .IsRequired();

                        entitBuilder.HasMany(x => x.Parents)
                            .WithOne(x => x.Tag)
                            .HasForeignKey(x => x.TagId);
                    });

                modelBuilder.Entity<Parent>(
                    entityBuilder =>
                    {
                        entityBuilder.HasKey(x => new
                        {
                            x.Timestamp,
                            x.TagId,
                        });

                        entityBuilder.OwnsOne(c => c.Child,
                            valueBuilder =>
                            {
                                valueBuilder.Property(x => x.ChildName)
                                    .IsRequired();
                                valueBuilder.Property(x => x.ChildCounter);
                            });
                        entityBuilder.Navigation(x => x.Child)
                            .IsRequired();

                        entityBuilder.HasOne(x => x.Tag)
                            .WithMany(x => x.Parents)
                            .HasForeignKey(x => x.TagId)
                            .OnDelete(DeleteBehavior.SetNull);
                    });

            }
        }

        public class Tag
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public ICollection<Parent> Parents { get; set; }
        }

        public class Parent
        {
            public int TagId { get; set; }

            public DateTime Timestamp { get; set; }

            public Tag Tag { get; set; }

            public Child Child { get; set; }

            public int Counter { get; set; }
        }

        public class Child
        {
            public string ChildName { get; set; }

            public int ChildCounter { get; set; }
        }

        [Test]
        public void Test1()
        {
            var builder = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(TestContext.CurrentContext.Test.Name);

            var tag = new Tag
            {
                Name = "FirstTag",
            };
            using (var context = new Context(builder.Options))
            {
                context.Tags.Add(tag);
                context.SaveChanges();
            }

            using (var context = new Context(builder.Options))
            {
                var parents = new[]
                {
                    new Parent
                    {
                        TagId = tag.Id,
                        Timestamp = DateTime.Now,
                        Child = new Child
                        {
                            ChildName = "Kind",
                            ChildCounter = 3,
                        },
                        Counter = 2,
                    }
                };

                context.Parents.UpsertRange(parents)
                    .On(x => new { x.Timestamp, x.TagId })
                    .NoUpdate()
                    .Run();
            }
        }
    }
}
