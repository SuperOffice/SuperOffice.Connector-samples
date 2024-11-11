 using SuperOffice.Online.IntegrationService;
using SuperOffice.CRM;
using Microsoft.Extensions.Hosting.Internal;
using SuperOffice.SystemUser;
using static SuperOffice.Connector.WCFServer.Utils.OAuthHelper;
using SuperOffice.Connector.WCFServer.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace SuperOffice.Connector.WCFServer
{
    public class QuoteConnector : OnlineQuoteConnector<JsonQuoteConnector>
    {
        public QuoteConnector() : base
           (
                // Application Identifier
                "Cust31038",
                 // Application Private key file
                 Debug()
           )
        { }

        private static string Debug()
        {

            Console.WriteLine("doing stuff");
            var temp = GetPrivateKey("LocalhostPrivateKey.xml");
            Console.WriteLine(temp);
            return temp;
        }

        protected override TResponse Execute<TRequest, TResponse>(TRequest request, Action<IQuoteConnector, TResponse> action)
        {
            return base.Execute(request, action);
        }

        protected override SuperOffice.JsonQuoteConnector GetInnerTypedQuoteConnector<TRequest>(TRequest request)
        {
            var inner = base.GetInnerTypedQuoteConnector(request);


            var folder = "test";

            if (String.IsNullOrWhiteSpace(folder))
                folder = "test";

            folder = Path.Combine(folder, request.ContextIdentifier);


            inner.FolderName = folder;

            return inner;
        }

         protected override ClaimsIdentity ValidateSuperOfficeSignedToken(string token)
        {
            return base.ValidateSuperOfficeSignedToken(token);
        }

    }
}
