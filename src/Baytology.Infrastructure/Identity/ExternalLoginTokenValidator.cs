using System.Net.Http.Json;
using Baytology.Application.Common.Interfaces;
using Baytology.Application.Common.Errors;
using Baytology.Domain.Common.Results;
using Baytology.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baytology.Infrastructure.Identity;

public class ExternalLoginTokenValidator(
    HttpClient httpClient,
    IOptions<GoogleAuthSettings> googleOptions,
    ILogger<ExternalLoginTokenValidator> logger) : IExternalLoginTokenValidator
{
    private readonly GoogleAuthSettings _googleSettings = googleOptions.Value;

    public async Task<Result<ExternalUserInfoDto>> ValidateTokenAsync(string provider, string idToken)
    {
        if (provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            return await ValidateGoogleTokenAsync(idToken);
        }

        return ApplicationErrors.ExternalLogin.ProviderInvalid;
    }

    private async Task<Result<ExternalUserInfoDto>> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            // Google endpoint for token info
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            if (!response.IsSuccessStatusCode)
            {
                return ApplicationErrors.ExternalLogin.GoogleTokenInvalid;
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleTokenPayload>();
            if (payload == null)
            {
                return ApplicationErrors.ExternalLogin.GoogleTokenParseFailed;
            }

            // Explicit validation
            if (payload.Aud != _googleSettings.ClientId)
            {
                return ApplicationErrors.ExternalLogin.AudienceMismatch;
            }

            if (payload.Iss != "accounts.google.com" && payload.Iss != "https://accounts.google.com")
            {
                return ApplicationErrors.ExternalLogin.GoogleIssuerMismatch;
            }

            if (long.TryParse(payload.Exp, out var exp))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expirationTime < DateTimeOffset.UtcNow)
                {
                    return ApplicationErrors.ExternalLogin.GoogleTokenExpired;
                }
            }

            return new ExternalUserInfoDto(payload.Sub, payload.Email, payload.GivenName, payload.FamilyName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating Google token");
            return ApplicationErrors.ExternalLogin.TokenValidationFailed("Google");
        }
    }

    private class GoogleTokenPayload
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Aud { get; set; } = string.Empty;
        public string Iss { get; set; } = string.Empty;
        public string Exp { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("given_name")]
        public string? GivenName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
    }
}
