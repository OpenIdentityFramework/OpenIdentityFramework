using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Constants;
using OpenIdentityFramework.Constants.Request;
using OpenIdentityFramework.Constants.Response.Errors;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation.Validation;

public class DefaultAuthorizeRequestParameterResponseTypeValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterResponseTypeValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestParameterResponseTypeValidator(IOptionsMonitor<OpenIdentityFrameworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    protected virtual IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }

    public virtual async Task<Result<IReadOnlySet<string>, ProtocolError>> ValidateResponseTypeAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestParameters);
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1 (Authorization Code)
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1 (Authorization Code)
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.3.2.1 (Hybrid Flow)
        // response_type - REQUIRED in both specs

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (!requestParameters.TryGetValue(DefaultAuthorizeRequestParameter.ResponseType, out var responseTypeValues) || responseTypeValues.Count == 0)
        {
            return ResponseTypeIsMissing();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (responseTypeValues.Count is not 1)
        {
            return MultipleResponseTypeValuesNotAllowed();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        var responseTypeString = responseTypeValues.ToString();
        if (string.IsNullOrEmpty(responseTypeString))
        {
            return ResponseTypeIsMissing();
        }

        // length check
        if (responseTypeString.Length > Options.CurrentValue.InputLengthRestrictions.ResponseType)
        {
            return ResponseTypeIsTooLong();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1
        // This specification defines the value "code", which must be used to signal that the client wants to use the authorization code flow.
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // When using the Authorization Code Flow, this value is "code".
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.3.2.1
        // When using the Hybrid Flow, this value is "code id_token"
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.2.2.1
        // When using the Implicit Flow, this value is "id_token"
        var parsedResponseTypes = responseTypeString.Split(' ');
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1
        // Extension response types MAY contain a space-delimited (%x20) list of values,
        // where the order of values does not matter (e.g., response type a b is the same as b a).
        // The meaning of such composite response types is defined by their respective specifications.
        var supportedResponseTypesResult = await GetSupportedResponseTypesAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            parsedResponseTypes,
            cancellationToken);
        if (supportedResponseTypesResult.HasError)
        {
            return InvalidResponseType();
        }

        var supportedResponseTypes = supportedResponseTypesResult.Ok;
        // "code" - required for authorization code flow
        // "id_token" - required for implicit flow, that returns only id_token, or for OIDC hybrid flow
        if (!supportedResponseTypes.Contains(DefaultResponseType.Code)
            && !supportedResponseTypes.Contains(DefaultResponseType.IdToken))
        {
            return UnsupportedResponseType();
        }

        return new(supportedResponseTypes);
    }

    protected virtual async Task<Result<IReadOnlySet<string>>> GetSupportedResponseTypesAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        string[] parsedResponseTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parsedResponseTypes);
        cancellationToken.ThrowIfCancellationRequested();
        var clientGrantTypes = await GetClientGrantTypesAsync(httpContext, operationContext, requestParameters, client, cancellationToken);
        var clientResponseTypes = await GetClientResponseTypesAsync(httpContext, operationContext, requestParameters, client, cancellationToken);
        var supportedResponseTypes = new HashSet<string>(StringComparer.Ordinal);
        var processedResponseTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parsedResponseType in parsedResponseTypes)
        {
            // check for duplicates
            if (!processedResponseTypes.Add(parsedResponseType))
            {
                return new();
            }

            switch (parsedResponseType)
            {
                case DefaultResponseType.Code:
                    {
                        if (clientResponseTypes.Contains(DefaultResponseType.Code) && clientGrantTypes.Contains(DefaultGrantType.AuthorizationCode))
                        {
                            supportedResponseTypes.Add(DefaultResponseType.Code);
                        }

                        break;
                    }
                case DefaultResponseType.IdToken:
                    {
                        if (clientResponseTypes.Contains(DefaultResponseType.IdToken) && clientGrantTypes.Contains(DefaultGrantType.Implicit))
                        {
                            supportedResponseTypes.Add(DefaultResponseType.IdToken);
                        }

                        break;
                    }
            }
        }

        return new(supportedResponseTypes);
    }

    protected virtual Task<IReadOnlySet<string>> GetClientGrantTypesAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        // https://openid.net/specs/openid-connect-registration-1_0-errata2.html#rfc.section.2
        // grant_types - JSON array containing a list of the OAuth 2.0 Grant Types that the Client is declaring that it will restrict itself to using.
        // https://www.rfc-editor.org/rfc/rfc7591.html#section-2
        // grant_types - Array of OAuth 2.0 grant type strings that the client can use at the token endpoint.
        var grantTypes = client.GetGrantTypes();
        // If omitted, the default is that the Client will use only the authorization_code Grant Type.
        if (grantTypes is null || grantTypes.Count == 0)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal)
            {
                DefaultGrantType.AuthorizationCode
            });
        }

        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(grantTypes, StringComparer.Ordinal));
    }

    protected virtual Task<IReadOnlySet<string>> GetClientResponseTypesAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        // https://openid.net/specs/openid-connect-registration-1_0-errata2.html#rfc.section.2
        // response_types - JSON array containing a list of the OAuth 2.0 "response_type" values that the Client is declaring that it will restrict itself to using.
        // https://www.rfc-editor.org/rfc/rfc7591.html#section-2
        // response_types - Array of the OAuth 2.0 response type strings that the client can use at the authorization endpoint.
        var configuredResponseTypes = client.GetResponseTypes();
        // If omitted, the default is that the Client will use only the "code" Response Type.
        if (configuredResponseTypes is null || configuredResponseTypes.Count == 0)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal)
            {
                DefaultResponseType.Code
            });
        }

        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(configuredResponseTypes, StringComparer.Ordinal));
    }

    protected virtual Result<IReadOnlySet<string>, ProtocolError> ResponseTypeIsMissing()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"response_type\" is missing"));
    }

    protected virtual Result<IReadOnlySet<string>, ProtocolError> MultipleResponseTypeValuesNotAllowed()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Multiple \"response_type\" values are present, but only one is allowed"));
    }

    protected virtual Result<IReadOnlySet<string>, ProtocolError> ResponseTypeIsTooLong()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"response_type\" is too long"));
    }

    protected virtual Result<IReadOnlySet<string>, ProtocolError> UnsupportedResponseType()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.UnsupportedResponseType, "Unsupported \"response_type\""));
    }

    protected virtual Result<IReadOnlySet<string>, ProtocolError> InvalidResponseType()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Invalid \"response_type\""));
    }
}