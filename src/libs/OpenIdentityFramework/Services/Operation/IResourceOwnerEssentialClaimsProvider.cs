﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Authentication;
using OpenIdentityFramework.Services.Operation.Models.ResourceOwnerEssentialClaimsProvider;

namespace OpenIdentityFramework.Services.Operation;

public interface IResourceOwnerEssentialClaimsProvider<TRequestContext, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRequestContext : class, IRequestContext
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
{
    Task<ResourceOwnerEssentialClaimsResult<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>> GetAsync(
        TRequestContext requestContext,
        AuthenticationTicket authenticationTicket,
        CancellationToken cancellationToken);
}
