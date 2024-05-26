using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace OpenIdentityFramework.Extensions;

public static class HttpRequestExtensions
{
    public static bool HasApplicationFormContentType(this HttpRequest request)
    {
        // https://github.com/dotnet/aspnetcore/blob/v8.0.5/src/Http/Http/src/Features/FormFeature.cs#L315-L319
        ArgumentNullException.ThrowIfNull(request);
        return MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) && contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }
}