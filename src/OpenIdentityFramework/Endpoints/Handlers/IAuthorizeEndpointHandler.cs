using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Endpoints.Handlers;

public interface IAuthorizeEndpointHandler<in THttpRequestContext> : IEndpointHandler<THttpRequestContext>
    where THttpRequestContext : class, IHttpRequestContext;