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
        IAuthorizeRequestParameterStateValidator<TOperationContext, TClient> stateValidator,
        IAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient> redirectUriValidator)
    {
        ArgumentNullException.ThrowIfNull(clientIdValidator);
        ArgumentNullException.ThrowIfNull(responseTypeValidator);
        ArgumentNullException.ThrowIfNull(responseModeValidator);
        ArgumentNullException.ThrowIfNull(stateValidator);
        ArgumentNullException.ThrowIfNull(redirectUriValidator);
        ClientIdValidator = clientIdValidator;
        ResponseTypeValidator = responseTypeValidator;
        ResponseModeValidator = responseModeValidator;
        StateValidator = stateValidator;
        RedirectUriValidator = redirectUriValidator;
    }

    protected virtual IAuthorizeRequestParameterClientIdValidator<TOperationContext, TClient> ClientIdValidator { get; }
    protected virtual IAuthorizeRequestParameterResponseTypeValidator<TOperationContext, TClient> ResponseTypeValidator { get; }
    protected virtual IAuthorizeRequestParameterResponseModeValidator<TOperationContext, TClient> ResponseModeValidator { get; }
    protected virtual IAuthorizeRequestParameterStateValidator<TOperationContext, TClient> StateValidator { get; }
    protected virtual IAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient> RedirectUriValidator { get; }

    public virtual async Task<Result<AuthorizeRequestRequiredParameters<TClient>, ProtocolError>> ValidateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var clientIdResult = await ClientIdValidator.ValidateClientIdAsync(
            httpContext,
            operationContext,
            requestParameters,
            cancellationToken);
        if (clientIdResult.HasError)
        {
            return new(clientIdResult.Error);
        }

        var client = clientIdResult.Ok;
        var responseTypeResult = await ResponseTypeValidator.ValidateResponseTypeAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        if (responseTypeResult.HasError)
        {
            return new(responseTypeResult.Error);
        }

        var responseType = responseTypeResult.Ok;
        var responseModeResult = await ResponseModeValidator.ValidateResponseModeAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            responseType,
            cancellationToken);
        if (responseModeResult.HasError)
        {
            return new(responseModeResult.Error);
        }

        var stateResult = await StateValidator.ValidateStateAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        if (stateResult.HasError)
        {
            return new(stateResult.Error);
        }

        var redirectUriResult = await RedirectUriValidator.ValidateRedirectUriAsync(
            httpContext,
            operationContext,
            requestParameters,
            client,
            cancellationToken);
        if (redirectUriResult.HasError)
        {
            return new(redirectUriResult.Error);
        }

        var result = new AuthorizeRequestRequiredParameters<TClient>(
            client,
            responseType,
            stateResult.Ok,
            responseModeResult.Ok,
            redirectUriResult.Ok);
        return new(result);
    }
}