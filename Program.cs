using AspNetCoreSample.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddOpenIdConnect(options =>
    {
        var oidc = builder.Configuration.GetSection("OpenIDConnect");
        var redirectUri = new Uri(oidc.GetValue<string>("redirect_uri") ?? "");
        options.Authority = oidc.GetValue<string>("issuer");
        options.ClientId = oidc.GetValue<string>("client_id");
        options.ClientSecret = oidc.GetValue<string>("client_secret");
        options.CallbackPath = redirectUri.AbsolutePath;
        options.ResponseType = "code";
        options.ResponseMode = "";
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
