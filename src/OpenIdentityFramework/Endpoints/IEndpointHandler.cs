using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Endpoints.Results;
using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Endpoints;

public interface IEndpointHandler<in TOperationContext>
    where TOperationContext : class, IOperationContext
{
    Task<IEndpointHandlerResult> HandleAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        CancellationToken cancellationToken);
}