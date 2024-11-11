using System.Security.Claims;

namespace SuperOffice.Connector.Blazor.Utils
{
    public interface ISystemUserManager
    {
        void StoreTokens(ClaimsIdentity claimsIdentity);
    }
}
