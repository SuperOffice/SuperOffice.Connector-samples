using Microsoft.IdentityModel.Tokens;

namespace SuperOffice.Connector.Blazor.Models
{
    public class AppSettings
    {
        public Application Auth { get; set; } = new Application();
        public OpenIdConnectConfiguration OpenIdConnectConfiguration { get; set; } = new OpenIdConnectConfiguration();
    }

    public class OpenIdConnectConfiguration
    {
        public string? AuthEndpoint { get; set; }
        public string? TokenEndpoint { get; set; }

        public string? JwksUri { get; set; }

        public IList<JsonWebKey>? JsonWebKeys { get; set; }
    }
    public class Application
    {

        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }

        public string? Environment { get; set; }

        public string? RedirectUri { get; set; }

        public string? PrivateKey { get; set; }
    }
}