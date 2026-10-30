using System.Security.Claims;
using HandilyCommerce.Domain.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HandilyCommerce.Api.Auth;

/// <summary>POST auth/login, auth/refresh, auth/logout (public, cookie session) and GET auth/me (bearer).</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, string authPath)
    {
        var loginPath = authPath + "/login";
        var mePath = authPath + "/me";
        var cookiePath = authPath;

        app.MapPost(loginPath, (LoginCommand command, IAuthPort authPort, ISessionPort sessions, HttpResponse response) =>
                Login(command, authPort, sessions, response, cookiePath))
            .WithName("Login")
            .WithSummary("Login do admin (stub) — devolve um JWT")
            .Accepts<LoginCommand>("application/json")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet(mePath, Me)
            .RequireAuthorization()
            .WithName("Me")
            .WithSummary("E-mail do usuário autenticado (exige Bearer)")
            .Produces<MeResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(authPath + "/refresh", (ISessionPort sessions, HttpContext context) =>
                Refresh(sessions, context, cookiePath))
            .WithName("RefreshSession")
            .WithSummary("Renova a sessão do admin (cookie + header X-Requested-With) e devolve novo JWT")
            .Produces<LoginResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        app.MapPost(authPath + "/logout", (ISessionPort sessions, HttpContext context) =>
                Logout(sessions, context, cookiePath))
            .WithName("Logout")
            .WithSummary("Encerra a sessão do admin e limpa o cookie")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden);
    }

    public static IResult Refresh(ISessionPort sessions, HttpContext context, string cookiePath)
    {
        if (!SessionCookie.HasCsrfHeader(context.Request))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = sessions.Refresh(SessionCookie.Read(context.Request));
        if (!result.IsSuccess)
        {
            SessionCookie.Clear(context.Response, cookiePath);
            return Unauthorized("Sessão inválida ou expirada.");
        }

        SessionCookie.Write(context.Response, cookiePath, result.RefreshToken!);
        return Results.Ok(new LoginResponse(result.AccessToken!.Token, result.AccessToken.ExpiresAt));
    }

    public static IResult Logout(ISessionPort sessions, HttpContext context, string cookiePath)
    {
        if (!SessionCookie.HasCsrfHeader(context.Request))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        sessions.Logout(SessionCookie.Read(context.Request));
        SessionCookie.Clear(context.Response, cookiePath);
        return Results.NoContent();
    }

    private static IResult Unauthorized(string detail) => Results.Problem(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Unauthorized",
        Detail = detail,
    });

    public static IResult Login(
        LoginCommand command, IAuthPort authPort, ISessionPort sessions, HttpResponse response, string cookiePath)
    {
        var result = authPort.Login(command);
        switch (result.Status)
        {
            case LoginStatus.Success:
                if (result.Email is not null)
                {
                    SessionCookie.Write(response, cookiePath, sessions.Start(result.Email));
                }

                return Results.Ok(new LoginResponse(result.Token!.Token, result.Token.ExpiresAt));
            case LoginStatus.Invalid:
                return Results.ValidationProblem(result.Errors.ToDictionary());
            default:
                return Unauthorized("Credenciais inválidas.");
        }
    }

    public static IResult Me(ClaimsPrincipal user) =>
        Results.Ok(new MeResponse(user.FindFirstValue("email") ?? user.Identity?.Name ?? string.Empty));
}

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record MeResponse(string Email);
