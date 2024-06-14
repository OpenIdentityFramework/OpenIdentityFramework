using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Services.Endpoints.Authorize.Models.AuthorizeRequestValidator;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation;

public class DefaultAuthorizeRequestValidator<TOperationContext> : IAuthorizeRequestValidator<TOperationContext>
    where TOperationContext : class, IOperationContext
{
    public virtual async Task<AuthorizeRequestValidationResult> ValidateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw new NotImplementedException();
    }
}