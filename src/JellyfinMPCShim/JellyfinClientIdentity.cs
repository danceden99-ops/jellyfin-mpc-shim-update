namespace JellyfinMPCShim;

public sealed class JellyfinClientIdentity
{
    public string ClientName { get; private set; } = "Jellyfin MPC Shim";
    public string ClientVersion { get; private set; } = "0.3.0";
    public string DeviceName { get; private set; } = Environment.MachineName;
    public string DeviceId { get; private set; } = Guid.NewGuid().ToString("N");

    public void Initialize(string clientName, string clientVersion, string deviceName, string deviceId)
    {
        ClientName = clientName;
        ClientVersion = clientVersion;
        DeviceName = deviceName;
        DeviceId = deviceId;
    }
}
