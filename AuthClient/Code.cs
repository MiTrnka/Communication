namespace AuthClient;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class OAuthClientController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private const string AUTH_SERVER_BASE_URL = "http://localhost:7001";

    public OAuthClientController()
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        {
            BaseAddress = new Uri(AUTH_SERVER_BASE_URL)
        };
    }

    [HttpGet("get-refresh-token")]
    public async Task<IActionResult> GetRefreshToken(string username)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(username),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/api/Auth/refresh-token", content);
        var refreshToken = await response.Content.ReadAsStringAsync();

        return Ok(refreshToken);
    }

    [HttpGet("access-token")]
    public async Task<IActionResult> GetAccessToken(string refreshToken)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(refreshToken),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/api/Auth/access-token", content);
        var accessToken = await response.Content.ReadAsStringAsync();

        return Ok(accessToken);
    }

    [HttpGet("access-protected-resource")]
    public async Task<IActionResult> AccessProtectedResource(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("/api/Secured/protected-data");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
        else
        {
            return Unauthorized("Nepodařilo se získat přístup k chráněnému zdroji.");
        }
    }
}