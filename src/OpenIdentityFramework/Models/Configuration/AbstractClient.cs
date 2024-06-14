using System.Collections.Generic;

namespace OpenIdentityFramework.Models.Configuration;

/// <summary>
///     OAuth 2.1 / OpenID Connect 1.0 client model
/// </summary>
public abstract class AbstractClient
{
    //   ___    _         _   _     ____    _
    //  / _ \  / \  _   _| |_| |__ |___ \  / |
    // | | | |/ _ \| | | | __| '_ \  __) | | |
    // | |_| / ___ \ |_| | |_| | | |/ __/ _| |
    //  \___/_/   \_\__,_|\__|_| |_|_____(_)_|
    //

    /// <summary>
    ///     Returns <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-2.2">the client identifier ("client_id")</a>, which is a unique string representing the registration information provided by the client.
    ///     The value of the client identifier corresponds to the "client_id" value described in <a href="https://www.rfc-editor.org/rfc/rfc7591#section-3.2.1">section 3.2.1 of the OAuth 2.0 Dynamic Client Registration Protocol specification</a>.
    ///     It SHOULD NOT be currently valid for any other registered client.
    /// </summary>
    /// <returns>A <see cref="string" /> that contains a non-null and non-empty value. According to the specification, string characters must be in the range 0x20 - 0x7E ASCII characters.</returns>
    public abstract string GetClientId();

    /// <summary>
    ///     Returns the expiration date of the client secret. Time at which the client secret will expire or 0 if it will not expire. The time is represented as the number of seconds from 1970-01-01T00:00:00Z as measured in UTC until the date/time of expiration.
    /// </summary>
    /// <returns>A <see cref="long" /> value that is equal to or greater than 0</returns>
    public abstract long GetClientSecretExpiresAt();

    /// <summary>
    ///     Returns OAuth 2.1 grant type strings that the client can use at the token endpoint.<br />
    ///     Allowed values are:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>"authorization_code"</term>
    ///             <description>
    ///                 The authorization code grant type defined in <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.3">OAuth 2.1, section 4.1.3</a>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>"implicit"</term>
    ///             <description>
    ///                 <para>The implicit grant type defined in <a href="https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.3.2">OpenID Connect 1.0, section 3.2</a>.</para>
    ///                 <para>
    ///                     Important note: This grant type will only work for getting an id_token (as it <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-10.1">does not conflict with the OAuth 2.1 specification</a>), or in Hybrid Flow.
    ///                     Direct token issuance from authorization endpoint is <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-10.1">not allowed according to the OAuth 2.1 specification</a>.
    ///                 </para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>"client_credentials"</term>
    ///             <description>
    ///                 The client credentials grant type defined in <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.2.1">OAuth 2.1, section 4.2.1</a>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>"refresh_token"</term>
    ///             <description>
    ///                 The refresh token grant type defined in <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.3.1">OAuth 2.1, section 4.3.1</a>.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     If omitted, the default is that the Client will use only the authorization_code Grant Type
    /// </summary>
    /// <returns>A set that contains 0 or more values. Can be <see langword="null" />.</returns>
    public abstract IReadOnlySet<string>? GetGrantTypes();

    /// <summary>
    ///     Returns OAuth 2.1 response type strings that the client can use at the authorization endpoint.<br />
    ///     Allowed values are:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>"code"</term>
    ///             <description>
    ///                 The authorization code response type defined in <a href="https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#section-4.1.1">OAuth 2.1, section 4.1.1</a>.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>"id_token"</term>
    ///             <description>
    ///                 The ID Token response type defined in <a href="https://openid.net/specs/openid-connect-core-1_0.html#rfc.section.2">OpenID Connect 1.0, section 2</a>.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <returns>A set that contains 0 or more values. Can be <see langword="null" />.</returns>
    public abstract IReadOnlySet<string>? GetResponseTypes();
}