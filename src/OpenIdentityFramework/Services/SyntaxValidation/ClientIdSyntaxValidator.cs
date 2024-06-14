using System;
using OpenIdentityFramework.Services.SyntaxValidation.Protocol;

namespace OpenIdentityFramework.Services.SyntaxValidation;

public static class ClientIdSyntaxValidator
{
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#appendix-A
        return VsCharSyntaxValidator.IsValid(value);
    }
}