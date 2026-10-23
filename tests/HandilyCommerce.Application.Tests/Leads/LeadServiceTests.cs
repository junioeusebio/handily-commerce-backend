using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Application.Tests.Leads;

public class LeadServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private static (LeadService Sut, FakeRepository Repo) Create() => Create(new FakeQueue());

    private static (LeadService Sut, FakeRepository Repo) Create(ILeadNotificationQueue queue)
    {
        var repo = new FakeRepository();
        return (new LeadService(repo, queue, new FixedTimeProvider(Now)), repo);
    }

    [Fact]
    public void Create_WithValidInput_EnqueuesNotification()
    {
        var queue = new FakeQueue();
        var (sut, _) = Create(queue);

        var result = sut.Create(new CreateLeadCommand("Ana", "ana@x.com", "Recife", null));

        Assert.Same(result.Lead, Assert.Single(queue.Leads));
    }

    [Fact]
    public void Create_WhenNotificationThrows_StillSucceeds()
    {
        var (sut, repo) = Create(new ThrowingQueue());

        var result = sut.Create(new CreateLeadCommand("Ana", "ana@x.com", "Recife", null));

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Leads);
    }

    [Fact]
    public void Create_WithInvalidInput_DoesNotEnqueue()
    {
        var queue = new FakeQueue();
        var (sut, _) = Create(queue);

        sut.Create(new CreateLeadCommand(null, null, null, null));

        Assert.Empty(queue.Leads);
    }

    [Fact]
    public void Create_WithValidInput_PersistsTrimmedLeadWithUtcTimestamp()
    {
        var (sut, repo) = Create();

        var result = sut.Create(new CreateLeadCommand(" Ana ", " ana@prefeitura.gov.br ", " Recife ", " Olá "));

        Assert.True(result.IsSuccess);
        var lead = Assert.Single(repo.Leads);
        Assert.Same(lead, result.Lead);
        Assert.NotEqual(Guid.Empty, lead.Id);
        Assert.Equal("Ana", lead.Name);
        Assert.Equal("ana@prefeitura.gov.br", lead.Email);
        Assert.Equal("Recife", lead.Municipality);
        Assert.Equal("Olá", lead.Message);
        Assert.Equal(Now, lead.CreatedAt);
        Assert.Equal(TimeSpan.Zero, lead.CreatedAt.Offset);
    }

    [Fact]
    public void Create_WithBlankMessage_StoresNullMessage()
    {
        var (sut, repo) = Create();

        var result = sut.Create(new CreateLeadCommand("Ana", "ana@x.com", "Recife", "   "));

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(repo.Leads).Message);
    }

    [Fact]
    public void Create_WithMissingRequiredFields_ReturnsErrorsAndDoesNotPersist()
    {
        var (sut, repo) = Create();

        var result = sut.Create(new CreateLeadCommand(null, " ", "", null));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lead);
        Assert.Equal(["email", "municipality", "name"], result.Errors.Keys.Order());
        Assert.Empty(repo.Leads);
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("ana@localhost")]
    [InlineData("Ana <ana@x.com>")]
    public void Create_WithInvalidEmail_ReturnsEmailError(string email)
    {
        var (sut, repo) = Create();

        var result = sut.Create(new CreateLeadCommand("Ana", email, "Recife", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(["E-mail inválido."], result.Errors["email"]);
        Assert.Empty(repo.Leads);
    }

    [Fact]
    public void Create_WithTooLongFields_ReturnsLengthErrors()
    {
        var (sut, _) = Create();

        var result = sut.Create(new CreateLeadCommand(
            new string('a', LeadService.NameMaxLength + 1),
            new string('a', LeadService.EmailMaxLength) + "@x.com",
            new string('m', LeadService.MunicipalityMaxLength + 1),
            new string('x', LeadService.MessageMaxLength + 1)));

        Assert.False(result.IsSuccess);
        Assert.Equal(["email", "message", "municipality", "name"], result.Errors.Keys.Order());
    }

    [Fact]
    public void Create_WithNullCommand_Throws()
    {
        var (sut, _) = Create();

        Assert.Throws<ArgumentNullException>(() => sut.Create(null!));
    }

    [Fact]
    public void LeadService_ImplementsILeadPort()
    {
        var (sut, _) = Create();

        Assert.IsType<ILeadPort>(sut, exactMatch: false);
    }

    private sealed class FakeRepository : ILeadRepository
    {
        public List<Lead> Leads { get; } = [];

        public void Add(Lead lead) => Leads.Add(lead);
    }

    private sealed class FakeQueue : ILeadNotificationQueue
    {
        public List<Lead> Leads { get; } = [];

        public void Enqueue(Lead lead) => Leads.Add(lead);
    }

    private sealed class ThrowingQueue : ILeadNotificationQueue
    {
        public void Enqueue(Lead lead) => throw new InvalidOperationException("boom");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
