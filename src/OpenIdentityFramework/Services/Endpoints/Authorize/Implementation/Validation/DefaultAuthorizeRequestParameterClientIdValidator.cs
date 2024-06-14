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
using OpenIdentityFramework.Services.Core;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;
using OpenIdentityFramework.Services.SyntaxValidation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation.Validation;

public class DefaultAuthorizeRequestParameterClientIdValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterClientIdValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestParameterClientIdValidator(
        IOptionsMonitor<OpenIdentityFrameworkOptions> options,
        IClientService<IOperationContext, TClient> clients)
    {
        Options = options;
        Clients = clients;
    }

    protected virtual IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }
    protected virtual IClientService<IOperationContext, TClient> Clients { get; }

    public virtual async Task<Result<TClient, ProtocolError>> ValidateClientIdAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestParameters);
        cancellationToken.ThrowIfCancellationRequested();
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // "client_id" - REQUIRED

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (!requestParameters.TryGetValue(DefaultAuthorizeRequestParameter.ClientId, out var clientIdValues) || clientIdValues.Count == 0)
        {
            return ClientIdIsMissing();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (clientIdValues.Count is not 1)
        {
            return MultipleClientIdValuesNotAllowed();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        var clientId = clientIdValues.ToString();
        if (string.IsNullOrEmpty(clientId))
        {
            return ClientIdIsMissing();
        }

        // length check
        if (clientId.Length > Options.CurrentValue.InputLengthRestrictions.ClientId)
        {
            return ClientIdIsTooLong();
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#appendix-A
        // "client_id" syntax validation
        if (!ClientIdSyntaxValidator.IsValid(clientId))
        {
            return InvalidClientIdSyntax();
        }

        var client = await Clients.FindEnabledAsync(operationContext, clientId, cancellationToken);
        if (client is null)
        {
            return UnknownOrDisabledClient();
        }

        return new(client);
    }

    protected virtual Result<TClient, ProtocolError> ClientIdIsMissing()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"client_id\" is missing"));
    }

    protected virtual Result<TClient, ProtocolError> MultipleClientIdValuesNotAllowed()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Multiple \"client_id\" values are present, but only one is allowed"));
    }

    protected virtual Result<TClient, ProtocolError> ClientIdIsTooLong()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "\"client_id\" is too long"));
    }

    protected virtual Result<TClient, ProtocolError> InvalidClientIdSyntax()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.InvalidRequest, "Invalid \"client_id\" syntax"));
    }

    protected virtual Result<TClient, ProtocolError> UnknownOrDisabledClient()
    {
        return new(new ProtocolError(DefaultAuthorizeEndpointError.UnauthorizedClient, "Unknown or disabled client"));
    }
}