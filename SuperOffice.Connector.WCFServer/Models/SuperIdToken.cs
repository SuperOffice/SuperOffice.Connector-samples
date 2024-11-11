using CoreWCF.IdentityModel.Claims;

namespace SuperOffice.Connector.WCFServer.Models
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
}
