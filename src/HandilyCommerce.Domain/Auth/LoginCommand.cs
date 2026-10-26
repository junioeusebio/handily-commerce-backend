namespace HandilyCommerce.Domain.Auth;

/// <summary>Login request body: e-mail + password.</summary>
public sealed record LoginCommand(string? Email, string? Password);
