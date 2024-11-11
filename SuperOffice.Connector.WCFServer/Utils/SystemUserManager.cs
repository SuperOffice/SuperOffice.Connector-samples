using System.Text.Json;
using static SuperOffice.Configuration.ConfigFile;

namespace SuperOffice.Connector.WCFServer.Utils
{
    public class SystemUserManager
    {
        private static void StoreTokens(TokenValidationResult tokenValidationResult)
        {
            //session["AccessToken"] = result.;
            var context = new
            {
                Email = tokenValidationResult.Claims.FirstOrDefault(c => c.Key.Contains("http://schemes.superoffice.net/identity/email")).Value.ToString(),
                ContextIdentifier = tokenValidationResult.Claims.FirstOrDefault(c => c.Key.Contains("http://schemes.superoffice.net/identity/ctx")).Value.ToString(),
                NetServerUrl = tokenValidationResult.Claims.FirstOrDefault(c => c.Key.Contains("http://schemes.superoffice.net/identity/netserver_url")).Value.ToString(),
                SystemToken = tokenValidationResult.Claims.FirstOrDefault(c => c.Key.Contains("http://schemes.superoffice.net/identity/system_token")).Value.ToString()
            };

            // Add this to a json-file in appdata. 
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "context.json");
            StoreUserContextToFile(context, filePath);
        }

        public static void StoreUserContextToFile(SuperOfficeContext context, string filePath)
        {
            try
            {
                List<SuperOfficeContext> existingContexts = new List<SuperOfficeContext>();

                // Check if the file exists and read existing contexts
                if (File.Exists(filePath))
                {
                    string existingJson = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        existingContexts = JsonSerializer.Deserialize<List<SuperOfficeContext>>(existingJson) ?? new List<SuperOfficeContext>();
                    }
                }

                // Update existing contexts or add new ones
                var existingContext = existingContexts.FirstOrDefault(c => c.ContextIdentifier == context.ContextIdentifier);
                if (existingContext != null)
                {
                    // Update the existing context
                    existingContext.Email = context.Email;
                    existingContext.ContextIdentifier = context.ContextIdentifier;
                    existingContext.NetServerUrl = context.NetServerUrl;
                    existingContext.SystemToken = context.SystemToken;
                }
                else
                {
                    // Add new context
                    existingContexts.Add(context);
                }

                // Serialize the context to JSON format
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(existingContexts, options);

                // Write the JSON to a file
                File.WriteAllText(filePath, json);

                Console.WriteLine("Context saved successfully to " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }

}
}
