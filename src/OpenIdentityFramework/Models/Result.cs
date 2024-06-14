using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenIdentityFramework.Models;

public class Result<TOk, TError>
{
    public Result(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
        HasError = true;
    }

    public Result(TOk ok)
    {
        ArgumentNullException.ThrowIfNull(ok);
        Ok = ok;
    }

    public TOk? Ok { get; }
    public TError? Error { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Ok))]
    public bool HasError { get; }
}

public class Result<TOk>
{
    public Result()
    {
        HasError = true;
    }

    public Result(TOk ok)
    {
        ArgumentNullException.ThrowIfNull(ok);
        Ok = ok;
    }

    public TOk? Ok { get; }

    [MemberNotNullWhen(false, nameof(Ok))]
    public bool HasError { get; }
}