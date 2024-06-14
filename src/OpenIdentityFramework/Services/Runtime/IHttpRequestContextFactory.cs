using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Services.Runtime;

public interface IOperationContextFactory<TOperationContext>
    where TOperationContext : IOperationContext
{
    Task<TOperationContext> CreateAsync(HttpContext httpContext, CancellationToken cancellationToken);
}