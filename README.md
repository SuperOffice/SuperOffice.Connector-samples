# SuperOffice.Connector.Connector-samples

This repository contains samples for creating a *basic* QuoteConnector for SuperOffice Online. 
QuoteConnectors for SuperOffice Online uses WCF/SOAP, and since .net Core does not natively support WCF, this projects makes use of the community-driven project [coreWCF](https://github.com/CoreWCF/CoreWCF) to add this support. CoreWCF has been picked up by microsoft and is now part of their [support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/corewcf), but SuperOffice does not have anything to do with this support directly.

*If you experience any problems, please provide as much details about your situation in an email to appdev@SUPEROFFICE.COM.  The more details you provide, the less email ping-pong there will be for follow-up questions requesting missing information.*

## Quick-Start

1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution
4. Publish to IIS
5. Setup NGROK
6. Access the application

## [SuperOffice.Connector.JsonQuoteConnector](./SuperOffice.Connector.JsonQuoteConnector)

The JsonQuoteConnector is a minimalistic hard-coded example that implements all of the required interfaces for a Quote Connector. Note that this is a simple example and does not handle all possible scenarios, and is built on .net Framework 4.8.

## [SuperOffice.Connector.Blazor](./SuperOffice.Connector.Blazor)

The Blazor project is a simple Blazor Server application that demonstrates how to add WCF using coreWCF. The WCF service in this application handles the authentication/authorization for the connetor, and levearages the JsonQuoteConnector for the implementation.

The app itself is the default Blazor Server template, with the addition of a button in top-right which gives you the option to `Login`.
This will forward you to the SuperOffice Gateway, and when the callback is received, the user is logged in and the button changes to `Logout` and displayes the users email from the returned Claims.

It also takes care of storing some properties from the returned token into a file located at `App_Data\context.json`. Storing this token is usefull to support multi-tenancy, and should NOT BE USED IN PRODUCTION. Store the token somewhere else/secure instead..
![ProjectImage](/Resources/frontpage.png)

The values stored in context.json are:

- `Token`: The token received from the SuperOffice Gateway
- `ContextIdentifier`: The context identifier for the tenant
- `NetServerUrl`: The NetServer URL for the tenant
- `WebApiUrl`: The WebApi URL for the tenant
- `SystemToken`: The systemUser token for the tenant

These values are stored to later be used to fetch additional information from the tenant. In some cases the quoteconnector need more context to provide completionItems back to SuperOffice, and the systemUser can be used to fetch this information.
This project does not show how to do the subsequent calls!

## NGROK

Even though it does not matter where your application is hosted, during development its handy to host the application locally and debug it.
To test the connector from an online tenant you can host the blazor application on your local machine/IIS and use [NGROK](https://ngrok.com/) to create a tunnel to your local environment. Please refer to NGROK documentation for how to set it up.