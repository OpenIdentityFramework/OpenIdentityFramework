﻿using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Constants;
using OpenIdentityFramework.Constants.Request;
using OpenIdentityFramework.Models;
using OpenIdentityFramework.Models.Authentication;
using OpenIdentityFramework.Models.Configuration;
using OpenIdentityFramework.Models.Operation;
using OpenIdentityFramework.Services.Endpoints.Token.Models.Validation.Flows.AuthorizationCode.Parameters;
using OpenIdentityFramework.Services.Endpoints.Token.Validation.Flows.AuthorizationCode.Parameters;
using OpenIdentityFramework.Services.Static.Cryptography;
using OpenIdentityFramework.Services.Static.SyntaxValidation;
using OpenIdentityFramework.Services.Static.WebUtilities;

namespace OpenIdentityFramework.Services.Endpoints.Token.Implementations.Validation.Flows.AuthorizationCode.Parameters;

public class DefaultTokenRequestAuthorizationCodeParameterCodeVerifierValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    : ITokenRequestAuthorizationCodeParameterCodeVerifierValidator<TRequestContext, TClient, TClientSecret, TAuthorizationCode, TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TRequestContext : class, IRequestContext
    where TClient : AbstractClient<TClientSecret>
    where TClientSecret : AbstractClientSecret, IEquatable<TClientSecret>
    where TAuthorizationCode : AbstractAuthorizationCode<TResourceOwnerEssentialClaims, TResourceOwnerIdentifiers>
    where TResourceOwnerEssentialClaims : AbstractResourceOwnerEssentialClaims<TResourceOwnerIdentifiers>
    where TResourceOwnerIdentifiers : AbstractResourceOwnerIdentifiers
{
    public DefaultTokenRequestAuthorizationCodeParameterCodeVerifierValidator(OpenIdentityFrameworkOptions frameworkOptions)
    {
        ArgumentNullException.ThrowIfNull(frameworkOptions);
        FrameworkOptions = frameworkOptions;
    }

    protected OpenIdentityFrameworkOptions FrameworkOptions { get; }

    public virtual Task<TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult> ValidateCodeVerifierAsync(
        TRequestContext requestContext,
        IFormCollection form,
        TClient client,
        TAuthorizationCode authorizationCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(authorizationCode);
        cancellationToken.ThrowIfCancellationRequested();
        // https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.1.3.1
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-09.html#section-4.1.3
        // code_verifier - REQUIRED, if the code_challenge parameter was included in the authorization request. MUST NOT be used otherwise. The original code verifier string.
        if (!form.TryGetValue(TokenRequestParameters.CodeVerifier, out var codeVerifierValues))
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.CodeVerifierIsMissing);
        }

        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-09.html#section-3.2
        // Request and response parameters defined by this specification MUST NOT be included more than once.
        if (codeVerifierValues.Count != 1)
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.MultipleCodeVerifierValuesNotAllowed);
        }

        var codeVerifier = codeVerifierValues.ToString();
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-09.html#section-3.2
        // Parameters sent without a value MUST be treated as if they were omitted from the request.
        if (string.IsNullOrEmpty(codeVerifier))
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.CodeVerifierIsMissing);
        }

        if (codeVerifier.Length < FrameworkOptions.InputLengthRestrictions.CodeVerifierMinLength)
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.CodeVerifierIsTooShort);
        }

        if (codeVerifier.Length > FrameworkOptions.InputLengthRestrictions.CodeVerifierMaxLength)
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.CodeVerifierIsTooLong);
        }

        if (!CodeVerifierSyntaxValidator.IsValid(codeVerifier))
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.InvalidCodeVerifierSyntax);
        }

        var codeChallenge = authorizationCode.GetCodeChallenge();
        var codeChallengeMethod = authorizationCode.GetCodeChallengeMethod();
        if (!IsValid(codeChallenge, codeChallengeMethod, codeVerifier))
        {
            return Task.FromResult(TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult.InvalidCodeVerifier);
        }

        return Task.FromResult(new TokenRequestAuthorizationCodeParameterCodeVerifierValidationResult(codeVerifier));
    }

    protected bool IsValid(string codeChallenge, string codeChallengeMethod, string codeVerifier)
    {
        if (codeChallengeMethod == DefaultCodeChallengeMethod.Plain)
        {
            return IsPlainValid(codeChallenge, codeVerifier);
        }

        if (codeChallengeMethod == DefaultCodeChallengeMethod.S256)
        {
            return IsS256Valid(codeChallenge, codeVerifier);
        }

        return false;
    }

    private static bool IsPlainValid(string? codeChallenge, string? codeVerifier)
    {
        return !string.IsNullOrEmpty(codeChallenge)
               && !string.IsNullOrEmpty(codeVerifier)
               && string.Equals(codeChallenge, codeVerifier, StringComparison.Ordinal);
    }

    private static bool IsS256Valid(string? codeChallenge, string? codeVerifier)
    {
        const int maxStackallocBytesCount = 1024;
        if (string.IsNullOrEmpty(codeChallenge) || string.IsNullOrEmpty(codeVerifier))
        {
            return false;
        }


        if (string.IsNullOrEmpty(codeChallenge))
        {
            return false;
        }

        var base64DecodedCodeChallengeBufferSize = Base64UrlDecoder.ComputeRequiredBufferSize(codeChallenge.Length);
        byte[]? base64DecodedCodeChallengeBufferFromPool = null;
        var base64DecodedCodeChallengeBuffer = base64DecodedCodeChallengeBufferSize <= maxStackallocBytesCount
            ? stackalloc byte[maxStackallocBytesCount]
            : base64DecodedCodeChallengeBufferFromPool = ArrayPool<byte>.Shared.Rent(base64DecodedCodeChallengeBufferSize);
        Span<byte> codeVerifierHash = stackalloc byte[Sha256Hasher.Sha256BytesCount];
        try
        {
            if (Base64UrlDecoder.TryDecode(codeChallenge, base64DecodedCodeChallengeBuffer, out var base64BytesCount)
                && base64BytesCount == Sha256Hasher.Sha256BytesCount)
            {
                var codeChallengeBytes = base64DecodedCodeChallengeBuffer.Slice(0, base64BytesCount);
                Sha256Hasher.ComputeSha256(codeVerifier, codeVerifierHash);
                return CryptographicOperations.FixedTimeEquals(codeVerifierHash, codeChallengeBytes);
            }
            else
            {
                return false;
            }
        }
        finally
        {
            codeVerifierHash.Clear();
            if (base64DecodedCodeChallengeBufferFromPool is not null)
            {
                ArrayPool<byte>.Shared.Return(base64DecodedCodeChallengeBufferFromPool, true);
            }
            else
            {
                base64DecodedCodeChallengeBuffer.Clear();
            }
        }
    }
}
