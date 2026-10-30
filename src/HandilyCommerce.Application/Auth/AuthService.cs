using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Application.Auth;

/// <summary>
/// Use case: stub login for the single admin configured via <see cref="AuthOptions"/>.
/// </summary>
public sealed class AuthService(
    AuthOptions options,
    IPasswordVerifier passwordVerifier,
    IAccessTokenIssuer tokenIssuer) : IAuthPort
{
    public LoginResult Login(LoginCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = command.Email?.Trim() ?? string.Empty;
        var password = command.Password ?? string.Empty;

        var errors = new Dictionary<string, string[]>();
        if (email.Length == 0)
        {
            errors["email"] = ["E-mail é obrigatório."];
        }

        if (password.Length == 0)
        {
            errors["password"] = ["Senha é obrigatória."];
        }

        if (errors.Count > 0)
        {
            return LoginResult.Invalid(errors);
        }

        if (!options.IsConfigured)
        {
            return LoginResult.Disabled();
        }

        // Always verify the password (even on e-mail mismatch) to keep timing uniform.
        var emailMatches = string.Equals(email, options.AdminEmail!.Trim(), StringComparison.OrdinalIgnoreCase);
        var passwordMatches = passwordVerifier.Verify(password, options.AdminPasswordHash!);

        return emailMatches && passwordMatches
            ? LoginResult.Success(tokenIssuer.Issue(options.AdminEmail.Trim()), options.AdminEmail.Trim())
            : LoginResult.Unauthorized();
    }
}
