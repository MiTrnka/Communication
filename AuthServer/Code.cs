namespace AuthServer;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

public class TokenService
{
    private static readonly ConcurrentDictionary<string, string> _refreshTokens = new();
    private readonly SymmetricSecurityKey _secretKey;
    private const int REFRESH_TOKEN_EXPIRY_MINUTES = 60 * 24 * 7; // 7 dní
    private const int ACCESS_TOKEN_EXPIRY_MINUTES = 15;

    public TokenService()
    {
        _secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyThatShouldBeInConfigInRealLife"));
    }

    public string GenerateRefreshToken(string username)
    {
        var refreshToken = Guid.NewGuid().ToString();
        _refreshTokens[refreshToken] = username;
        return refreshToken;
    }

    public string ValidateRefreshTokenAndGenerateAccessToken(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var username))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.AddMinutes(ACCESS_TOKEN_EXPIRY_MINUTES),
            SigningCredentials = new SigningCredentials(_secretKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("refresh-token")]
    public IActionResult GenerateRefreshToken([FromBody] string username)
    {
        var refreshToken = _tokenService.GenerateRefreshToken(username);
        return Ok(refreshToken);
    }

    [HttpPost("access-token")]
    public IActionResult GenerateAccessToken([FromBody] string refreshToken)
    {
        try
        {
            var accessToken = _tokenService.ValidateRefreshTokenAndGenerateAccessToken(refreshToken);
            return Ok(accessToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid refresh token");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class SecuredController : ControllerBase
{
    [Authorize]
    [HttpGet("protected-data")]
    public IActionResult GetProtectedData()
    {
        return Ok("Toto je chráněný obsah, který můžete vidět jen s platným přístupovým tokenem.");
    }
}
