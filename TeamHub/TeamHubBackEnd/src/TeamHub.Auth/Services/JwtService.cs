using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.DTOs;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Services;

internal sealed class JwtService : IJwtService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private readonly string issuer;
    private readonly string audience;
    private readonly SigningCredentials signingCredentials;

    public JwtService(IConfiguration configuration)
    {
        issuer = configuration["Authentication:Jwt:Issuer"]
            ?? throw new InvalidOperationException("Authentication:Jwt:Issuer is not configured.");
        audience = configuration["Authentication:Jwt:Audience"]
            ?? throw new InvalidOperationException("Authentication:Jwt:Audience is not configured.");
        var secretKey = configuration["Authentication:Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Authentication:Jwt:SecretKey is not configured.");
        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("Authentication:Jwt:SecretKey must contain at least 32 bytes.");
        }

        signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AuthResponse CreateAccessToken(AuthUser user)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(AccessTokenLifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("UserId", user.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("EmployeeId", user.EmployeeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("Username", user.Username),
            new("Email", user.Email),
            new("DepartmentId", user.DepartmentId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("DesignationId", user.DesignationId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("jti", Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(user.Roles.Select(role => new Claim("Role", role)));
        claims.AddRange(user.Permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: now,
            expires,
            signingCredentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            (int)AccessTokenLifetime.TotalSeconds,
            user.Roles,
            user.Permissions);
    }
}
