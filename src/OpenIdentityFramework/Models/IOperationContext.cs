using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenIdentityFramework.Models;

public interface IOperationContext : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}