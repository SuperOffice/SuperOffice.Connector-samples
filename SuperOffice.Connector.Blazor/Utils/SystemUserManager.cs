using Microsoft.Extensions.Hosting.Internal;
using Org.BouncyCastle.Asn1.Ess;
using SuperOffice.Connector.Blazor.Models;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace SuperOffice.Connector.Blazor.Utils
{
    public class SystemUserManager : ISystemUserManager
    {
        public string FilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "context.json");
            }
        }

        /// <summary>
        /// Stores user tokens extracted from a ClaimsIdentity object into a JSON file.
        /// It creates a CustomerContext object populated with values from specific claims in the ClaimsIdentity.
        /// If the file specified by FilePath does not exist, it creates the file.
        /// Then it calls the StoreCustomerContext method to store the CustomerContext in the specified file.
        /// </summary>
        /// <param name="claimsIdentity">The ClaimsIdentity object containing user claims.</param>
        public void StoreTokens(ClaimsIdentity claimsIdentity)
        {
            var context = new CustomerContext
            {
                Email = claimsIdentity.FindFirst("http://schemes.superoffice.net/identity/email)")?.Value,
                ContextIdentifier = claimsIdentity.FindFirst("http://schemes.superoffice.net/identity/ctx")?.Value,
                NetServerUrl = claimsIdentity.FindFirst("http://schemes.superoffice.net/identity/netserver_url")?.Value,
                WebApiUrl = claimsIdentity.FindFirst("http://schemes.superoffice.net/identity/webapi_url")?.Value,
                SystemToken = claimsIdentity.FindFirst("http://schemes.superoffice.net/identity/system_token")?.Value
            };

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath);
            }
            StoreCustomerContext(context, FilePath);
        }


        private void SaveToFile(string filePath, List<CustomerContext> customerContexts)
        {
            // Serialize the context to JSON format
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(customerContexts, options);
            // Write the JSON to a file
            File.WriteAllText(filePath, json);
        }

        private void StoreCustomerContext(CustomerContext context, string filePath)
        {
            try
            {
                var fileContent = File.ReadAllText(filePath);
                List<CustomerContext> fileContexts = new List<CustomerContext>();

                if (fileContent.IsNullOrEmpty())
                {
                    fileContexts = new List<CustomerContext>();
                }
                else
                {
                    fileContexts = JsonSerializer.Deserialize<List<CustomerContext>>(fileContent);
                    var customerContext = fileContexts.FirstOrDefault(c => c.ContextIdentifier == context.ContextIdentifier);
                    if (customerContext != null)
                    {
                        fileContexts.Remove(context);
                    }
                }
                fileContexts.Add(context);
                SaveToFile(filePath, fileContexts);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}

