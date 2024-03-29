﻿using System.Diagnostics.CodeAnalysis;
using OpenIdentityFramework.Constants.Response.Errors;
using OpenIdentityFramework.Models;

namespace OpenIdentityFramework.Services.Endpoints.Authorize.Models.Validation.OpenIdConnect;

public class AuthorizeRequestOidcParameterRequestValidationResult
{
    public static readonly AuthorizeRequestOidcParameterRequestValidationResult Null = new();

    public static readonly AuthorizeRequestOidcParameterRequestValidationResult MultipleRequestValues = new(new(
        AuthorizeErrors.InvalidRequest,
        "Multiple \"request\" parameter values are present, but only 1 has allowed"));

    public static readonly AuthorizeRequestOidcParameterRequestValidationResult RequestNotSupported = new(new(
        AuthorizeErrors.RequestNotSupported,
        "\"request\" parameter provided but not supported"));

    public AuthorizeRequestOidcParameterRequestValidationResult()
    {
    }

    public AuthorizeRequestOidcParameterRequestValidationResult(ProtocolError error)
    {
        Error = error;
        HasError = true;
    }

    public ProtocolError? Error { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool HasError { get; }
}
