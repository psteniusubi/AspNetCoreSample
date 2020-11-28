# ASP.NET Core 3.1 and Ubisecure SSO integration with OpenID Connect

## Introduction

This is a sample ASP.NET Core 3.1 application to illustrate integration with OpenID Connect Auhthorization Code flow. 

The [aspnet21 branch](../../tree/aspnet21) has a previous version for ASP.NET Core 2.1.

## Configuration

An OpenID Connect Client needs to be configured with information about the OpenID Connect Provider and client credentials. This sample app puts these configuration items into [appsettings.json](appsettings.json) file as properties of OpenIDConnect key:

* `issuer` - name of OpenID Connect Provider
* `client_id` and `client_secret` - client credentials registered with OpenID Connect Provider 
* `redirect_uri` - this value must match deployment and is registered with OpenID Connect Provider

```json
{
  "OpenIDConnect": {
    "issuer": "https://login.example.ubidemo.com/uas",
    "client_id": "public",
    "client_secret": "public",
    "redirect_uri": "http://localhost:19282/public"
  }
}
```

## Code review

Most code files are as generated by the Visual Studio 2019 ASP.NET Core wizard. The files modified for this integration are

* [Startup.cs](Startup.cs)
* [Controllers/HomeController.cs](Controllers/HomeController.cs)
* [Views/Home/Index.cshtml](Views/Home/Index.cshtml)

### Startup.cs

The following indicates [OpenID Connect](https://docs.microsoft.com/en-us/aspnet/core/api/microsoft.aspnetcore.authentication.openidconnect) is used to authenticate new anonymous users trying to access the application. 
Cookies are used to persist an authenticated session. 
Do review details of [ASP.NET Core cookie authentication](https://docs.microsoft.com/en-us/aspnet/core/api/microsoft.aspnetcore.authentication.cookies) before going into production: how large will the cookie or cookies become and how is their integrity protected?

```c#
                .AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
```

Here I'm setting up the built-in OpenID Connect client of ASP.NET Core to be interoperable with Ubisecure SSO. I'm reading some settings from appsettings.json. 

```c#
                .AddOpenIdConnect(options =>
                {
                    var oidc = Configuration.GetSection("OpenIDConnect");
                    var redirectUri = new Uri(oidc.GetValue<string>("redirect_uri"));
                    options.Authority = oidc.GetValue<string>("issuer");
                    options.ClientId = oidc.GetValue<string>("client_id");
                    options.ClientSecret = oidc.GetValue<string>("client_secret");
                    options.CallbackPath = redirectUri.AbsolutePath;
                    options.ResponseType = "code";
                    options.ResponseMode = null;
                    options.DisableTelemetry = true;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    // workaround for https://github.com/dotnet/aspnetcore/issues/19939
                    if (!"https".Equals(redirectUri.Scheme) && redirectUri.IsLoopback)
                    {
                        options.CorrelationCookie = new CookieBuilderFilter(options.CorrelationCookie);
                        options.NonceCookie = new CookieBuilderFilter(options.NonceCookie);
                    }
                })
                .AddCookie(); 

```

Note that the previous code has a SameSite cookie workaround that lets this app run on http://localhost.

Make sure your Configure method enables authentication and authorization.

```c#
            app.UseAuthentication();

            app.UseAuthorization();
```

### HomeController.cs

`HomeController` has a single operation that sets the model to current user. `[Authorize]` tag tells the ASP.NET middleware that access to this controller requires authentication.

```c#
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(User as ClaimsPrincipal);
        }
    }
```

### Index.cshtml

The following generates a simple html list showing all [claims](https://docs.microsoft.com/en-us/dotnet/api/system.security.claims.claimsprincipal) received from OpenID Connect provider

```cshtml
@model System.Security.Claims.ClaimsPrincipal

<!DOCTYPE html>

<html>
<body>
    <h1>Welcome</h1>
    <dl>
        @foreach (var claim in Model.Claims)
        {
            <dt><b>@claim.Type</b></dt>
            <dd><i>@claim.Value</i></dd>
        }
    </dl>
</body>
</html>
```

## Launching

Use Visual Studio 2019 to launch AspNetCoreSample application on http://localhost:19282

This application is also deployed live on Azure Web Apps at https://ubi-aspnet-core-sample.azurewebsites.net

### Command line

You first need to install [Git tools](https://git-scm.com/downloads) and [ASP.NET Core runtime](https://dotnet.microsoft.com/download)

The following will launch the application on http://localhost:19282

```
git clone https://github.com/psteniusubi/AspNetCoreSample.git
cd AspNetCoreSample
dotnet run
```
