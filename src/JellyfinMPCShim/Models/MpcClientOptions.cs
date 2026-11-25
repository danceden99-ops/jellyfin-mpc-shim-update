namespace JellyfinMPCShim.Models;

public class MpcClientOptions
{
    public string? TempPath { get; set; }
    public MpcFork Fork { get; set; } = MpcFork.Auto;
    public bool StartFullscreen { get; set; }
    public string? ExtraLaunchArgs { get; set; }
}
