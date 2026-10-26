using System.Security.Claims;
using HandilyCommerce.Domain.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HandilyCommerce.Api.Auth;

/// <summary>POST auth/login (public) and GET auth/me (bearer-protected example).</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, string loginPath, string mePath)
    {
        app.MapPost(loginPath, Login)
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
    }

    public static IResult Login(LoginCommand command, IAuthPort authPort)
    {
        var result = authPort.Login(command);
        return result.Status switch
        {
            LoginStatus.Success => Results.Ok(new LoginResponse(result.Token!.Token, result.Token.ExpiresAt)),
            LoginStatus.Invalid => Results.ValidationProblem(result.Errors.ToDictionary()),
            _ => Results.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Credenciais inválidas.",
            }),
        };
    }

    public static IResult Me(ClaimsPrincipal user) =>
        Results.Ok(new MeResponse(user.FindFirstValue("email") ?? user.Identity?.Name ?? string.Empty));
}

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record MeResponse(string Email);
