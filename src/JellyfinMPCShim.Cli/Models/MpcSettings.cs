namespace JellyfinMPCShim.Cli.Models;
public class MpcSettings
{
    public string Path { get; set; } = null!;
    public int Port { get; set; }
    public string? Fork { get; set; }
    public bool StartFullscreen { get; set; }
    public string? ExtraArgs { get; set; }
}
