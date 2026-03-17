using System.IdentityModel.Tokens.Jwt;
using JobOnlineAPI.Controllers;

namespace JobOnlineAPI.Services
{
    public interface IJwtTokenService
    {
        string GenerateJwtToken(UserModel user);
        Task<JwtSecurityToken> ValidateTokenAsync(string token);
        string GenerateRefreshToken(string username, string role);
        Task<JwtSecurityToken> ValidateRefreshTokenAsync(string token);
    }
}