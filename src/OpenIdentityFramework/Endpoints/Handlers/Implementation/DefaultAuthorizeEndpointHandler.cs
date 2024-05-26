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

namespace OpenIdentityFramework.Endpoints.Handlers.Implementation;

public class DefaultAuthorizeEndpointHandler<THttpRequestContext>
    : IAuthorizeEndpointHandler<THttpRequestContext>
    where THttpRequestContext : class, IHttpRequestContext
{
    public DefaultAuthorizeEndpointHandler(
        IOptionsMonitor<OpenIdentityFrameworkOptions> options,
        ILogger<DefaultAuthorizeEndpointHandler<THttpRequestContext>> logger)
    {
        Options = options;
        Logger = logger;
    }

    protected IOptionsMonitor<OpenIdentityFrameworkOptions> Options { get; }
    protected ILogger<DefaultAuthorizeEndpointHandler<THttpRequestContext>> Logger { get; }

    public async Task<IEndpointHandlerResult> HandleAsync(THttpRequestContext requestContext, CancellationToken cancellationToken)
    {
        Logger.HandleAsyncStarted();
        ArgumentNullException.ThrowIfNull(requestContext);
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-3.1
        // The authorization server MUST support the use of the HTTP GET method Section 9.3.1 of [RFC9110] for the authorization endpoint
        // and MAY support the POST method (Section 9.3.3 of [RFC9110]) as well.
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.2.1
        // Authorization Servers MUST support the use of the HTTP GET and POST methods defined in RFC 7231 [RFC7231] at the Authorization Endpoint.
        // Clients MAY use the HTTP GET or POST methods to send the Authorization Request to the Authorization Server.
        // If using the HTTP GET method, the request parameters are serialized using URI Query String Serialization, per Section 13.1.
        // If using the HTTP POST method, the request parameters are serialized using Form Serialization, per Section 13.2.
        IReadOnlyDictionary<string, StringValues> parameters;
        if (HttpMethods.IsGet(requestContext.HttpContext.Request.Method))
        {
            parameters = requestContext.HttpContext.Request.Query.AsReadOnlyDictionary();
        }
        else if (HttpMethods.IsPost(requestContext.HttpContext.Request.Method))
        {
            if (!requestContext.HttpContext.Request.HasApplicationFormContentType())
            {
                return new DefaultStatusCodeResult(HttpStatusCode.UnsupportedMediaType);
            }

            var form = await requestContext.HttpContext.Request.ReadFormAsync(cancellationToken);
            parameters = form.AsReadOnlyDictionary();
        }

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