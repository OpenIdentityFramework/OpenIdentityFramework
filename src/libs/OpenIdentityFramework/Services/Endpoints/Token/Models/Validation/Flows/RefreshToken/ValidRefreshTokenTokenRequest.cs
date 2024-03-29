﻿using System;
using OpenIdentityFramework.Models.Authentication;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Models.Operation;
using OpenIdentityFramework.Services.Core.Models.ResourceOwnerProfileService;
using OpenIdentityFramework.Services.Core.Models.ResourceService;

namespace OpenIdentityFramework.Services.Endpoints.Token.Models.Validation.Flows.RefreshToken;

public class ValidRefreshTokenTokenRequest<TClient, TClientSecret, TScope, TResource, TResourceSecret, TRefreshToken, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TClient : AbstractClient<TClientSecret>
    where TClientSecret : AbstractClientSecret, IEquatable<TClientSecret>
    where TScope : AbstractScope
    where TResource : AbstractResource<TResourceSecret>
    where TResourceSecret : AbstractResourceSecret, IEquatable<TResourceSecret>
    where TRefreshToken : AbstractRefreshToken<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
{
    public ValidRefreshTokenTokenRequest(
        TClient client,
        ValidResources<TScope, TResource, TResourceSecret> allowedResources,
        string refreshTokenHandle,
        TRefreshToken refreshToken,
        ResourceOwnerProfile<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> resourceOwnerProfile)
    {
        Client = client;
        AllowedResources = allowedResources;
        RefreshTokenHandle = refreshTokenHandle;
        RefreshToken = refreshToken;
        ResourceOwnerProfile = resourceOwnerProfile;
    }

    public TClient Client { get; }
    public ValidResources<TScope, TResource, TResourceSecret> AllowedResources { get; }
    public string RefreshTokenHandle { get; }
    public TRefreshToken RefreshToken { get; }
    public ResourceOwnerProfile<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers> ResourceOwnerProfile { get; }
}
