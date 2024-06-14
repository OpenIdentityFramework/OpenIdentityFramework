using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

public interface IAuthorizeRequestParameterRedirectUriValidator<in TOperationContext, in TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    Task<Result<string, ProtocolError>> ValidateRedirectUriAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        CancellationToken cancellationToken);
}