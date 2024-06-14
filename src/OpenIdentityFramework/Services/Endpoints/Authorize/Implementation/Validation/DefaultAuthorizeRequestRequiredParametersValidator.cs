using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Models.Validation;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation.Validation;

public class DefaultAuthorizeRequestRequiredParametersValidator<TOperationContext, TClient>
    : IAuthorizeRequestRequiredParametersValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestRequiredParametersValidator(
        IAuthorizeRequestParameterClientIdValidator<TOperationContext, TClient> clientIdValidator,
        IAuthorizeRequestParameterResponseTypeValidator<TOperationContext, TClient> responseTypeValidator,
        IAuthorizeRequestParameterResponseModeValidator<TOperationContext, TClient> responseModeValidator,
        IAuthorizeRequestParameterStateValidator<TOperationContext, TClient> stateValidator)
    {
        ArgumentNullException.ThrowIfNull(clientIdValidator);
        ArgumentNullException.ThrowIfNull(responseTypeValidator);
        ArgumentNullException.ThrowIfNull(responseModeValidator);
        ArgumentNullException.ThrowIfNull(stateValidator);
        ClientIdValidator = clientIdValidator;
        ResponseTypeValidator = responseTypeValidator;
        ResponseModeValidator = responseModeValidator;
        StateValidator = stateValidator;
    }

    protected virtual IAuthorizeRequestParameterClientIdValidator<TOperationContext, TClient> ClientIdValidator { get; }
    protected virtual IAuthorizeRequestParameterResponseTypeValidator<TOperationContext, TClient> ResponseTypeValidator { get; }
    protected virtual IAuthorizeRequestParameterResponseModeValidator<TOperationContext, TClient> ResponseModeValidator { get; }
    protected virtual IAuthorizeRequestParameterStateValidator<TOperationContext, TClient> StateValidator { get; }

    public virtual async Task<Result<AuthorizeRequestRequiredParameters<TClient>, ProtocolError>> ValidateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clientId = await ClientIdValidator.ValidateClientIdAsync(
            httpContext,
            operationContext,
            requestParameters,
            cancellationToken);
        if (clientId.HasError)
        {
            return new(clientId.Error);
        }

        var client = clientId.Ok;
        var responseType = await ResponseTypeValidator.ValidateResponseTypeAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        if (responseType.HasError)
        {
            return new(responseType.Error);
        }

        var responseTypes = responseType.Ok;
        var responseMode = await ResponseModeValidator.ValidateResponseModeAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            responseTypes,
            cancellationToken);
        if (responseMode.HasError)
        {
            return new(responseMode.Error);
        }

        var state = await StateValidator.ValidateStateAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        if (state.HasError)
        {
            return new(state.Error);
        }

        throw new NotImplementedException();
    }
}