using Microsoft.AspNetCore.Http;
using System;

namespace AspNetCoreSample.Utils
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
}
