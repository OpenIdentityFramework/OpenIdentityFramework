using System.Threading;
using System.Threading.Tasks;
using OpenIdentityFramework.Endpoints.Results;
using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Endpoints;

public interface IEndpointHandler<in THttpRequestContext>
    where THttpRequestContext : class, IHttpRequestContext
{
    Task<IEndpointHandlerResult> HandleAsync(THttpRequestContext requestContext, CancellationToken cancellationToken);
}