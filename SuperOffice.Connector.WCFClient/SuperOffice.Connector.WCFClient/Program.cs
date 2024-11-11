// See https://aka.ms/new-console-template for more information
using ServiceReference1;
using ServiceReference2;

Console.WriteLine("Hello, World!");

// Instantiate the Service wrapper specifying the binding and optionally the Endpoint URL. The BasicHttpBinding could be used instead.
//var client = new ServiceClient(ServiceClient.EndpointConfiguration.BasicHttpBinding_IService, "http://localhost:5032/Services/Service.svc");
//var result = await client.GetDataAsync(1);

var client = new OnlineQuoteConnectorClient(OnlineQuoteConnectorClient.EndpointConfiguration.BasicHttpBinding_IOnlineQuoteConnector, "http://localhost:82/services/quoteconnector.svc");

var connectionConfigFields = new ConnectionConfigFields();

var connectionData = new QuoteConnectionInfo();
int connectionId = 1;
string contextIdentifier = "context";
string sessionId = "session";
var userInfo = new UserInfo();

var result = await client.TestConnectionAsync(connectionConfigFields, connectionData, connectionId, contextIdentifier, sessionId, userInfo);
Console.WriteLine(result.Result);
Console.WriteLine(result);
