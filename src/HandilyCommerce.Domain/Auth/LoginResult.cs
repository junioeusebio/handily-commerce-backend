namespace HandilyCommerce.Domain.Auth;

/// <summary>Outcome of <see cref="IAuthPort.Login"/>.</summary>
public sealed record LoginResult(LoginStatus Status, AccessToken? Token, IReadOnlyDictionary<string, string[]> Errors)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();

    /// <summary>Authenticated e-mail (set on success; used to start the refresh session).</summary>
    public string? Email { get; init; }

    public static LoginResult Success(AccessToken token, string? email = null) =>
        new(LoginStatus.Success, token, NoErrors) { Email = email };

    public static LoginResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(LoginStatus.Invalid, null, errors);

    public static LoginResult Unauthorized() => new(LoginStatus.Unauthorized, null, NoErrors);

    public static LoginResult Disabled() => new(LoginStatus.Disabled, null, NoErrors);
}
