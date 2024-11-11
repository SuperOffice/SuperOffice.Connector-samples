// Custom method to simulate reading from appsettings.json
using System.Collections.Specialized;
using System.Configuration;

public class AppSettingsHelper
{
        public static void LoadAppSettings()
        {
            // Build the configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Populate ConfigurationManager.AppSettings
        var appSettings = new NameValueCollection();
            foreach (var section in configuration.GetSection("AppSettings").AsEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(section.Key) && section.Value != null)
                {
                    appSettings.Set(section.Key.Replace("AppSettings:", ""), section.Value);
                }
            }

            // Replace the AppSettings property with your custom NameValueCollection
            System.Configuration.ConfigurationManager.AppSettings["ApplicationPrivateKeyFile"] = "123";
        }
}