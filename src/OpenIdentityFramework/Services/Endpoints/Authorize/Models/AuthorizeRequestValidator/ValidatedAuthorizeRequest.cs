using System.Collections.Generic;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Models.AuthorizeRequestValidator;

public class ValidatedAuthorizeRequest<TClient>
    where TClient : AbstractClient
{
    public ValidatedAuthorizeRequest(
        TClient client,
        IReadOnlySet<string> responseType,
        string? state,
        string responseMode,
        string redirectUri)
    {
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