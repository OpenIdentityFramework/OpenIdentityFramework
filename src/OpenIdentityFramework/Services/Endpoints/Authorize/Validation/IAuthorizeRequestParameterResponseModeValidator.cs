using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Validation;

public interface IAuthorizeRequestParameterResponseModeValidator<in TOperationContext, in TClient>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    Task<Result<string, ProtocolError>> ValidateResponseModeAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        IReadOnlyDictionary<string, StringValues> requestParameters,
        TClient client,
        IReadOnlySet<string> responseType,
        CancellationToken cancellationToken);
}