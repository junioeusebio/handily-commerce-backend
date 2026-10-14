using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Domain.Tests.Leads;

public class CreateLeadResultTests
{
    [Fact]
    public void Success_HasLeadAndNoErrors()
    {
        var lead = new Lead(Guid.NewGuid(), "Ana", "ana@x.com", "Recife", null, DateTimeOffset.UtcNow);

        var result = CreateLeadResult.Success(lead);

        Assert.True(result.IsSuccess);
        Assert.Same(lead, result.Lead);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Invalid_HasErrorsAndNoLead()
    {
        var result = CreateLeadResult.Invalid(new Dictionary<string, string[]> { ["email"] = ["E-mail inválido."] });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lead);
        Assert.Single(result.Errors);
    }
}
