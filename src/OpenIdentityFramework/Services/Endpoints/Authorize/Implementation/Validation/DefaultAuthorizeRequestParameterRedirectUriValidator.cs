using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Implementation.Validation;

public class DefaultAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient>
    : IAuthorizeRequestParameterRedirectUriValidator<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public Task<Result<string, ProtocolError>> ValidateRedirectUriAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotImplementedException();
    }
}