using CoreWCF.IdentityModel.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SuperOffice.Connector.WCFServer.Models;
using SuperOffice.SuperID;
using System.Text.Json;

namespace SuperOffice.Connector.WCFServer.Utils
{
    public interface IOAuthHelper
    {
        void LogSettings();
        Task InitializeOpenIdConnectEndpointsAsync();
        Task<TokenValidationResult> ValidateSuperOfficeTokenAsync(string token);
        OAuthHelper.SuperIdToken TranslateToSuperIdToken(TokenValidationResult result);
    }

    public class OAuthHelper : IOAuthHelper
    {
        public class SuperIdToken
        {
            public Claim[] Claims { get; private set; }

            public string IdentityProvider => Get<string>("http://schemes.superoffice.net/identity/identityprovider");

            public string Ticket => Get<string>("http://schemes.superoffice.net/identity/ticket");

            public string NetserverUrl => Get<string>("http://schemes.superoffice.net/identity/netserver_url");

            public string SystemToken => Get<string>("http://schemes.superoffice.net/identity/system_token");

            public string Email => Get<string>("http://schemes.superoffice.net/identity/email");

            public string ContextIdentifier => Get<string>("http://schemes.superoffice.net/identity/ctx");

            public SuperIdToken(Claim[] claims)
            {
                Claims = claims;
            }

            public TCarrier Get<TCarrier>(string key)
            {
                Claim claim = Claims.FirstOrDefault((Claim c) => key.Equals(c.ClaimType, StringComparison.InvariantCultureIgnoreCase));
                if (claim == null)
                {
                    return default(TCarrier);
                }

                return (TCarrier)claim.Resource;
            }
        }


        private readonly AppSettings _settings;

        private readonly IHttpClientFactory _httpClientFactory;

        public OAuthHelper(IOptions<AppSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public void LogSettings()
        {
            Console.WriteLine($"Able to get the ClientId from appsettings.json: {_settings.Auth.ClientId}");
            Console.WriteLine($"Able to get the authorize endpoint from .well-known: {_settings.OpenIdConnectConfiguration.AuthEndpoint}");
            // Log other properties as needed
        }

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
                using (JsonDocument document = JsonDocument.Parse(response))
                {
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

        public async Task<TokenValidationResult> ValidateSuperOfficeTokenAsync(string token)
        {
            var securityTokenHandler =
                new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();

            string issuer;
            string audience;

            // extract the ValidAudience claim value (database serial number).
            var securityToken = securityTokenHandler.ReadJsonWebToken(token);

            // get the audience from the token
            if (!securityToken.TryGetPayloadValue<string>("aud", out audience))
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException(
                    "Unable to read ValidAudience from token.");
            }

            // get the issuer from the token
            if (!securityToken.TryGetPayloadValue<string>("iss", out issuer))
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenException(
                    "Unable to read ValidAudience from token.");
            }

            var validationParameters =
                new Microsoft.IdentityModel.Tokens.TokenValidationParameters();
            validationParameters.ValidAudience = audience;
            validationParameters.ValidIssuer = issuer;

            validationParameters.IssuerSigningKeys = await GetJsonWebKeysAsync();

            var result = securityTokenHandler.ValidateToken(token, validationParameters);

            if (result.Exception != null || !result.IsValid)
            {
                throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException(
                    "Failed to validate the token", result.Exception);
            }
            return result;
        }

        private async Task<IList<JsonWebKey>> GetJsonWebKeysAsync()
        {
            // TODO: example only... needs exception handing...!!!
            var client = new HttpClient();
            var jwksContent = await client.GetStringAsync(_settings.OpenIdConnectConfiguration.JwksUri);
            return JsonWebKeySet.Create(jwksContent).Keys;
        }

        public OAuthHelper.SuperIdToken TranslateToSuperIdToken(TokenValidationResult result)
        {
            var claims = result.ClaimsIdentity.Claims.Select(c => new CoreWCF.IdentityModel.Claims.Claim(c.Type, c.Value.ToString(), "read")).ToArray();
            return new OAuthHelper.SuperIdToken(claims);
        }
    }
}
