using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Models.AuthorizeRequestValidator;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation;

public class DefaultAuthorizeRequestValidator<TOperationContext, TClient>
    : IAuthorizeRequestValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeRequestValidator(
        IAuthorizeRequestRequiredParametersValidator<TOperationContext, TClient> requiredParametersValidator)
    {
        ArgumentNullException.ThrowIfNull(requiredParametersValidator);
        RequiredParametersValidator = requiredParametersValidator;
    }

    protected virtual IAuthorizeRequestRequiredParametersValidator<TOperationContext, TClient> RequiredParametersValidator { get; }

    public virtual async Task<Result<ValidatedAuthorizeRequest<TClient>, AuthorizeRequestValidationError<TClient>>> ValidateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requiredParametersResult = await RequiredParametersValidator.ValidateAsync(
            httpContext,
            operationContext,
            requestParameters,
            cancellationToken);
        if (requiredParametersResult.HasError)
        {
            return new(new AuthorizeRequestValidationError<TClient>(requiredParametersResult.Error));
        }

        var requiredParameters = requiredParametersResult.Ok;
        throw new NotImplementedException();
    }
}