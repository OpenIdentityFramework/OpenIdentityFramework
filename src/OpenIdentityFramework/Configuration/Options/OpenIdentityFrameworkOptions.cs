namespace OpenIdentityFramework.Configuration.Options;

public class OpenIdentityFrameworkOptions
{
    public ErrorHandlingOptions ErrorHandling { get; set; } = new();
    public EndpointOptions Endpoints { get; set; } = new();
}