using System;
using System.Collections.Generic;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Models.Validation;

// https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
public class AuthorizeRequestRequiredParameters<TClient>
    where TClient : AbstractClient
{
    public AuthorizeRequestRequiredParameters(TClient client, IReadOnlySet<string> responseType, string? state, string responseMode, string redirectUri)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(responseType);

        if (string.IsNullOrWhiteSpace(responseMode))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(responseMode));
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(redirectUri));
        }

        Client = client;
        ResponseType = responseType;
        State = state;
        ResponseMode = responseMode;
        RedirectUri = redirectUri;
    }

    public TClient Client { get; }

    public IReadOnlySet<string> ResponseType { get; }

    public string? State { get; }

    public string ResponseMode { get; }

    public string RedirectUri { get; }
}