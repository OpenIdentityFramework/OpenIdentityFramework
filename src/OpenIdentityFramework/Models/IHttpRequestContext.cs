using Microsoft.AspNetCore.Http;

namespace OpenIdentityFramework.Models;

public interface IHttpRequestContext : IOperationContext
{
    HttpContext HttpContext { get; }
}