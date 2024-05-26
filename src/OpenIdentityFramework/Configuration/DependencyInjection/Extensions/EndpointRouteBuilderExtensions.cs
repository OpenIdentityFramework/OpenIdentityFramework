using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Endpoints;
using OpenIdentityFramework.Endpoints.Handlers;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Services.Runtime;

namespace OpenIdentityFramework.Configuration.DependencyInjection.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOpenIdentityFrameworkEndpoints<THttpRequestContext>(this IEndpointRouteBuilder endpoints)
        where THttpRequestContext : class, IHttpRequestContext
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var optionsMonitor = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenIdentityFrameworkOptions>>();
        var options = optionsMonitor.CurrentValue;
        endpoints.AddEndpoint<IAuthorizeEndpointHandler<THttpRequestContext>, THttpRequestContext>(
            options.Endpoints.Authorize.Path,
            new[]
            {
                HttpMethods.Get,
                HttpMethods.Post
            });
        return endpoints;
    }

    public static IEndpointConventionBuilder AddEndpoint<TEndpointHandler, THttpRequestContext>(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        IEnumerable<string> httpMethods)
        where TEndpointHandler : IEndpointHandler<THttpRequestContext>
        where THttpRequestContext : class, IHttpRequestContext
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        static async Task RequestDelegate(HttpContext httpContext)
        {
            var contextFactory = httpContext.RequestServices.GetRequiredService<IHttpRequestContextFactory<THttpRequestContext>>();
            var endpointHandler = httpContext.RequestServices.GetRequiredService<TEndpointHandler>();
            await ExecuteHandlerInContextAsync(httpContext, contextFactory, endpointHandler, httpContext.RequestAborted);
        }

        return endpoints.MapMethods(pattern, httpMethods, RequestDelegate);
    }

    private static async Task ExecuteHandlerInContextAsync<TEndpointHandler, THttpRequestContext>(
        HttpContext httpContext,
        IHttpRequestContextFactory<THttpRequestContext> contextFactory,
        TEndpointHandler endpointHandler,
        CancellationToken cancellationToken)
        where TEndpointHandler : IEndpointHandler<THttpRequestContext>
        where THttpRequestContext : class, IHttpRequestContext
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestContext = await contextFactory.CreateAsync(httpContext, cancellationToken);
        var endpointResult = await endpointHandler.HandleAsync(requestContext, cancellationToken);
        await requestContext.CommitAsync(cancellationToken);
        await endpointResult.ExecuteAsync(httpContext, cancellationToken);
    }
}