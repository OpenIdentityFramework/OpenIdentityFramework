using System;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Services.Endpoints.Authorize.Models.Validation;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Models.AuthorizeRequestValidator;

public class AuthorizeRequestValidationError<TClient>
    where TClient : AbstractClient
{
    public AuthorizeRequestValidationError(ProtocolError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public AuthorizeRequestValidationError(
        AuthorizeRequestRequiredParameters<TClient> requiredParameters,
        ProtocolError error)
    {
        ArgumentNullException.ThrowIfNull(requiredParameters);
        ArgumentNullException.ThrowIfNull(error);
        RequiredParameters = requiredParameters;
        Error = error;
    }

    public AuthorizeRequestRequiredParameters<TClient>? RequiredParameters { get; }

    public ProtocolError Error { get; }
}