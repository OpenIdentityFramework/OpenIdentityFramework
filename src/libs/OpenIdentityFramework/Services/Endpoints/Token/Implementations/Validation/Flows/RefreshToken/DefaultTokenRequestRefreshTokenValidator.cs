﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Constants;
using OpenIdentityFramework.Constants.Response.Errors;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Authentication;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Models.Operation;
using OpenIdentityFramework.Services.Core;
using OpenIdentityFramework.Services.Endpoints.Token.Models.Validation.Flows.RefreshToken;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.CommonParameters;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.Flows.RefreshToken;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.Flows.RefreshToken.Parameters;

namespace OpenIdentityFramework.Services.Endpoints.Token.Implementations.Validation.Flows.RefreshToken;

public class DefaultTokenRequestRefreshTokenValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers, TGrantedConsent>
    : ITokenRequestRefreshTokenValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRequestContext : class, IRequestContext
    where TClient : AbstractClient<TClientSecret>
    where TClientSecret : AbstractClientSecret, IEquatable<TClientSecret>
    where TScope : AbstractScope
    where TResource : AbstractResource<TResourceSecret>
    where TResourceSecret : AbstractResourceSecret, IEquatable<TResourceSecret>
    where TRefreshToken : AbstractRefreshToken<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
    where TGrantedConsent : AbstractGrantedConsent
{
    protected static readonly TokenRequestRefreshTokenValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> UnauthorizedClient =
        new(new ProtocolError(TokenErrors.UnauthorizedClient, "The authenticated client is not authorized to use this authorization grant type"));

    protected static readonly TokenRequestRefreshTokenValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> DisabledUser =
        new(new ProtocolError(TokenErrors.InvalidGrant, "User account for provided refresh token has been disabled"));

    public DefaultTokenRequestRefreshTokenValidator(
        ITokenRequestRefreshTokenParameterRefreshTokenValidator<TRequestContext, TClient, TClientSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> refreshTokenValidator,
        ITokenRequestCommonParameterScopeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret> scopeValidator,
        IResourceOwnerProfileService<TRequestContext, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> resourceOwnerProfile,
        IGrantedConsentService<TRequestContext, TClient, TClientSecret, TGrantedConsent> grantedConsents)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenValidator);
        ArgumentNullException.ThrowIfNull(scopeValidator);
        ArgumentNullException.ThrowIfNull(resourceOwnerProfile);
        ArgumentNullException.ThrowIfNull(grantedConsents);
        RefreshTokenValidator = refreshTokenValidator;
        ScopeValidator = scopeValidator;
        ResourceOwnerProfile = resourceOwnerProfile;
        GrantedConsents = grantedConsents;
    }

    protected ITokenRequestRefreshTokenParameterRefreshTokenValidator<TRequestContext, TClient, TClientSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> RefreshTokenValidator { get; }
    protected ITokenRequestCommonParameterScopeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret> ScopeValidator { get; }
    protected IResourceOwnerProfileService<TRequestContext, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> ResourceOwnerProfile { get; }
    protected IGrantedConsentService<TRequestContext, TClient, TClientSecret, TGrantedConsent> GrantedConsents { get; }

    public virtual async Task<TokenRequestRefreshTokenValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>> ValidateAsync(
        TRequestContext requestContext,
        IFormCollection form,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (!client.GetGrantTypes().Contains(DefaultGrantTypes.RefreshToken))
        {
            return UnauthorizedClient;
        }

        var refreshTokenValidation = await RefreshTokenValidator.ValidateRefreshTokenAsync(requestContext, form, client, cancellationToken);
        if (refreshTokenValidation.HasError)
        {
            return new(refreshTokenValidation.Error);
        }

        var refreshTokenScopes = refreshTokenValidation.RefreshToken.GetGrantedScopes();
        var grantedConsent = await GrantedConsents.FindAsync(
            requestContext,
            refreshTokenValidation.RefreshToken.GetEssentialResourceOwnerClaims().GetResourceOwnerIdentifiers().GetSubjectId(),
            client,
            cancellationToken);

        if (grantedConsent == null || !grantedConsent.GetGrantedScopes().IsSupersetOf(refreshTokenScopes))
        {
            return UnauthorizedClient;
        }

        var scopeValidation = await ScopeValidator.ValidateScopeAsync(requestContext, form, client, refreshTokenScopes, cancellationToken);
        if (scopeValidation.HasError)
        {
            return new(scopeValidation.Error);
        }

        var resourceOwnerProfileValidation = await ResourceOwnerProfile.GetResourceOwnerProfileAsync(
            requestContext,
            refreshTokenValidation.RefreshToken.GetEssentialResourceOwnerClaims(),
            scopeValidation.AllowedResources,
            cancellationToken);

        if (!resourceOwnerProfileValidation.IsActive)
        {
            return DisabledUser;
        }

        return new(new ValidRefreshTokenTokenRequest<TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>(
            client,
            scopeValidation.AllowedResources,
            refreshTokenValidation.Handle,
            refreshTokenValidation.RefreshToken,
            resourceOwnerProfileValidation.Profile));
    }
}
