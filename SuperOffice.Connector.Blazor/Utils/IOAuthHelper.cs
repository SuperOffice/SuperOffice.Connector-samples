using Microsoft.IdentityModel.Tokens;

namespace SuperOffice.Connector.Blazor.Utils
{
    public interface IOAuthHelper
    {
        Task InitializeOpenIdConnectEndpointsAsync();
        TokenValidationResult ValidateSuperOfficeTokenAsync(string token);
        Task GetJsonWebKeysAsync();
    }
}
