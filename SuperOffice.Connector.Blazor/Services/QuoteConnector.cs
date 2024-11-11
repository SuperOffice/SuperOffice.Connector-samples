using SuperOffice.Online.IntegrationService;
using SuperOffice.CRM;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using SuperOffice.SuperID.Contracts.V1;
using SuperOffice.SuperID.Contracts;
using SuperOffice.Online.Tokens;
using SuperOffice.Security.Principal;
using Microsoft.Identity.Client;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using SuperOffice.Connector.Blazor.Utils;
using SuperOffice.CRM.ArchiveLists;
using System.Runtime;
using SuperOffice.Connector.Blazor.Models;
using Microsoft.Extensions.Options;
using SuperOffice.CD.DSL.Database;

namespace SuperOffice.Connector.Blazor.Services
{
    public class QuoteConnector : OnlineQuoteConnector<JsonQuoteConnector>, IIntegrationServiceConnectorAuth
    {
        private readonly AppSettings _settings;

        private readonly PartnerTokenIssuer _partnerTokenIssuer;

        private readonly IOAuthHelper _oauthHelper;

         public QuoteConnector(IOptions<AppSettings> settings, IOAuthHelper oauthHelper) : base
           (
                settings.Value.Auth.ClientId,
                GetPrivateKey(settings.Value.Auth.PrivateKey)
           )
        {
            _settings = settings.Value;
            _oauthHelper = oauthHelper;
            this._partnerTokenIssuer = new PartnerTokenIssuer(new PartnerCertificateResolver(() => PrivateKey));
        }

        /// <summary>
        /// Authenticates an integration service request by validating the provided signed token and ensuring it matches the expected audience.
        /// Returns an authentication response indicating success or failure.
        /// </summary>
        /// <param name="request">The authentication request containing the signed token.</param>
        /// <returns>An AuthenticationResponse indicating the result of the authentication process.</returns>
        AuthenticationResponse IIntegrationServiceConnectorAuth.Authenticate(AuthenticationRequest request)
        {
            var applicationIdentifier = _settings.Auth.ClientId;

            try
            {
                var token = ValidateSuperOfficeSignedToken(request.SignedToken);

                if (!string.Equals("spn:" + applicationIdentifier, token.FindFirst("aud").Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new AuthenticationResponse
                    {
                        Succeeded = false,
                        Reason = "Wrong audience, missmatch on application identifier"
                    };
                }

                return new AuthenticationResponse
                {
                    Succeeded = true,
                    SignedApplicationToken = _partnerTokenIssuer.SignPartnerToken(token.GetNonce())
                };
            }
            catch
            {
                return new AuthenticationResponse
                {
                    Succeeded = false,
                    Reason = "Failed to validate authentication request"
                };
            }
        }


        /// <summary>
        /// Validates a SuperOffice signed token by using the OAuthHelper to validate the token and returning the associated ClaimsIdentity.
        /// </summary>
        /// <param name="token">The signed token to validate.</param>
        /// <returns>A ClaimsIdentity representing the validated token.</returns>
        protected override ClaimsIdentity ValidateSuperOfficeSignedToken(string token)

        {
            TokenValidationResult validationResult = _oauthHelper.ValidateSuperOfficeTokenAsync(token);
            return validationResult.ClaimsIdentity;
        }

        protected override string CreatePartnerToken(ClaimsIdentity token)
        {
            return _partnerTokenIssuer.SignPartnerToken(token.GetNonce());
        }

        public string TemplateFolder
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Template");
            }
        }

        /// <summary>
        /// Retrieves and configures an instance of JsonQuoteConnector based on the provided request.
        /// Ensures the appropriate folder structure is set up for the context identifier in the request.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request object.</typeparam>
        /// <param name="request">The request object containing the context identifier.</param>
        /// <returns>An instance of JsonQuoteConnector configured with the appropriate folder name.</returns>
        protected override SuperOffice.JsonQuoteConnector GetInnerTypedQuoteConnector<TRequest>(TRequest request)

        {
            var inner = base.GetInnerTypedQuoteConnector(request);

            // Use _projectRootPath directly
            var folder = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the folder path with the context identifier from the request
            folder = Path.Combine(folder, "App_Data", request.ContextIdentifier);

            // Check if the folder exists; if not, create or copy it
            if (!Directory.Exists(folder))
            {
                DirectoryCopy(TemplateFolder, folder, false);
            }

            // Set the folder name in the inner connector
            inner.FolderName = folder;

            return inner;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}