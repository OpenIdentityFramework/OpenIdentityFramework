﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Configuration.Options.Enums;

namespace OpenIdentityFramework.Extensions;

public static class HttpResponseExtensions
{
    public static void SetNoCache(this HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        response.Headers["Pragma"] = "no-cache";
    }

    public static void SetCache(this HttpResponse response, TimeSpan maxAge)
    {
        ArgumentNullException.ThrowIfNull(response);
        var durationInSeconds = maxAge.Ticks / TimeSpan.TicksPerSecond;
        if (durationInSeconds <= 0)
        {
            SetNoCache(response);
            return;
        }

        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers["Cache-Control"] = $"max-age={durationInSeconds:D}";
        }

        string vary;
        if (response.Headers.TryGetValue("Vary", out var existingVary))
        {
            var existingVaryString = existingVary.ToString();
            vary = existingVaryString == "*"
                ? "*"
                : $"{existingVaryString}, Origin";
        }
        else
        {
            vary = "Origin";
        }

        response.Headers["Vary"] = vary;
    }

    public static void SetNoReferrer(this HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers["Referrer-Policy"] = "no-referrer";
    }

    public static void AddScriptCspHeaders(this HttpResponse response, ContentSecurityPolicyOptions cspOptions, string hash)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(cspOptions);
        var cspHeader = cspOptions.Level switch
        {
            ContentSecurityPolicyLevel.One => $"default-src 'none'; script-src 'unsafe-inline' '{hash}'",
            ContentSecurityPolicyLevel.Two => $"default-src 'none'; script-src '{hash}'",
            _ => throw new ArgumentOutOfRangeException(nameof(cspOptions.Level), "Invalid content security policy level")
        };
        response.Headers["Content-Security-Policy"] = cspHeader;
        if (cspOptions.AddDeprecatedHeader)
        {
            response.Headers["X-Content-Security-Policy"] = cspHeader;
        }
    }

    public static async Task WriteHtmlAsync(this HttpResponse response, string html, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(response);
        response.ContentType = "text/html; charset=UTF-8";
        await response.WriteAsync(html, Encoding.UTF8, cancellationToken);
    }
}
