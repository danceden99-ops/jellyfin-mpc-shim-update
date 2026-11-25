namespace JellyfinMPCShim.Models;

public sealed class MpcForkProfile
{
    public MpcFork Fork { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int StartupTimeoutMs { get; init; }
    public int StartupPollIntervalMs { get; init; }
    public int CommandSettleDelayMs { get; init; }
    public string FullscreenSwitch { get; init; } = "/fullscreen";

    public static MpcFork ResolveFork(MpcFork configuredFork, string? executablePath)
    {
        if (configuredFork != MpcFork.Auto)
        {
            return configuredFork;
        }

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var normalized = executablePath.ToLowerInvariant();
            if (normalized.Contains("unplugged", StringComparison.Ordinal))
            {
                return MpcFork.UnpluggedMpcHc;
            }

            if (normalized.Contains("mpc-be", StringComparison.Ordinal))
            {
                return MpcFork.MpcBe;
            }
        }

        return MpcFork.MpcHcClsid2;
    }

    public static MpcForkProfile GetProfile(MpcFork configuredFork, string? executablePath)
    {
        var resolved = ResolveFork(configuredFork, executablePath);
        return resolved switch
        {
            MpcFork.MpcBe => new MpcForkProfile
            {
                Fork = MpcFork.MpcBe,
                DisplayName = "MPC-BE (Aleksoid1978)",
                StartupTimeoutMs = 8000,
                StartupPollIntervalMs = 250,
                CommandSettleDelayMs = 150,
                FullscreenSwitch = "/fullscreen"
            },
            MpcFork.UnpluggedMpcHc => new MpcForkProfile
            {
                Fork = MpcFork.UnpluggedMpcHc,
                DisplayName = "Unplugged MPC-HC (spedagadi)",
                // Unplugged initializes ML/DirectShow tone-mapping pipelines on startup
                StartupTimeoutMs = 15000,
                StartupPollIntervalMs = 300,
                CommandSettleDelayMs = 300,
                FullscreenSwitch = "/fullscreen"
            },
            _ => new MpcForkProfile
            {
                Fork = MpcFork.MpcHcClsid2,
                DisplayName = "MPC-HC (clsid2)",
                StartupTimeoutMs = 5000,
                StartupPollIntervalMs = 200,
                CommandSettleDelayMs = 100,
                FullscreenSwitch = "/fullscreen"
            }
        };
    }

    public static MpcFork ParseOrDefault(string? value)
    {
        return Enum.TryParse<MpcFork>(value, ignoreCase: true, out var parsed)
            ? parsed
            : MpcFork.Auto;
    }
}
