using System.Threading;
using System.Threading.Tasks;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Core;

public interface IClientService<in TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    Task<TClient?> FindEnabledAsync(
        TOperationContext operationContext,
        string clientId,
        CancellationToken cancellationToken);
}