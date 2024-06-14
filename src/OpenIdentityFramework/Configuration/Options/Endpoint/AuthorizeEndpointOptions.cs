using OpenIdentityFramework.Constants;

namespace OpenIdentityFramework.Configuration.Options.Endpoint;

public class AuthorizeEndpointOptions
{
    public string Path { get; set; } = DefaultRoute.Authorize;
}