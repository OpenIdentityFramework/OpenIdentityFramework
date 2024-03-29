﻿using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIdentityFramework.Configuration.Options;
using OpenIdentityFramework.Constants.Response;
using OpenIdentityFramework.Extensions;
using OpenIdentityFramework.Services.Endpoints.Token.Models.TokenResponseGenerator;

namespace OpenIdentityFramework.Endpoints.Results.Implementations;

public class DefaultTokenSuccessfulResult : IEndpointHandlerResult
{
    public DefaultTokenSuccessfulResult(OpenIdentityFrameworkOptions frameworkOptions, SuccessfulTokenResponse successfulTokenResponse)
    {
        ArgumentNullException.ThrowIfNull(frameworkOptions);
        ArgumentNullException.ThrowIfNull(successfulTokenResponse);
        FrameworkOptions = frameworkOptions;
        SuccessfulTokenResponse = successfulTokenResponse;
    }

    protected OpenIdentityFrameworkOptions FrameworkOptions { get; }
    protected SuccessfulTokenResponse SuccessfulTokenResponse { get; }

    public virtual async Task ExecuteAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        cancellationToken.ThrowIfCancellationRequested();
        var response = new ResponseDto(
            SuccessfulTokenResponse.AccessToken,
            SuccessfulTokenResponse.IssuedTokenType,
            SuccessfulTokenResponse.RefreshToken,
            SuccessfulTokenResponse.ExpiresIn,
            SuccessfulTokenResponse.IdToken,
            SuccessfulTokenResponse.Scope,
            SuccessfulTokenResponse.Issuer);
        httpContext.Response.StatusCode = 200;
        httpContext.Response.SetNoCache();
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }

    protected class ResponseDto
    {
        public ResponseDto(string accessToken, string tokenType, string? refreshToken, long expiresIn, string? idToken, string? scope, string issuer)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
            RefreshToken = refreshToken;
            ExpiresIn = expiresIn;
            IdToken = idToken;
            Scope = scope;
            Issuer = issuer;
        }

        [JsonPropertyName(TokenResponseParameters.AccessToken)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string AccessToken { get; }

        [JsonPropertyName(TokenResponseParameters.TokenType)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TokenType { get; }

        [JsonPropertyName(TokenResponseParameters.RefreshToken)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; }

        [JsonPropertyName(TokenResponseParameters.ExpiresIn)]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public long ExpiresIn { get; }

        [JsonPropertyName(TokenResponseParameters.IdToken)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IdToken { get; }

        [JsonPropertyName(TokenResponseParameters.Scope)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Scope { get; }

        [JsonPropertyName(TokenResponseParameters.Issuer)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Issuer { get; }
    }
}
