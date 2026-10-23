using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TeamHub.Auth.Contracts;
using TeamHub.Auth.Controllers;
using TeamHub.Auth.Entra;
using TeamHub.Auth.Infrastructure;
using TeamHub.Auth.Services;
using TeamHub.Auth.Authorization;

namespace TeamHub.Auth;

public static class TeamHubAuthServiceCollectionExtensions
{
    public const string EntraAuthenticationScheme = "EntraBearer";

    public static IServiceCollection AddTeamHubAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var entraEnabled = configuration.GetValue<bool>("Auth:Entra:Enabled");

        if (!environment.IsDevelopment() && !entraEnabled)
        {
            throw new InvalidOperationException(
                "Microsoft Entra ID must be enabled outside Development. Set Auth:Entra:Enabled=true " +
                "and provide valid Auth:Entra tenant/client configuration before deploying.");
        }

        var issuer = configuration["Authentication:Jwt:Issuer"]
            ?? throw new InvalidOperationException("Authentication:Jwt:Issuer is not configured.");
        var audience = configuration["Authentication:Jwt:Audience"]
            ?? throw new InvalidOperationException("Authentication:Jwt:Audience is not configured.");
        var secretKey = configuration["Authentication:Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "Configure Authentication:Jwt:SecretKey using a secure secret provider or environment variable.");
        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("Authentication:Jwt:SecretKey must contain at least 32 bytes.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var defaultScheme = entraEnabled ? EntraAuthenticationScheme : JwtBearerDefaults.AuthenticationScheme;
        var authentication = services.AddAuthentication(defaultScheme);

        authentication.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "Username",
                RoleClaimType = "Role"
            };
        });

        if (entraEnabled)
        {
            var instance = configuration["Auth:Entra:Instance"] ?? "https://login.microsoftonline.com/";
            var tenantId = configuration["Auth:Entra:TenantId"]
                ?? throw new InvalidOperationException("Auth:Entra:TenantId is required when Entra ID is enabled.");
            var entraAudience = configuration["Auth:Entra:Audience"]
                ?? configuration["Auth:Entra:ClientId"]
                ?? throw new InvalidOperationException("Auth:Entra:Audience (or ClientId) is required when Entra ID is enabled.");

            authentication.AddJwtBearer(EntraAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
                options.Audience = entraAudience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = entraAudience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "Role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = EntraClaimsIssuer.OnTokenValidated
                };
            });
        }

        services.AddAuthorization(options =>
        {
            foreach (var policy in RbacPolicies.PolicyPermissions)
            {
                options.AddPolicy(policy.Key, builder => builder
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(policy.Value)));
            }

            if (!environment.IsDevelopment())
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            }
        });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IJwtService, JwtService>();
        return services;
    }
}
