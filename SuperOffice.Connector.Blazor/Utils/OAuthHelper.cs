using CoreWCF.IdentityModel.Claims;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperOffice.Connector.Blazor.Models;
using SuperOffice.CRM.ArchiveLists;
using SuperOffice.SuperID;
using System.Text.Json;

namespace SuperOffice.Connector.Blazor.Utils
{
    public class OAuthHelper(IOptions<AppSettings> settings, IHttpClientFactory httpClientFactory) : IOAuthHelper
    {
        private readonly AppSettings _settings = settings.Value;

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        /// <summary>
        /// Initializes the OpenID Connect endpoints by fetching the discovery document from the specified environment
        /// and extracting the authorization, token, and JWKS URI endpoints. Throws exceptions if any endpoint is missing
        /// or if there are HTTP or JSON parsing errors.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeOpenIdConnectEndpointsAsync()
        {
            // Create a new HttpClient instance from the factory
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                // Construct the discovery endpoint URL
                string discoveryUrl = $"https://{_settings.Auth.Environment}.superoffice.com/login/.well-known/openid-configuration";

                // Fetch and parse the discovery document
                var response = await httpClient.GetStringAsync(discoveryUrl);
                using JsonDocument document = JsonDocument.Parse(response);
                JsonElement root = document.RootElement;

                // Extract and set the required endpoints, throwing an exception if not found
                string authEndpoint = root.GetProperty("authorization_endpoint").GetString();
                _settings.OpenIdConnectConfiguration.AuthEndpoint = root.GetProperty("authorization_endpoint").GetString()
                    ?? throw new InvalidOperationException("Authorization endpoint is null or missing.");
                _settings.OpenIdConnectConfiguration.TokenEndpoint = root.GetProperty("token_endpoint").GetString()
                    ?? throw new InvalidOperationException("Token endpoint is null or missing.");
                _settings.OpenIdConnectConfiguration.JwksUri = root.GetProperty("jwks_uri").GetString()
                    ?? throw new InvalidOperationException("JWKS URI is null or missing.");
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP errors
                Console.WriteLine($"HTTP request error: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                // Handle JSON parsing errors
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other general errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates a SuperOffice token by reading its claims and verifying its audience and issuer against the provided
        /// validation parameters. Throws exceptions if the token is invalid or if any required claims are missing.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>A TokenValidationResult indicating the outcome of the validation process.</returns>
        /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">Thrown when the token is missing required claims.</exception>
        /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenValidationException">Thrown when the token validation fails.</exception>
        public TokenValidationResult ValidateSuperOfficeTokenAsync(string token)
        {
            var securityTokenHandler =
                new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();


            // extract the ValidAudience claim value (database serial number).
            var securityToken = securityTokenHandler.ReadJsonWebToken(token);

            // get the audience from the token
            if (!securityToken.TryGetPayloadValue<string>("aud", out string audience))
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException(
                    "Unable to read ValidAudience from token.");
            }

            // get the issuer from the token
            if (!securityToken.TryGetPayloadValue<string>("iss", out string issuer))
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException(
                    "Unable to read ValidAudience from token.");
            }

            var validationParameters =
                new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidAudience = audience,
                    ValidIssuer = issuer,

                    IssuerSigningKeys = _settings.OpenIdConnectConfiguration.JsonWebKeys
                };

            var result = securityTokenHandler.ValidateToken(token, validationParameters);

            if (result.Exception != null || !result.IsValid)
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException(
                    "Failed to validate the token", result.Exception);
            }
            return result;
        }

        /// <summary>
        /// Fetches the JSON Web Keys (JWKS) from the configured JWKS URI and updates the OpenIdConnectConfiguration with the retrieved keys.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="HttpRequestException">Thrown when there is an error in the HTTP request.</exception>
        /// <exception cref="JsonException">Thrown when there is an error in parsing the JSON response.</exception>
        public async Task GetJsonWebKeysAsync()
        {
            try
            {
                // TODO: example only... needs exception handing...!!!
                var client = new HttpClient();
                var jwksContent = await client.GetStringAsync(_settings.OpenIdConnectConfiguration.JwksUri);
                _settings.OpenIdConnectConfiguration.JsonWebKeys = JsonWebKeySet.Create(jwksContent).Keys;
            }
            catch
            {
                // TODO: Handle exceptions
            }
        }
    }
}
