namespace HandilyCommerce.Domain.Auth;

/// <summary>Driven port: issue a signed access token for an authenticated e-mail.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(string email);
}
