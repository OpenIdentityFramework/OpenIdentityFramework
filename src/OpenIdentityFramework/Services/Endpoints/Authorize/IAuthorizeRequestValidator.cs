using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Services.Endpoints.Authorize.Models.AuthorizeRequestValidator;

namespace OpenIdentityFramework.Services.Endpoints.Authorize;

public interface IAuthorizeRequestValidator<in TOperationContext>
    where TOperationContext : class, IOperationContext
{
    Task<AuthorizeRequestValidationResult> ValidateAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        CancellationToken cancellationToken);
}