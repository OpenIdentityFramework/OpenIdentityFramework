﻿using System;
using OpenIdentityFramework.Services.Static.SyntaxValidation.Protocol;

namespace OpenIdentityFramework.Services.Static.SyntaxValidation;

public static class CodeSyntaxValidator
{
    public static bool IsValid(ReadOnlySpan<char> value)
    {
        // https://www.ietf.org/archive/id/draft-ietf-oauth-v2-1-09.html#appendix-A.11
        return VsCharSyntaxValidator.IsValid(value);
    }
}
