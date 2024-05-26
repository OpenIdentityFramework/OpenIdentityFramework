using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace OpenIdentityFramework.Endpoints.Results;

public interface IEndpointHandlerResult
{
    Task ExecuteAsync(HttpContext httpContext, CancellationToken cancellationToken);
}