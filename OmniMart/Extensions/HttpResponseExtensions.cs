using Microsoft.AspNetCore.Http;
using System;

namespace OmniMart.API.Extensions;

public static class HttpResponseExtensions
{
    public static void SetAuthCookies(this HttpResponse response, string accessToken, string refreshToken)
    {
        var accessOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        };
        // We used the same name "AuthToken" to avoid breaking previous Hangfire settings.
        response.Cookies.Append("AuthToken", accessToken, accessOptions);

        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        response.Cookies.Append("RefreshToken", refreshToken, refreshOptions);
    }
}