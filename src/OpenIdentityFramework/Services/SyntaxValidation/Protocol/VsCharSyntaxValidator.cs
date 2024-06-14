using System;

namespace OpenIdentityFramework.Services.SyntaxValidation.Protocol;

public static class VsCharSyntaxValidator
{
    private const char VsCharMin = (char) 0x20;
    private const char VsCharMax = (char) 0x7E;

    public static bool IsValid(ReadOnlySpan<char> value)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-11.html#appendix-A
        foreach (var ch in value)
        {
            if (ch is < VsCharMin or > VsCharMax)
            {
                return false;
            }
        }

        return true;
    }
}