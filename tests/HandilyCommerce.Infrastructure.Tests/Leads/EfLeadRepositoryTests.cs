using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Infrastructure.Leads;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Tests.Leads;

/// <summary>Uses EF InMemory — no live Supabase / Postgres required in CI.</summary>
public class EfLeadRepositoryTests
{
    [Fact]
    public void Add_PersistsLead()
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(nameof(Add_PersistsLead))
            .Options;
        var lead = new Lead(Guid.NewGuid(), "Ana", "ana@x.com", "Recife", null, DateTimeOffset.UtcNow, Phone: "11912345678");

        using (var context = new HandilyCommerceDbContext(options))
        {
            new EfLeadRepository(context).Add(lead);
        }

        using var read = new HandilyCommerceDbContext(options);
        var stored = Assert.Single(read.Leads.AsNoTracking());
        Assert.Equal(lead, stored);
    }

    [Fact]
    public void Model_MapsLeadsTable()
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(nameof(Model_MapsLeadsTable))
            .Options;
        using var context = new HandilyCommerceDbContext(options);

        var entity = context.Model.FindEntityType(typeof(Lead))!;

        Assert.Equal(LeadModel.TableName, entity.GetTableName());
        Assert.Equal(120, entity.FindProperty(nameof(Lead.Name))!.GetMaxLength());
        Assert.Equal(LeadModel.PhoneMaxLength, entity.FindProperty(nameof(Lead.Phone))!.GetMaxLength());
        Assert.True(entity.FindProperty(nameof(Lead.Phone))!.IsNullable);
    }
}
