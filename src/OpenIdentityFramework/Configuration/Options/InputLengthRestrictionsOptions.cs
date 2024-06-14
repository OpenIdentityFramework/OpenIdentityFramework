namespace OpenIdentityFramework.Configuration.Options;

public class InputLengthRestrictionsOptions
{
    /// <summary>
    ///     Max length for "client_id".
    /// </summary>
    public int ClientId { get; set; } = 100;

    /// <summary>
    ///     Max length for "response_type".
    /// </summary>
    public int ResponseType { get; set; } = 100;

    /// <summary>
    ///     Max length for "response_mode".
    /// </summary>
    public int ResponseMode { get; set; } = 100;

    /// <summary>
    ///     Max length for "state".
    /// </summary>
    public int State { get; set; } = 300;
}