using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Services.Runtime;

public interface IHttpRequestContextFactory<THttpRequestContext>
    where THttpRequestContext : IHttpRequestContext
{
    Task<THttpRequestContext> CreateAsync(HttpContext httpContext, CancellationToken cancellationToken);
}