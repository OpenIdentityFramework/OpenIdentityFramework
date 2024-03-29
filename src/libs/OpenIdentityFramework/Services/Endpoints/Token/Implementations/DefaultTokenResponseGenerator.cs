﻿using System;
using System.Threading;
using System.Threading.Tasks;
using OpenIdentityFramework.Constants;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Authentication;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Models.Operation;
using OpenIdentityFramework.Services.Core;
using OpenIdentityFramework.Services.Core.Models.ResourceOwnerProfileService;
using OpenIdentityFramework.Services.Core.Models.ResourceService;
using OpenIdentityFramework.Services.Endpoints.Token.Models.TokenResponseGenerator;
using OpenIdentityFramework.Services.Endpoints.Token.Models.Validation.TokenRequestValidator;

namespace OpenIdentityFramework.Services.Endpoints.Token.Implementations;

public class DefaultTokenResponseGenerator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers, TAccessToken>
    : ITokenResponseGenerator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRequestContext : class, IRequestContext
    where TClient : AbstractClient<TClientSecret>
    where TClientSecret : AbstractClientSecret, IEquatable<TClientSecret>
    where TScope : AbstractScope
    where TResource : AbstractResource<TResourceSecret>
    where TResourceSecret : AbstractResourceSecret, IEquatable<TResourceSecret>
    where TAuthorizationCode : AbstractAuthorizationCode<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRefreshToken : AbstractRefreshToken<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
    where TAccessToken : AbstractAccessToken<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
{
    public DefaultTokenResponseGenerator(
        TimeProvider systemClock,
        IAccessTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAccessToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> accessTokenService,
        IIdTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> idTokenService,
        IRefreshTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> refreshTokenService)
    {
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(accessTokenService);
        ArgumentNullException.ThrowIfNull(idTokenService);
        ArgumentNullException.ThrowIfNull(refreshTokenService);
        TimeProvider = systemClock;
        AccessTokenService = accessTokenService;
        IdTokenService = idTokenService;
        RefreshTokenService = refreshTokenService;
    }

    protected TimeProvider TimeProvider { get; }
    protected IAccessTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAccessToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> AccessTokenService { get; }
    protected IIdTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> IdTokenService { get; }
    protected IRefreshTokenService<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> RefreshTokenService { get; }

    public virtual async Task<TokenResponseGenerationResult> CreateResponseAsync(
        TRequestContext requestContext,
        ValidTokenRequest<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.GrantType == DefaultGrantTypes.AuthorizationCode)
        {
            if (request.AuthorizationCode is null || request.ResourceOwnerProfile is null)
            {
                return new("Invalid request state");
            }

            return await CreateAuthorizationCodeResponseAsync(
                requestContext,
                request.Client,
                request.AllowedResources,
                request.AuthorizationCode,
                request.ResourceOwnerProfile,
                request.Issuer,
                cancellationToken);
        }

        if (request.GrantType == DefaultGrantTypes.ClientCredentials)
        {
            return await CreateClientCredentialsResponseAsync(
                requestContext,
                request.Client,
                request.AllowedResources,
                request.Issuer,
                cancellationToken);
        }

        if (request.GrantType == DefaultGrantTypes.RefreshToken)
        {
            if (request.RefreshToken is null)
            {
                return new("Invalid request state");
            }

            return await CreateRefreshTokenResponseAsync(
                requestContext,
                request.Client,
                request.AllowedResources,
                request.RefreshToken,
                request.ResourceOwnerProfile,
                request.Issuer,
                cancellationToken);
        }

        return new("Unsupported grant type");
    }

    protected virtual async Task<TokenResponseGenerationResult> CreateAuthorizationCodeResponseAsync(
        TRequestContext requestContext,
        TClient client,
        ValidResources<TScope, TResource, TResourceSecret> grantedResources,
        ValidAuthorizationCode<TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> authorizationCode,
        ResourceOwnerProfile<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> resourceOwnerProfile,
        string issuer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(authorizationCode);
        ArgumentNullException.ThrowIfNull(grantedResources);
        ArgumentNullException.ThrowIfNull(resourceOwnerProfile);
        cancellationToken.ThrowIfCancellationRequested();
        var accessTokenResult = await AccessTokenService.CreateAccessTokenAsync(
            requestContext,
            client,
            issuer,
            DefaultGrantTypes.AuthorizationCode,
            resourceOwnerProfile,
            grantedResources,
            TimeProvider.GetUtcNow(),
            cancellationToken);
        if (accessTokenResult.HasError)
        {
            return new(accessTokenResult.ErrorDescription);
        }

        string? idTokenHandle = null;
        if (grantedResources.HasOpenId)
        {
            var idTokenResult = await IdTokenService.CreateIdTokenAsync(
                requestContext,
                client,
                issuer,
                null,
                accessTokenResult.AccessToken.Handle,
                null,
                resourceOwnerProfile,
                client.ShouldIncludeUserClaimsInIdTokenTokenResponse(),
                grantedResources,
                accessTokenResult.AccessToken.ActualIssuedAt,
                cancellationToken);
            if (idTokenResult.HasError)
            {
                return new(idTokenResult.ErrorDescription);
            }

            idTokenHandle = idTokenResult.IdToken.Handle;
        }

        string? refreshTokenHandle = null;
        if (grantedResources.HasOfflineAccess)
        {
            var refreshTokenResult = await RefreshTokenService.CreateAsync(
                requestContext,
                issuer,
                null,
                accessTokenResult.AccessToken,
                cancellationToken);
            if (refreshTokenResult.HasError)
            {
                return new(refreshTokenResult.ErrorDescription);
            }

            refreshTokenHandle = refreshTokenResult.RefreshToken.Handle;
        }

        var resultScope = grantedResources.HasAnyScope() ? string.Join(' ', grantedResources.RawScopes) : null;
        var successfulResponse = new SuccessfulTokenResponse(
            accessTokenResult.AccessToken.Handle,
            DefaultAccessTokenType.Bearer,
            refreshTokenHandle,
            accessTokenResult.AccessToken.LifetimeInSeconds,
            idTokenHandle,
            resultScope,
            issuer);
        return new(successfulResponse);
    }

    protected virtual async Task<TokenResponseGenerationResult> CreateClientCredentialsResponseAsync(
        TRequestContext requestContext,
        TClient client,
        ValidResources<TScope, TResource, TResourceSecret> grantedResources,
        string issuer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grantedResources);
        cancellationToken.ThrowIfCancellationRequested();
        var accessTokenResult = await AccessTokenService.CreateAccessTokenAsync(
            requestContext,
            client,
            issuer,
            DefaultGrantTypes.ClientCredentials,
            null,
            grantedResources,
            TimeProvider.GetUtcNow(),
            cancellationToken);
        if (accessTokenResult.HasError)
        {
            return new(accessTokenResult.ErrorDescription);
        }

        var resultScope = grantedResources.HasAnyScope() ? string.Join(' ', grantedResources.RawScopes) : null;
        var successfulResponse = new SuccessfulTokenResponse(
            accessTokenResult.AccessToken.Handle,
            DefaultAccessTokenType.Bearer,
            null,
            accessTokenResult.AccessToken.LifetimeInSeconds,
            null,
            resultScope,
            issuer);
        return new(successfulResponse);
    }

    protected virtual async Task<TokenResponseGenerationResult> CreateRefreshTokenResponseAsync(
        TRequestContext requestContext,
        TClient client,
        ValidResources<TScope, TResource, TResourceSecret> grantedResources,
        ValidRefreshToken<TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> refreshToken,
        ResourceOwnerProfile<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>? resourceOwnerProfile,
        string issuer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(grantedResources);
        cancellationToken.ThrowIfCancellationRequested();
        var accessTokenResult = await AccessTokenService.CreateAccessTokenAsync(
            requestContext,
            client,
            issuer,
            DefaultGrantTypes.RefreshToken,
            resourceOwnerProfile,
            grantedResources,
            TimeProvider.GetUtcNow(),
            cancellationToken);
        if (accessTokenResult.HasError)
        {
            return new(accessTokenResult.ErrorDescription);
        }

        string? idTokenHandle = null;
        if (grantedResources.HasOpenId)
        {
            if (resourceOwnerProfile is null)
            {
                return new("Invalid request state");
            }

            var idTokenResult = await IdTokenService.CreateIdTokenAsync(
                requestContext,
                client,
                issuer,
                null,
                accessTokenResult.AccessToken.Handle,
                null,
                resourceOwnerProfile,
                client.ShouldIncludeUserClaimsInIdTokenTokenResponse(),
                grantedResources,
                accessTokenResult.AccessToken.ActualIssuedAt,
                cancellationToken);
            if (idTokenResult.HasError)
            {
                return new(idTokenResult.ErrorDescription);
            }

            idTokenHandle = idTokenResult.IdToken.Handle;
        }

        string? refreshTokenHandle = null;
        if (grantedResources.HasOfflineAccess)
        {
            var refreshTokenResult = await RefreshTokenService.CreateAsync(
                requestContext,
                issuer,
                refreshToken,
                accessTokenResult.AccessToken,
                cancellationToken);
            if (refreshTokenResult.HasError)
            {
                return new(refreshTokenResult.ErrorDescription);
            }

            refreshTokenHandle = refreshTokenResult.RefreshToken.Handle;
        }

        var resultScope = grantedResources.HasAnyScope() ? string.Join(' ', grantedResources.RawScopes) : null;
        var successfulResponse = new SuccessfulTokenResponse(
            accessTokenResult.AccessToken.Handle,
            DefaultAccessTokenType.Bearer,
            refreshTokenHandle,
            accessTokenResult.AccessToken.LifetimeInSeconds,
            idTokenHandle,
            resultScope,
            issuer);
        return new(successfulResponse);
    }
}
