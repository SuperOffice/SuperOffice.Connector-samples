using Microsoft.Extensions.DependencyInjection;
using SuperOffice.Connector.WCFServer.Models;
using SuperOffice.Connector.WCFServer.Utils;
using SuperOffice.Online.IntegrationService.Contract;

var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

// Register AppSettings using Configure
var appSettingsSection = builder.Configuration.GetSection("Appsettings");
builder.Services.Configure<AppSettings>(appSettingsSection);

// Register IHttpClientFactory and IOAuthHelper with OAuthHelper as its implementation
builder.Services.AddHttpClient(); // Register IHttpClientFactory
builder.Services.AddSingleton<IOAuthHelper, OAuthHelper>(); // Register OAuthHelper as a transient service

var app = builder.Build();

// Build the configuration
var configBuilder = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<Service>();
    serviceBuilder.AddServiceEndpoint<Service, IService>(
        new BasicHttpBinding(BasicHttpSecurityMode.None), 
        "/Service.svc"
        );

    serviceBuilder.AddService<QuoteConnector>();
    serviceBuilder.AddServiceEndpoint<QuoteConnector, IOnlineQuoteConnector>(
    new BasicHttpBinding(BasicHttpSecurityMode.None), 
    "/QuoteConnector.svc"
    );
    
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled  = true;
});

// Retrieve OAuthHelper and log settings to verify
var oauthHelper = app.Services.GetRequiredService<IOAuthHelper>();
oauthHelper.LogSettings();

await oauthHelper.InitializeOpenIdConnectEndpointsAsync();

app.Run();
