using OpenIdentityFramework.Configuration.Options.Endpoint;

namespace OpenIdentityFramework.Configuration.Options;

public class EndpointOptions
{
    public AuthorizeEndpointOptions Authorize { get; set; } = new();
}