using System;
using System.Threading;
using System.Threading.Tasks;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Core.Implementation;

public class DefaultClientService<TOperationContext, TClient>
    : IClientService<TOperationContext, TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public Task<TClient?> FindEnabledAsync(TOperationContext operationContext, string clientId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}