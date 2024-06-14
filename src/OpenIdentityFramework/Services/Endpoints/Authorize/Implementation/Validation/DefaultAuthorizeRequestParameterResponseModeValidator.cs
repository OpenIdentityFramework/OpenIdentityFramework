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

public class DefaultAuthorizeRequestParameterResponseModeValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterResponseModeValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestParameterResponseModeValidator(IOptionsMonitor<OpenIdentityFrameworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    protected virtual IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }

    public virtual async Task<Result<string, ProtocolError>> ValidateResponseModeAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        IReadOnlySet<string> responseType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestParameters);
        ArgumentNullException.ThrowIfNull(responseType);
        cancellationToken.ThrowIfCancellationRequested();
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // https://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#rfc.section.2.1
        // response_mode - OPTIONAL. Informs the Authorization Server of the mechanism to be used
        // for returning parameters from the Authorization Endpoint.
        var defaultResponseMode = await InferDefaultResponseModeAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            responseType,
            cancellationToken);
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (!requestParameters.TryGetValue(DefaultAuthorizeRequestParameter.ResponseMode, out var responseModeValues) || responseModeValues.Count == 0)
        {
            return new(defaultResponseMode);
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (responseModeValues.Count is not 1)
        {
            return MultipleResponseModeValuesNotAllowed();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        var responseMode = responseModeValues.ToString();
        if (string.IsNullOrEmpty(responseMode))
        {
            return new(defaultResponseMode);
        }

        // length check
        if (responseMode.Length > Options.CurrentValue.InputLengthRestrictions.ResponseMode)
        {
            return ResponseModeIsTooLong();
        }

        return responseMode switch
        {
            DefaultResponseMode.Fragment => new(DefaultResponseMode.Fragment),
            DefaultResponseMode.Query => new(DefaultResponseMode.Query),
            DefaultResponseMode.FormPost => new(DefaultResponseMode.FormPost),
            _ => UnsupportedResponseMode()
        };
    }

    protected virtual Task<string> InferDefaultResponseModeAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        IReadOnlySet<string> responseType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responseType);
        cancellationToken.ThrowIfCancellationRequested();
        if (responseType.Count == 1 && responseType.Contains(DefaultResponseType.IdToken))
        {
            // https://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#rfc.section.3
            // The default Response Mode for this Response Type is the "fragment" encoding and the query encoding MUST NOT be used
            return Task.FromResult(DefaultResponseMode.Fragment);
        }

        if (responseType.Count == 2
            && responseType.Contains(DefaultResponseType.Code)
            && responseType.Contains(DefaultResponseType.IdToken))
        {
            // https://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#rfc.section.5
            // code id_token - When supplied as the value for the response_type parameter,
            // a successful response MUST include both an Authorization Code and an id_token.
            // The default Response Mode for this Response Type is the "fragment" encoding
            // and the query encoding MUST NOT be used.
            // Both successful and error responses SHOULD be returned using the supplied Response Mode,
            // or if none is supplied, using the default Response Mode.
            return Task.FromResult(DefaultResponseMode.Fragment);
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.2
        // If the resource owner grants the access request, the authorization server issues an authorization code
        // and delivers it to the client by adding the following parameters to the query component of the redirect URI
        // using the application/x-www-form-urlencoded format
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.2.1
        // If the resource owner denies the access request or if the request fails for reasons other than a missing or invalid redirect URI,
        // the authorization server informs the client by adding the following parameters to the query component of the redirect URI
        // using the application/x-www-form-urlencoded format
        // https://openid.net/specs/oauth-v2-multiple-response-types-1_0.html#rfc.section.2.1
        // For purposes of this specification, the default Response Mode for the OAuth 2.0 "code" Response Type is the "query" encoding.
        return Task.FromResult(DefaultResponseMode.Query);
    }

    protected virtual Result<string, ProtocolError> MultipleResponseModeValuesNotAllowed()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Multiple \"response_mode\" values are present, but only one is allowed"));
    }

    protected virtual Result<string, ProtocolError> ResponseModeIsTooLong()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"response_mode\" is too long"));
    }


    protected virtual Result<string, ProtocolError> UnsupportedResponseMode()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Unsupported \"response_mode\""));
    }
}