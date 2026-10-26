namespace HandilyCommerce.Domain.Auth;

/// <summary>Driven port: verify a plain password against a stored hash (constant time).</summary>
public interface IPasswordVerifier
{
    bool Verify(string password, string passwordHash);
}
