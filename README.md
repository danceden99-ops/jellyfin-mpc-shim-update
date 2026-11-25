# Jellyfin MPC Shim (Jellyfin 12.x Update)

Jellyfin MPC Shim is a Windows cast client for Jellyfin that plays your media files using Media Player Classic forks, similar to [jellyfin-mpv-shim](https://github.com/jellyfin/jellyfin-mpv-shim).

## Acknowledgment

This project is a fork/update of the original **[Trellmor/jellyfin-mpc-shim](https://github.com/Trellmor/jellyfin-mpc-shim)** repository. All credit for the original architecture, MPC-HC Web Interface integration, and SyncPlay support goes to Trellmor and contributors.

## What's New in This Build

### Jellyfin Server 12.x API & Login Support
- **Standard `Authorization: MediaBrowser` Headers:** Replaces legacy `X-Emby-Authorization` / `X-Emby-Token` headers with the standard `Authorization: MediaBrowser Client="...", Device="...", DeviceId="...", Version="...", Token="..."` format required by Jellyfin 12.x.
- **Updated WebSocket & Stream URLs:** Passes the `Authorization` header on `/socket` WebSocket handshakes and uses `ApiKey` + `DeviceId` query parameters (instead of the removed `api_key` parameter) for WebSockets and direct stream URLs.
- **Quick Connect & Persisted Token Login:** Supports signing in via **Quick Connect** (without storing a plaintext password) as well as **Username / Password**, persists the resulting access token across restarts, and includes a **Sign Out** button to revoke the token when switching accounts.
- **CLI Token Support:** `JellyfinMPCShim.Cli` also supports `AccessToken` in `appsettings.json` alongside `Username` / `Password`.

### Supported MPC Forks
You can select your player fork in the Tray settings (or let it **Auto-detect from executable**):
- **[MPC-HC (clsid2)](https://github.com/clsid2/mpc-hc)** — Standard web-interface startup wait and command timing.
- **[MPC-BE (Aleksoid1978)](https://github.com/Aleksoid1978/MPC-BE)** — Extended startup wait (8s) and command settle delay (150ms) for internal splitters/renderers.
- **[Unplugged MPC-HC (spedagadi)](https://github.com/spedagadi/unplugged-mpc-hc)** — Extended startup wait (15s) and command settle delay (300ms) for ML/DirectShow tone-mapping initialization. *(Note: Current standalone `zion-s3-shell` releases do not expose the HTTP Web Interface; see the [Unplugged workaround](#3-unplugged-mpc-hc-spedagadiunplugged-mpc-hc) below to use `clsid2/mpc-hc` with `unplugged-VideoRenderer`.)*
- **Launch Options:** Includes **Start Fullscreen** (`/fullscreen`) and **Extra Args** for custom command-line switches.

---

## Build Requirements

- **OS:** Windows 10 / Windows 11 (required to build the `net8.0-windows7.0` WinForms Tray application)
- **.NET SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (verified with **.NET SDK 10.0.404**)
- **Runtime (on target machine):** [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0) (required when running the framework-dependent build)
- **Internet access** during `dotnet restore` to download NuGet packages (`Jellyfin.Sdk`, `MPC-HC.Domain`, `Websocket.Client`, `Flurl`, `FontAwesome.Sharp`, `Serilog`, etc.)

---

## Build Instructions

Open a terminal (PowerShell or Command Prompt) in the repository root:

1. **Restore NuGet packages:**
   ```powershell
   dotnet restore JellyfinMPCShim.sln
   ```

2. **Build the solution in Release mode:**
   ```powershell
   dotnet build JellyfinMPCShim.sln --no-restore -c Release
   ```

3. **Build the Self-Contained Standalone Tray Executable (No .NET Runtime Required on Target PC):**
   Run the included batch script from the repository root:
   ```cmd
   build-tray-standalone.bat
   ```
   Or run the publish command manually:
   ```powershell
   dotnet publish src\JellyfinMPCShim.Tray\JellyfinMPCShim.Tray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\tray-standalone
   ```
   This bundles the .NET 8.0 Desktop Runtime and managed dependencies into `publish\tray-standalone\JellyfinMPCShim.Tray.exe` (alongside its config and native WindowsDesktop DLLs). Copy the entire `publish\tray-standalone\` folder to any Windows x64 PC—no .NET installation is required on that machine.

---

## Running the Application

- **Standalone Tray App (self-contained, no .NET runtime needed):**
  ```text
  publish\tray-standalone\JellyfinMPCShim.Tray.exe
  ```
- **Framework-Dependent Tray App (requires .NET 8.0 Desktop Runtime):**
  ```text
  src\JellyfinMPCShim.Tray\bin\Release\net8.0-windows7.0\JellyfinMPCShim.Tray.exe
  ```
- **Framework-Dependent CLI App (requires .NET 8.0 Runtime):**
  ```text
  src\JellyfinMPCShim.Cli\bin\Release\net8.0\JellyfinMPCShim.Cli.exe
  ```

You can run the standalone Tray app directly from PowerShell or Explorer:

```powershell
.\publish\tray-standalone\JellyfinMPCShim.Tray.exe
```

---

## Player & Jellyfin Setup

1. Install your preferred MPC fork ([clsid2/mpc-hc](https://github.com/clsid2/mpc-hc), [Aleksoid1978/MPC-BE](https://github.com/Aleksoid1978/MPC-BE), or [spedagadi/unplugged-mpc-hc](https://github.com/spedagadi/unplugged-mpc-hc)).
2. Open your MPC player and enable the built-in web interface:
   - Go to **View > Options > Player > Web Interface**
   - Check **Listen on port** (default: `13579`).
3. Launch `JellyfinMPCShim.Tray.exe`.
4. In the **Jellyfin** section:
   - Enter your **Server** URL (e.g., `http://192.168.x.x:8096` or `https://your-domain`).
   - Either enter **Username** + **Password** and click **OK**, **or** click **Quick Connect** and approve the displayed code in your Jellyfin web/mobile app (the status label turns green **Connected** once signed in).
5. In the **MPC** section:
   - Select the path to your MPC executable (e.g., `mpc-hc64.exe`, `mpc-be64.exe`, or `unplugged-mpc-hc.exe`).
   - Set **Port** to match the Web Interface port configured in step 2 (`13579`).
   - Choose your **Fork** (or leave on **Auto-detect from executable**), and optionally enable **Start Fullscreen** or add **Extra Args**.
6. Click **OK** to save and minimize to the Windows system tray (or **Cancel** to discard changes).
7. Open Jellyfin Web (or any Jellyfin client), click the **Cast** icon, and select **Jellyfin MPC Shim Tray**.

---

## Tray Icon Status & Hover Tooltip

The system tray icon is a color-coded triangle that reflects the shim's live connection status:

- **Green:** Connected to the Jellyfin server **and** the configured MPC executable exists on disk (`Status: OK`).
- **Yellow:** Currently attempting to connect to Jellyfin or waiting for Quick Connect approval (`Status: CONNECTING`).
- **Red:** Not connected to the Jellyfin server and/or the MPC executable path is missing or invalid (`Status: FAIL`).

Hovering your mouse over the tray icon displays a summary tooltip showing:
- `Server: <url> [OK | FAIL | CONNECTING]`
- `MPC: <executable path> [OK | FAIL]`
- `Status: OK | CONNECTING | FAIL`

---

## Recommended MPC Fork Settings for Remote Control

All three supported MPC forks must have their built-in **Web Interface** enabled so the shim can open streams, seek, pause/resume, and report playback progress back to Jellyfin.

### 1. MPC-HC ([clsid2/mpc-hc](https://github.com/clsid2/mpc-hc))
- **Enable Web Interface:** **View > Options > Player > Web Interface** → check **Listen on port** (`13579`), and check **Allow access from localhost only** (recommended for security when the shim runs on the same PC).
- **Single-Instance Mode:** **View > Options > Player** → select **Use the same player for each media file** so cast commands reuse the active window instead of spawning multiple players.
- **Shim Setting:** Set **Fork** to `MPC-HC (clsid2)` or `Auto-detect from executable`.

### 2. MPC-BE ([Aleksoid1978/MPC-BE](https://github.com/Aleksoid1978/MPC-BE))
- **Enable Web Interface:** **View > Options > Player > Web Interface** → check **Listen on port** (`13579`), and check **Allow access from localhost only**.
- **Single-Instance Mode:** **View > Options > Player** → select **Use the same player for each media file**.
- **Playback Auto-Start:** Keep **Play** enabled on open (default) so the shim's `OpenFile` + `Play` commands start playback immediately.
- **Shim Setting:** Set **Fork** to `MPC-BE (Aleksoid1978)` or `Auto-detect from executable` (uses an 8s startup wait and 150ms command settle delay).

### 3. Unplugged MPC-HC ([spedagadi/unplugged-mpc-hc](https://github.com/spedagadi/unplugged-mpc-hc))

> **Current Limitation (`zion-s3-shell`):** Current Unplugged releases (v2.1/v2.2 on the `zion-s3-shell` branch) replace MPC-HC's classic MFC menus, dialogs, and registry schema (`HKCU\Software\MPC-HC`) with a custom DirectX/ImGui overlay (`s3_shell`) that reads/writes only `s3_shell.ini` and the Unplugged registry hive. There is currently no UI toggle, setting, or registry key exposed to enable `EnableWebServer` or `WebServerPort`, and remote HTTP control (`/variables.html`, `/command.html` on port `13579`) is a future roadmap item rather than an active feature. As a result, **Unplugged's standalone player cannot currently be controlled by `jellyfin-mpc-shim`**.

#### Recommended Workaround: Standard `clsid2/mpc-hc` + `unplugged-VideoRenderer`
You can still get full Jellyfin casting **plus** Unplugged's ML tone mapping, Zion Grade, and DisplayCAL `.cube` LUT support by pairing standard MPC-HC with Unplugged's standalone DirectShow renderer:

1. Download the standalone **[unplugged-VideoRenderer](https://github.com/spedagadi/unplugged-VideoRenderer)** release and register its `.ax` DirectShow filter.
2. Install standard **[MPC-HC (clsid2)](https://github.com/clsid2/mpc-hc)**, open **View > Options > Playback > Output**, and select **Unplugged Video Renderer** as your DirectShow Video Renderer.
3. Enable the Web Interface (**View > Options > Player > Web Interface** → check **Listen on port** `13579`) and **Use the same player for each media file** in standard MPC-HC.
4. Point `jellyfin-mpc-shim` to `mpc-hc64.exe` (you can select `Unplugged MPC-HC (spedagadi)` in **Fork** if you want the extended 15s startup wait and 300ms settle delay for the ML renderer pipeline, or `MPC-HC (clsid2)`).

---

## SyncPlay

Right-click the system tray icon and use the **Groups** menu to view and join active SyncPlay groups.

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)** — see [LICENSE.md](LICENSE.md) for full details. It is a modified fork of [Trellmor/jellyfin-mpc-shim](https://github.com/Trellmor/jellyfin-mpc-shim), also distributed under the GPL-3.0 license.
