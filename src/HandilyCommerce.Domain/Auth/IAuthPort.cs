namespace HandilyCommerce.Domain.Auth;

/// <summary>Driving port: authenticate the admin and issue an access token.</summary>
public interface IAuthPort
{
    LoginResult Login(LoginCommand command);
}
