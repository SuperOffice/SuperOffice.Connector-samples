using CoreWCF.Channels;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using SuperOffice.Connector.Blazor.Components;
using SuperOffice.Connector.Blazor.Services;
using SuperOffice.Online.IntegrationService.Contract;
using SuperOffice.SuperID.Contracts;
using System.Web.Services.Description;
using SuperOffice.Connector.Blazor.Utils;
using SuperOffice.Connector.Blazor.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SuperOffice.Contracts.UsageStats;

var builder = WebApplication.CreateBuilder(args);

// CoreWCF Services Setup
builder.Services.AddServiceModelServices()
                .AddServiceModelMetadata()
                .AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

// HTTP Client Factory
builder.Services.AddHttpClient(); // IHttpClientFactory is automatically available with AddHttpClient

// AppSettings Configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("Appsettings"));

// Application-Specific Services
builder.Services.AddTransient<IOAuthHelper, OAuthHelper>()
                .AddTransient<QuoteConnector>()
                .AddScoped<ISystemUserManager, SystemUserManager>();

// Razor Components
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// Retrieve the values from configuration directly
var appSettingsSection = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

//// Custom
builder.Services.AddHttpContextAccessor();

// Add authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = $"https://{builder.Configuration["AppSettings:Auth:Environment"]}.superoffice.com/login";
    options.ClientId = builder.Configuration["AppSettings:Auth:ClientId"];
    options.ClientSecret = builder.Configuration["AppSettings:Auth:ClientSecret"];
    options.CallbackPath = "/callback";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
    options.SaveTokens = true;

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Support for multi tenancy
            var uctx = context.HttpContext.Request.Query["uctx"];
            if (uctx.Count > 0)
            {
                context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.Replace("/login/common", "/login/" + uctx[0]);
            }

            return Task.FromResult(0);
        },
        OnTokenValidated = async context =>
        {
            // Intercept the callback and access token details
            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;

            var systemUserManager = context.HttpContext.RequestServices.GetRequiredService<ISystemUserManager>();
            systemUserManager.StoreTokens(claimsIdentity);

            // Store any values you need, such as custom claims or token details
            //var accessToken = context.TokenEndpointResponse.AccessToken;
            //var idToken = context.TokenEndpointResponse.IdToken;

            // Set RedirectUri to index page after successful authentication
            context.Properties.RedirectUri = "/";
        }
    };
});

var app = builder.Build();

//TODO: Figure out a better way to set this value
System.Configuration.ConfigurationManager.AppSettings["SuperIdCertificate"] = "16b7fb8c3f9ab06885a800c64e64c97c4ab5e98c";

// Retrieve OAuthHelper and run the initilize methods. TODO: Merge these two, they can be run in the same method
var oauthHelper = app.Services.GetRequiredService<IOAuthHelper>();
await oauthHelper.InitializeOpenIdConnectEndpointsAsync();
await oauthHelper.GetJsonWebKeysAsync();

app.UseServiceModel(serviceBuilder =>
{
    var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
    serviceBuilder.AddService<QuoteConnector>();
    serviceBuilder.AddServiceEndpoint<QuoteConnector, IOnlineQuoteConnector>(
    binding,
    "/Services/QuoteConnector.svc"
    );

    serviceBuilder.AddServiceEndpoint<QuoteConnector, IIntegrationServiceConnectorAuth>(
        binding,
        "/Services/QuoteConnector.svc"
        );

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Custom
app.UseAuthentication();
app.UseAuthorization();

app.Run();
