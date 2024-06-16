using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenIdentityFramework.Services.SyntaxValidation;

public static class ClientRedirectUriSyntaxValidator
{
    public static bool IsValid(string value, [NotNullWhen(true)] out Uri? uri)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-2.3
        // The redirect URI MUST be an absolute URI as defined by [RFC3986] Section 4.3.
        // The redirect URI MAY include an "application/x-www-form-urlencoded" formatted query component ([WHATWG.URL]), which MUST be retained when adding additional query parameters.
        // The redirect URI MUST NOT include a fragment component.
        if (Uri.TryCreate(value, UriKind.Absolute, out var typedRedirectUri)
            && typedRedirectUri.IsWellFormedOriginalString()
            && string.IsNullOrEmpty(typedRedirectUri.Fragment))
        {
            uri = typedRedirectUri;
            return true;
        }

        uri = null;
        return false;
    }
}