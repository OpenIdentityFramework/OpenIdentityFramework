namespace OpenIdentityFramework.Constants;

public static class DefaultResponseMode
{
    /// <summary>
    ///     In this mode, Authorization Response parameters are encoded in the query string added to the redirect_uri when redirecting back to the Client.
    /// </summary>
    public const string Query = "query";

    /// <summary>
    ///     In this mode, Authorization Response parameters are encoded in the fragment added to the redirect_uri when redirecting back to the Client.
    /// </summary>
    public const string Fragment = "fragment";

    /// <summary>
    ///     In this mode, Authorization Response parameters are encoded as HTML form values that are auto-submitted in the User Agent,
    ///     and thus are transmitted via the HTTP POST method to the Client, with the result parameters being encoded in the body using the application/x-www-form-urlencoded format.
    ///     The action attribute of the form MUST be the Client's Redirection URI.
    ///     The method of the form attribute MUST be POST. Because the Authorization Response is intended to be used only once,
    ///     the Authorization Server MUST instruct the User Agent (and any intermediaries) not to store or reuse the content of the response.
    /// </summary>
    public const string FormPost = "form_post";
}