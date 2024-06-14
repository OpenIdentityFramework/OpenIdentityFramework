using System;
using OpenIdentityFramework.Services.SyntaxValidation.Protocol;

namespace OpenIdentityFramework.Services.SyntaxValidation;

public static class StateSyntaxValidator
{
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#appendix-A.5
        return VsCharSyntaxValidator.IsValid(value);
    }
}