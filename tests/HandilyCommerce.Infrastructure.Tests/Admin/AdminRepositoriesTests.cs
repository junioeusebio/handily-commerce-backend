using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Products;
using HandilyCommerce.Infrastructure.Courses;
using HandilyCommerce.Infrastructure.Leads;
using HandilyCommerce.Infrastructure.Persistence;
using HandilyCommerce.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Tests.Admin;

/// <summary>Admin repository operations on EF InMemory (no live Postgres).</summary>
public class AdminRepositoriesTests
{
    private static DbContextOptions<HandilyCommerceDbContext> Options(string name) =>
        new DbContextOptionsBuilder<HandilyCommerceDbContext>().UseInMemoryDatabase(name).Options;

    [Fact]
    public void Leads_ListFindUpdateDeleteCount()
    {
        var options = Options(nameof(Leads_ListFindUpdateDeleteCount));
        var now = DateTimeOffset.UtcNow;
        var old = new Lead(Guid.NewGuid(), "A", "a@x.com", "M", null, now.AddHours(-1));
        var recent = new Lead(Guid.NewGuid(), "B", "b@x.com", "M", null, now, LeadStatus.Contatado);
        using (var seed = new HandilyCommerceDbContext(options))
        {
            var repo = new EfLeadRepository(seed);
            repo.Add(old);
            repo.Add(recent);
        }

        using var context = new HandilyCommerceDbContext(options);
        var sut = new EfLeadRepository(context);

        var (all, total) = sut.List(null, 0, 10);
        Assert.Equal([recent.Id, old.Id], all.Select(l => l.Id));
        Assert.Equal(2, total);
        var (filtered, filteredTotal) = sut.List(LeadStatus.Novo, 0, 10);
        Assert.Equal(old.Id, Assert.Single(filtered).Id);
        Assert.Equal(1, filteredTotal);
        Assert.Equal(LeadStatus.Novo, sut.Find(old.Id)!.Status);

        Assert.True(sut.Update(old with { Status = LeadStatus.Descartado, InternalNote = "spam" }));
        Assert.False(sut.Update(old with { Id = Guid.NewGuid() }));
        Assert.Equal("spam", sut.Find(old.Id)!.InternalNote);

        var counts = sut.CountByStatus();
        Assert.Equal(1, counts[LeadStatus.Descartado]);
        Assert.Equal(1, counts[LeadStatus.Contatado]);

        Assert.True(sut.Delete(old.Id));
        Assert.False(sut.Delete(old.Id));
        Assert.Null(sut.Find(old.Id));
    }

    [Fact]
    public void LeadModel_MapsAdminFields()
    {
        using var context = new HandilyCommerceDbContext(Options(nameof(LeadModel_MapsAdminFields)));
        var entity = context.Model.FindEntityType(typeof(Lead))!;

        Assert.Equal(LeadModel.StatusMaxLength, entity.FindProperty(nameof(Lead.Status))!.GetMaxLength());
        Assert.Equal(2000, entity.FindProperty(nameof(Lead.InternalNote))!.GetMaxLength());
    }

    [Fact]
    public void Courses_AddFindUpdateDeleteCount()
    {
        var options = Options(nameof(Courses_AddFindUpdateDeleteCount));
        using var context = new HandilyCommerceDbContext(options);
        var sut = new EfCourseRepository(context);
        var first = new Course("c1", "T1", BnccAxis.MundoDigital, "s", "a");
        var second = new Course("c2", "T2", BnccAxis.CulturaDigital, "s", "a", 10);

        sut.Add(first, null);
        sut.Add(second, null);
        Assert.Equal(["c1", "c2"], sut.ListAll().Select(c => c.Id));
        Assert.Equal(2, sut.Count());

        Assert.True(sut.Update(first with { Title = "Novo" }, 99));
        Assert.True(sut.Update(second with { Title = "Mantém ordem" }, null));
        Assert.False(sut.Update(new Course("x", "T", BnccAxis.MundoDigital, "s", "a"), null));
        Assert.Equal(["c2", "c1"], sut.ListAll().Select(c => c.Id));
        Assert.Equal("Novo", sut.Find("c1")!.Title);

        Assert.True(sut.Delete("c1"));
        Assert.False(sut.Delete("c1"));
        Assert.Equal(1, sut.Count());
    }

    [Fact]
    public void Products_AddFindUpdateDeleteSku()
    {
        var options = Options(nameof(Products_AddFindUpdateDeleteSku));
        var id = Guid.NewGuid();
        using (var seed = new HandilyCommerceDbContext(options))
        {
            new EfProductRepository(seed).Add(new Product
            {
                Id = id,
                Name = "Kit",
                Sku = "K1",
                CreatedAt = DateTimeOffset.UtcNow,
                Items = [new Item { Id = Guid.NewGuid(), ProductId = id, Name = "Velho" }],
            });
        }

        using var context = new HandilyCommerceDbContext(options);
        var sut = new EfProductRepository(context);
        Assert.True(sut.SkuExists("K1", null));
        Assert.False(sut.SkuExists("K1", id));
        Assert.Equal(1, sut.Count());

        var replaced = new Product
        {
            Id = id,
            Name = "Kit 2",
            Sku = "K2",
            Items = [new Item { Id = Guid.NewGuid(), Name = "A" }, new Item { Id = Guid.NewGuid(), Name = "B" }],
        };
        Assert.True(sut.Update(replaced));
        Assert.False(sut.Update(new Product { Id = Guid.NewGuid(), Name = "x" }));

        var stored = sut.Find(id)!;
        Assert.Equal("Kit 2", stored.Name);
        Assert.Equal(["A", "B"], stored.Items.Select(i => i.Name).Order());
        Assert.Equal(2, context.Items.Count());

        Assert.True(sut.Delete(id));
        Assert.False(sut.Delete(id));
        Assert.Null(sut.Find(id));
    }
}
