﻿namespace OpenIdentityFramework.Constants.Request;

public static class AuthorizeRequestParameters
{
    public const string ClientId = ClientAuthenticationParameters.ClientId;
    public const string CodeChallenge = "code_challenge";
    public const string CodeChallengeMethod = "code_challenge_method";
    public const string RedirectUri = "redirect_uri";
    public const string ResponseType = "response_type";
    public const string Scope = "scope";
    public const string State = "state";
    public const string ResponseMode = "response_mode";

    public const string Nonce = "nonce";
    public const string Display = "display";
    public const string Prompt = "prompt";
    public const string MaxAge = "max_age";
    public const string UiLocales = "ui_locales";
    public const string LoginHint = "login_hint";
    public const string AcrValues = "acr_values";
    public const string Request = "request";
    public const string RequestUri = "request_uri";
    public const string Registration = "registration";
}
