using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.DTOs;
using TeamHub.Auth.Infrastructure;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IRefreshTokenService refreshTokenService,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "teamhub.refreshToken";
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    [AllowAnonymous]
    [HttpPost("dev-sign-in")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponse>> DevSignIn(
        DevSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsDevelopmentBypassAllowed())
        {
            return NotFound();
        }

        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var session = await authService.DevSignInAsync(
            request.UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (session is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateProblem(
                StatusCodes.Status403Forbidden,
                "Development sign-in denied",
                "The account is missing, inactive, or not approved for development sign-in."));
        }

        SetRefreshCookie(session.RefreshToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(session.Response);
    }

    [AllowAnonymous]
    [HttpGet("dev-users")]
    [ProducesResponseType<IReadOnlyList<DevSignInUser>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DevSignInUser>>> DevUsers(CancellationToken cancellationToken)
    {
        if (!IsDevelopmentBypassAllowed())
        {
            return NotFound();
        }

        var users = await authService.GetDevSignInUsersAsync(cancellationToken);
        return Ok(users);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken(CancellationToken cancellationToken)
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var token = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(token))
        {
            ClearRefreshCookie();
            return Unauthorized(CreateProblem(
                StatusCodes.Status401Unauthorized,
                "Refresh token required",
                "Sign in again to continue."));
        }

        var session = await refreshTokenService.RotateAsync(
            token,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        if (session is null)
        {
            ClearRefreshCookie();
            return Unauthorized(CreateProblem(
                StatusCodes.Status401Unauthorized,
                "Refresh token invalid",
                "Sign in again to continue."));
        }

        SetRefreshCookie(session.RefreshToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(session.Response);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (TryGetUserId(out var userId))
        {
            await refreshTokenService.RevokeCurrentAsync(
                userId,
                Request.Cookies[RefreshCookieName],
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (!HasTrustedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (TryGetUserId(out var userId))
        {
            await refreshTokenService.RevokeAllAsync(
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue("UserId"), out userId);

    private bool IsDevelopmentBypassAllowed() =>
        environment.IsDevelopment()
        && !configuration.GetValue("Auth:Entra:Enabled", false);

    private bool HasTrustedOrigin()
    {
        var origin = Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        var allowedOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200", "https://localhost:4200"];
        return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            MaxAge = RefreshTokenLifetime,
            IsEssential = true
        });
    }

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            IsEssential = true
        });

    private static ProblemDetails CreateProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };
}
