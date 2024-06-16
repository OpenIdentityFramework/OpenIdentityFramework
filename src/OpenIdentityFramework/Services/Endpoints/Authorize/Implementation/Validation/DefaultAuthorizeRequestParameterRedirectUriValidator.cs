using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Constants.Request;
using OpenIdentityFramework.Constants.Response.Errors;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;
using OpenIdentityFramework.Services.SyntaxValidation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation.Validation;

public class DefaultAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestParameterRedirectUriValidator(IOptionsMonitor<OpenIdentityFrameworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    protected virtual IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }

    public virtual async Task<Result<string, ProtocolError>> ValidateRedirectUriAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-2.3.1
        // Authorization servers MUST require clients to register their complete redirect URI (including the path component).
        // Authorization servers MUST reject authorization requests that specify a redirect URI
        // that doesn't exactly match one that was registered, with an exception for loopback redirects,
        // where an exact match is required except for the port URI component, see Section 4.1.1 for details.
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1
        // "redirect_uri": OPTIONAL if only one redirect URI is registered for this client.
        // REQUIRED if multiple redirect URIs are registered for this client. See Section 2.3.2.
        // In particular, the authorization server MUST validate the "redirect_uri" in the request if present,
        // ensuring that it matches one of the registered redirect URIs previously established during client registration (Section 2).
        // When comparing the two URIs the authorization server MUST ensure that the two URIs are equal, see [RFC3986], Section 6.2.1,
        // Simple String Comparison, for details.
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-8.4.2
        // The authorization server MUST allow any port to be specified at the time of the request for loopback IP redirect URIs,
        // to accommodate clients that obtain an available ephemeral port from the operating system at the time of the request.
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-8.4.3
        // To perform an authorization request with a private-use URI scheme redirect,
        // the native app launches the browser with a standard authorization request,
        // but one where the redirect URI utilizes a private-use URI scheme it registered with the operating system.
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // redirect_uri - REQUIRED. Redirection URI to which the response will be sent.
        // This URI MUST exactly match one of the Redirection URI values for the Client pre-registered at the OpenID Provider,
        // with the matching performed as described in Section 6.2.1 of [RFC3986] (Simple String Comparison).
        // When using this flow, the Redirection URI SHOULD use the https scheme; however, it MAY use the http scheme,
        // provided that the Client Type is confidential, as defined in Section 2.1 of OAuth 2.0,
        // and provided the OP allows the use of http Redirection URIs in this case.
        // Also, if the Client is a native application, it MAY use the http scheme with localhost
        // or the IP loopback literals 127.0.0.1 or [::1] as the hostname.
        // The Redirection URI MAY use an alternate scheme, such as one that is intended
        // to identify a callback into a native application.
        // ------------------
        // Microsoft, redirect_uri - required https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow#request-an-authorization-code
        // AWS, redirect_uri - required https://docs.aws.amazon.com/cognito/latest/developerguide/authorization-endpoint.html#get-authorize
        // Google, redirect_uri - required https://developers.google.com/identity/protocols/oauth2/web-server#httprest_1
        // Okta, redirect_uri - required https://developer.okta.com/docs/reference/api/oidc/#authorize
        // ------------------
        // Despite OAuth 2.1 specification making the redirect_uri parameter optional, popular implementations explicitly require it.
        // To distinguish OAuth 2.1 from OpenID Connect 1.0, scope validation is necessary (it must include the openid value).
        // Errors may occur during the scope validation process.
        // As OAuth 2.1 is a framework, making this parameter mandatory would not violate the specification.
        ArgumentNullException.ThrowIfNull(requestParameters);
        cancellationToken.ThrowIfCancellationRequested();
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (!requestParameters.TryGetValue(DefaultAuthorizeRequestParameter.RedirectUri, out var redirectUriValues) || redirectUriValues.Count == 0)
        {
            return RedirectUriIsMissing();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (redirectUriValues.Count is not 1)
        {
            return MultipleRedirectUriValuesNotAllowed();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        var redirectUri = redirectUriValues.ToString();
        if (string.IsNullOrEmpty(redirectUri))
        {
            return RedirectUriIsMissing();
        }

        // length check
        if (redirectUri.Length > Options.CurrentValue.InputLengthRestrictions.RedirectUri)
        {
            return RedirectUriIsTooLong();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#appendix-A.6
        // "redirect_uri" syntax validation
        if (!ClientRedirectUriSyntaxValidator.IsValid(redirectUri, out var typedRedirectUri))
        {
            return InvalidRedirectUriSyntax();
        }

        var clientRedirectUris = await GetClientRedirectUrisAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-8.5.4
        // Loopback interface redirect URIs MAY use the http scheme (i.e., without TLS).
        // This is acceptable for loopback interface redirect URIs as the HTTP request never leaves the device.
        if (typedRedirectUri.IsLoopback)
        {
            foreach (var clientRedirectUri in clientRedirectUris)
            {
                // Ignore port for loopback
                if (ClientRedirectUriSyntaxValidator.IsValid(clientRedirectUri, out var typedClientRedirectUri)
                    && typedClientRedirectUri.IsLoopback
                    && typedClientRedirectUri.Scheme == typedRedirectUri.Scheme
                    && typedClientRedirectUri.Host == typedRedirectUri.Host
                    && typedClientRedirectUri.PathAndQuery == typedRedirectUri.PathAndQuery)
                {
                    return new(redirectUri);
                }
            }
        }
        else
        {
            // http scheme allowed only for loopback redirects
            if (typedRedirectUri.Scheme == "http")
            {
                return InvalidRedirectUri();
            }

            if (clientRedirectUris.Contains(redirectUri))
            {
                return new(redirectUri);
            }
        }

        return InvalidRedirectUri();
    }

    protected virtual Task<IReadOnlySet<string>> GetClientRedirectUrisAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        var clientRedirectUris = client.GetRedirectUris();
        if (clientRedirectUris is null || clientRedirectUris.Count == 0)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }

        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(clientRedirectUris, StringComparer.Ordinal));
    }

    protected virtual Result<string, ProtocolError> RedirectUriIsMissing()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"redirect_uri\" is missing"));
    }

    protected virtual Result<string, ProtocolError> MultipleRedirectUriValuesNotAllowed()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Multiple \"redirect_uri\" values are present, but only one is allowed"));
    }

    protected virtual Result<string, ProtocolError> RedirectUriIsTooLong()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"redirect_uri\" is too long"));
    }

    protected virtual Result<string, ProtocolError> InvalidRedirectUriSyntax()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Invalid \"redirect_uri\" syntax"));
    }

    protected virtual Result<string, ProtocolError> InvalidRedirectUri()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Invalid \"redirect_uri\""));
    }
}