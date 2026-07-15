using Campaign.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _tokenService;

    public AuthController(IConfiguration configuration, JwtTokenService tokenService)
    {
        _configuration = configuration;
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponse> IssueToken([FromBody] TokenRequest request)
    {
        var clients = _configuration.GetSection("ApiClients").Get<List<ApiClient>>() ?? [];

        var client = clients.FirstOrDefault(c =>
            c.ClientId == request.ClientId && c.ClientSecret == request.ClientSecret);

        if (client is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid client credentials."
            });
        }

        var (token, expiresAtUtc) = _tokenService.CreateToken(client);
        return Ok(new TokenResponse(token, "Bearer", expiresAtUtc));
    }
}

public sealed record TokenRequest(string ClientId, string ClientSecret);

public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);