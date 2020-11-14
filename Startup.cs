﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System;

namespace AspNetCoreSample
{
    /// <summary>
    /// Workaround for https://github.com/dotnet/aspnetcore/issues/19939
    /// Applies only when app is hosted on http://localhost
    /// </summary>
    public class CookieBuilderFilter : CookieBuilder
    {
        public CookieBuilderFilter(CookieBuilder cookieBuilder)
        {
            Domain = cookieBuilder.Domain;
            Expiration = cookieBuilder.Expiration;
            HttpOnly = cookieBuilder.HttpOnly;
            IsEssential = cookieBuilder.IsEssential;
            MaxAge = cookieBuilder.MaxAge;
            Name = cookieBuilder.Name;
            Path = cookieBuilder.Path;
            SameSite = cookieBuilder.SameSite;
            SecurePolicy = cookieBuilder.SecurePolicy;
        }
        public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
        {
            if (SameSite == SameSiteMode.None && !context.Request.IsHttps && SecurePolicy != CookieSecurePolicy.Always)
            {
                SameSite = SameSiteMode.Unspecified;
            }
            return base.Build(context, expiresFrom);
        }
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
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

            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
