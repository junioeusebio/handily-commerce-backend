namespace HandilyCommerce.Domain.Auth;

/// <summary>Opaque refresh token handed to the client (cookie) and its expiry.</summary>
public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>Outcome of <see cref="ISessionPort.Refresh"/>: null members mean 401.</summary>
public sealed record RefreshResult(AccessToken? AccessToken, IssuedRefreshToken? RefreshToken)
{
    public bool IsSuccess => AccessToken is not null && RefreshToken is not null;

    public static RefreshResult Unauthorized() => new(null, null);
}

/// <summary>Driving port: admin session renewal (refresh token rotation) and logout.</summary>
public interface ISessionPort
{
    IssuedRefreshToken Start(string email);

    RefreshResult Refresh(string? token);

    void Logout(string? token);
}
