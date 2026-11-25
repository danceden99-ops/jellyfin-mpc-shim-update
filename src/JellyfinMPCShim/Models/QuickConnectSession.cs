namespace JellyfinMPCShim.Models;

public sealed class QuickConnectSession
{
    public string Code { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public bool Authenticated { get; init; }
}
