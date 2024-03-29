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
using OpenIdentityFramework.Services.Endpoints.Token.Models.Validation.Flows.AuthorizationCode;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.CommonParameters;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.Flows.AuthorizationCode;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.Flows.AuthorizationCode.Parameters;

namespace OpenIdentityFramework.Services.Endpoints.Token.Implementations.Validation.Flows.AuthorizationCode;

public class DefaultTokenRequestAuthorizationCodeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers, TGrantedConsent>
    : ITokenRequestAuthorizationCodeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRequestContext : class, IRequestContext
    where TClient : AbstractClient<TClientSecret>
    where TClientSecret : AbstractClientSecret, IEquatable<TClientSecret>
    where TScope : AbstractScope
    where TResource : AbstractResource<TResourceSecret>
    where TResourceSecret : AbstractResourceSecret, IEquatable<TResourceSecret>
    where TAuthorizationCode : AbstractAuthorizationCode<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TGrantedConsent : AbstractGrantedConsent
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
{
    protected static readonly TokenRequestAuthorizationCodeValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> UnauthorizedClient =
        new(new ProtocolError(TokenErrors.UnauthorizedClient, "The authenticated client is not authorized to use this authorization grant type"));

    protected static readonly TokenRequestAuthorizationCodeValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> InvalidGrant =
        new(new ProtocolError(TokenErrors.InvalidGrant,
            "The provided authorization grant (e.g., authorization code) is invalid, expired, revoked, does not match the redirect URI used in the authorization request, or was issued to another client"));

    protected static readonly TokenRequestAuthorizationCodeValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> DisabledUser =
        new(new ProtocolError(TokenErrors.InvalidGrant, "User account for provided authorization code has been disabled"));

    public DefaultTokenRequestAuthorizationCodeValidator(
        ITokenRequestAuthorizationCodeParameterCodeValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> codeValidator,
        ITokenRequestAuthorizationCodeParameterCodeVerifierValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> codeVerifierValidator,
        ITokenRequestCommonParameterScopeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret> scopeValidator,
        IResourceOwnerProfileService<TRequestContext, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> resourceOwnerProfile,
        IGrantedConsentService<TRequestContext, TClient, TClientSecret, TGrantedConsent> grantedConsents)
    {
        ArgumentNullException.ThrowIfNull(codeValidator);
        ArgumentNullException.ThrowIfNull(codeVerifierValidator);
        ArgumentNullException.ThrowIfNull(scopeValidator);
        ArgumentNullException.ThrowIfNull(resourceOwnerProfile);
        ArgumentNullException.ThrowIfNull(grantedConsents);
        CodeValidator = codeValidator;
        CodeVerifierValidator = codeVerifierValidator;
        ScopeValidator = scopeValidator;
        ResourceOwnerProfile = resourceOwnerProfile;
        GrantedConsents = grantedConsents;
    }

    protected ITokenRequestAuthorizationCodeParameterCodeValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> CodeValidator { get; }
    protected ITokenRequestAuthorizationCodeParameterCodeVerifierValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> CodeVerifierValidator { get; }
    protected ITokenRequestCommonParameterScopeValidator<TRequestContext, TClient, TClientSecret, TScope, TResource, TResourceSecret> ScopeValidator { get; }
    protected IResourceOwnerProfileService<TRequestContext, TScope, TResource, TResourceSecret, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> ResourceOwnerProfile { get; }
    protected IGrantedConsentService<TRequestContext, TClient, TClientSecret, TGrantedConsent> GrantedConsents { get; }

    public virtual async Task<TokenRequestAuthorizationCodeValidationResult<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>> ValidateAsync(
        TRequestContext requestContext,
        IFormCollection form,
        TClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        var clientAuthorizationFlows = client.GetGrantTypes();
        if (!clientAuthorizationFlows.Contains(DefaultGrantTypes.AuthorizationCode))
        {
            return UnauthorizedClient;
        }

        var codeValidation = await CodeValidator.ValidateCodeAsync(requestContext, form, client, cancellationToken);
        if (codeValidation.HasError)
        {
            return new(codeValidation.Error);
        }

        if (!string.Equals(codeValidation.AuthorizationCode.GetClientId(), client.GetClientId(), StringComparison.Ordinal))
        {
            return InvalidGrant;
        }

        var codeVerifierValidation = await CodeVerifierValidator.ValidateCodeVerifierAsync(requestContext, form, client, codeValidation.AuthorizationCode, cancellationToken);
        if (codeVerifierValidation.HasError)
        {
            return new(codeVerifierValidation.Error);
        }

        var codeScopes = codeValidation.AuthorizationCode.GetGrantedScopes();
        var grantedConsent = await GrantedConsents.FindAsync(
            requestContext,
            codeValidation.AuthorizationCode.GetEssentialResourceOwnerClaims().GetResourceOwnerIdentifiers().GetSubjectId(),
            client,
            cancellationToken);

        if (grantedConsent == null || !grantedConsent.GetGrantedScopes().IsSupersetOf(codeScopes))
        {
            return UnauthorizedClient;
        }

        var scopeValidation = await ScopeValidator.ValidateScopeAsync(requestContext, form, client, codeScopes, cancellationToken);
        if (scopeValidation.HasError)
        {
            return new(scopeValidation.Error);
        }

        var resourceOwnerProfileValidation = await ResourceOwnerProfile.GetResourceOwnerProfileAsync(
            requestContext,
            codeValidation.AuthorizationCode.GetEssentialResourceOwnerClaims(),
            scopeValidation.AllowedResources,
            cancellationToken);

        if (!resourceOwnerProfileValidation.IsActive)
        {
            return DisabledUser;
        }

        return new(new ValidAuthorizationCodeTokenRequest<TClient, TClientSecret, TScope, TResource, TResourceSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>(
            client,
            scopeValidation.AllowedResources,
            codeValidation.Handle,
            codeValidation.AuthorizationCode,
            resourceOwnerProfileValidation.Profile));
    }
}
