using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Endpoints.Handlers;

public interface IAuthorizeEndpointHandler<in TOperationContext> : IEndpointHandler<TOperationContext>
    where TOperationContext : class, IOperationContext;