using JellyfinMPCShim.Models;

namespace JellyfinMPCShim.Interfaces;

public interface IMpcClient
{
    Task Start(string path, int port);
    Task Start(string path, int port, MpcFork fork, bool startFullscreen = false, string? extraLaunchArgs = null);
}
