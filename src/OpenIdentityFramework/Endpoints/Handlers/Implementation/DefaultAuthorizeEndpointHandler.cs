using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Endpoints.Results;
using OpenIdentityFramework.Endpoints.Results.Implementation;
using OpenIdentityFramework.Extensions;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize;

namespace OpenIdentityFramework.Endpoints.Handlers.Implementation;

public class DefaultAuthorizeEndpointHandler<TOperationContext, TClient>
    : IAuthorizeEndpointHandler<TOperationContext>
    where TOperationContext : class, IOperationContext
    where TClient : AbstractClient
{
    public DefaultAuthorizeEndpointHandler(
        IOptionsMonitor<OpenIdentityFrameworkOptions> options,
        IAuthorizeRequestValidator<TOperationContext, TClient> requestValidator,
        ILogger<DefaultAuthorizeEndpointHandler<TOperationContext, TClient>> logger)
    {
        Options = options;
        RequestValidator = requestValidator;
        Logger = logger;
    }

    protected IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }
    protected IAuthorizeRequestValidator<TOperationContext, TClient> RequestValidator { get; }
    protected ILogger<DefaultAuthorizeEndpointHandler<TOperationContext, TClient>> Logger { get; }

    public virtual async Task<IEndpointHandlerResult> HandleAsync(
        HttpContext httpContext,
        TOperationContext operationContext,
        CancellationToken cancellationToken)
    {
        Logger.HandleAsyncStarted();
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(operationContext);
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // The authorization server MUST support the use of the HTTP GET method section 9.3.1 of [RFC9110] for the authorization endpoint
        // and MAY support the POST method (section 9.3.3 of [RFC9110]) as well.
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // Authorization Servers MUST support the use of the HTTP GET and POST methods defined in RFC 7231 [RFC7231] at the Authorization Endpoint.
        // Clients MAY use the HTTP GET or POST methods to send the Authorization Request to the Authorization Server.
        // If using the HTTP GET method, the request parameters are serialized using URI Query String Serialization, per section 13.1.
        // If using the HTTP POST method, the request parameters are serialized using Form Serialization, per section 13.2.
        IReadOnlyDictionary<string, StringValues> parameters;
        if (HttpMethods.IsGet(httpContext.Request.Method))
        {
            parameters = httpContext.Request.Query.AsReadOnlyDictionary();
        }
        else if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            if (!httpContext.Request.HasApplicationFormContentType())
            {
                return new DefaultStatusCodeResult(HttpStatusCode.UnsupportedMediaType);
            }

            var form = await httpContext.Request.ReadFormAsync(cancellationToken);
            parameters = form.AsReadOnlyDictionary();
        }
        else
        {
            return new DefaultStatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        var validationResult = await RequestValidator.ValidateAsync(httpContext, operationContext, parameters, cancellationToken);
        throw new NotImplementedException();
    }
}

public static partial class DefaultAuthorizeEndpointHandlerLoggingExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "HandleAsync started")]
    public static partial void HandleAsyncStarted(this ILogger logger);
}