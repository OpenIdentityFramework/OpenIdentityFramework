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

public class DefaultAuthorizeRequestParameterStateValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterStateValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestParameterStateValidator(IOptionsMonitor<OpenIdentityFrameworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    protected virtual IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }

    public virtual Task<Result<string?, ProtocolError>> ValidateStateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestParameters);
        cancellationToken.ThrowIfCancellationRequested();
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // "state" - OPTIONAL. An opaque value used by the client to maintain state between the request and callback.
        // The authorization server includes this value when redirecting the user agent back to the client.

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (!requestParameters.TryGetValue(DefaultAuthorizeRequestParameter.State, out var stateValues) || stateValues.Count == 0)
        {
            return Task.FromResult(new Result<string?, ProtocolError>((string?) null));
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (stateValues.Count is not 1)
        {
            return Task.FromResult(MultipleStateValuesNotAllowed());
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        var state = stateValues.ToString();
        if (string.IsNullOrEmpty(state))
        {
            return Task.FromResult(new Result<string?, ProtocolError>((string?) null));
        }

        // length check
        if (state.Length > Options.CurrentValue.InputLengthRestrictions.State)
        {
            return Task.FromResult(StateIsTooLong());
        }

        if (!StateSyntaxValidator.IsValid(state))
        {
            return Task.FromResult(InvalidStateSyntax());
        }

        return Task.FromResult(new Result<string?, ProtocolError>(state));
    }

    protected virtual Result<string?, ProtocolError> MultipleStateValuesNotAllowed()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Multiple \"state\" values are present, but only one is allowed"));
    }

    protected virtual Result<string?, ProtocolError> StateIsTooLong()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"state\" is too long"));
    }

    protected virtual Result<string?, ProtocolError> InvalidStateSyntax()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Invalid \"state\" syntax"));
    }
}