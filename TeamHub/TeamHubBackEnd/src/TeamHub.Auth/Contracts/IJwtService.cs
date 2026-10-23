using TeamHub.Auth.DTOs;
using TeamHub.Auth.Models;

namespace TeamHub.Auth.Contracts;

public interface IJwtService
{
    AuthResponse CreateAccessToken(AuthUser user);
}
