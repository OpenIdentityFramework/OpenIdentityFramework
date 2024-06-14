using System;

namespace OpenIdentityFramework.Models;

public class ProtocolError
{
    public ProtocolError(string error, string? description)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(error));
        }

        Error = error;
        Description = description;
    }

    public string Error { get; }

    public string? Description { get; }
}