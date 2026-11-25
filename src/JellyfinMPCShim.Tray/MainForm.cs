using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using FontAwesome.Sharp;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using JellyfinMPCShim.Interfaces;
using JellyfinMPCShim.Models;
using JellyfinMPCShim.Models.WebsocketData;
using JellyfinMPCShim.Tray.Properties;
using Microsoft.Extensions.Hosting;

namespace JellyfinMPCShim.Tray;
public partial class MainForm : Form, IJellyfinMessageHandler
{
    private readonly IHostApplicationLifetime _hostlLifetime;
    private readonly IJellyfinClient _jellyfinClient;
    private readonly IMpcClient _mpcClient;
    private readonly JellyfinSdkSettings _jellyfinSdkSettings;
    private readonly JellyfinClientIdentity _clientIdentity;
    private bool _visible;
    private bool _isConnecting;
    private Guid? _currentGroup;
    private CancellationTokenSource? _quickConnectCts;
    private System.Drawing.Icon? _currentTrayIcon;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public MainForm(
        IHostApplicationLifetime hostlLifetime,
        IJellyfinClient jellyfinClient,
        IMpcClient mpcClient,
        JellyfinSdkSettings jellyfinSdkSettings,
        JellyfinClientIdentity clientIdentity)
    {
        _hostlLifetime = hostlLifetime;
        _jellyfinClient = jellyfinClient;
        _jellyfinClient.AddMessageHandler(this);
        _mpcClient = mpcClient;
        _jellyfinSdkSettings = jellyfinSdkSettings;
        _clientIdentity = clientIdentity;
        InitializeComponent();
        connectionToolStripMenuItem.Image = IconChar.Cloud.ToBitmap(width: 16, height: 16, color: Color.Black);
        settingsToolStripMenuItem.Image = IconChar.Gear.ToBitmap(width: 16, height: 16, color: Color.Black);
        groupsToolStripMenuItem.Image = IconChar.UserGroup.ToBitmap(width: 16, height: 16, color: Color.Black);
        logsToolStripMenuItem.Image = IconChar.FileLines.ToBitmap(width: 16, height: 16, color: Color.Black);
        exitToolStripMenuItem.Image = IconChar.Close.ToBitmap(width: 16, height: 16, color: Color.Black);

        comboBoxMpcFork.DataSource = new[]
        {
            new MpcForkItem(MpcFork.Auto, "Auto-detect from executable"),
            new MpcForkItem(MpcFork.MpcHcClsid2, "MPC-HC (clsid2)"),
            new MpcForkItem(MpcFork.MpcBe, "MPC-BE (Aleksoid1978)"),
            new MpcForkItem(MpcFork.UnpluggedMpcHc, "Unplugged MPC-HC (spedagadi)")
        };
        comboBoxMpcFork.DisplayMember = nameof(MpcForkItem.Label);
        comboBoxMpcFork.ValueMember = nameof(MpcForkItem.Fork);

        UpdateTrayStatusIcon();
    }

    private void LoadSettingsToForm()
    {
        textBoxJellyfinHost.Text = Settings.Default.JellyfinHost;
        textBoxJellyfinUsername.Text = Settings.Default.JellyfinUser;
        textBoxJellyfinPassword.Text = Settings.Default.JellyfinPw;
        textBoxCastName.Text = string.IsNullOrWhiteSpace(Settings.Default.CastDeviceName)
            ? "Jellyfin MPC Shim Tray"
            : Settings.Default.CastDeviceName;
        textBoxMpcPath.Text = Settings.Default.MpcPath;
        numericUpDownMpcPort.Value = Settings.Default.MpcPort;
        comboBoxMpcFork.SelectedValue = MpcForkProfile.ParseOrDefault(Settings.Default.MpcFork);
        checkBoxMpcFullscreen.Checked = Settings.Default.MpcFullscreen;
        textBoxMpcArgs.Text = Settings.Default.MpcExtraArgs;
    }

    private void ApplyCastIdentity()
    {
        var castName = string.IsNullOrWhiteSpace(Settings.Default.CastDeviceName)
            ? "Jellyfin MPC Shim Tray"
            : Settings.Default.CastDeviceName.Trim();
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.3.0";
        _jellyfinSdkSettings.Initialize(castName, version, Environment.MachineName, _clientIdentity.DeviceId);
        _clientIdentity.Initialize(castName, version, Environment.MachineName, _clientIdentity.DeviceId);
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        Visible = false;
        ShowInTaskbar = false;

        LoadSettingsToForm();
        UpdateAuthStatusLabel();
        UpdateTrayStatusIcon();

        var hasToken = !string.IsNullOrWhiteSpace(Settings.Default.JellyfinToken);
        var hasCredentials = !string.IsNullOrWhiteSpace(Settings.Default.JellyfinUser) &&
                             !string.IsNullOrWhiteSpace(Settings.Default.JellyfinPw);

        if (string.IsNullOrWhiteSpace(Settings.Default.JellyfinHost) ||
            (!hasToken && !hasCredentials) ||
            string.IsNullOrWhiteSpace(Settings.Default.MpcPath) ||
            !File.Exists(Settings.Default.MpcPath))
        {
            ShowForm();
            return;
        }

        try
        {
            await ConnectJellyfin();
        }
        catch (Exception ex)
        {
            ShowForm();
            MessageBox.Show($"Error connecting to Jellyfin\r\r{ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await StartMpcClient();
    }

    private Task StartMpcClient()
    {
        var fork = MpcForkProfile.ParseOrDefault(Settings.Default.MpcFork);
        return _mpcClient.Start(
            Settings.Default.MpcPath,
            Settings.Default.MpcPort,
            fork,
            Settings.Default.MpcFullscreen,
            Settings.Default.MpcExtraArgs);
    }

    private void UpdateAuthStatusLabel()
    {
        if (_jellyfinClient.IsConnected)
        {
            labelAuthStatus.Text = "Connected";
            labelAuthStatus.ForeColor = Color.ForestGreen;
            labelAuthStatus.Font = new Font(labelAuthStatus.Font, FontStyle.Bold);
        }
        else if (!string.IsNullOrWhiteSpace(Settings.Default.JellyfinToken))
        {
            labelAuthStatus.Text = "Saved token available";
            labelAuthStatus.ForeColor = SystemColors.ControlText;
            labelAuthStatus.Font = new Font(labelAuthStatus.Font, FontStyle.Regular);
        }
        else
        {
            labelAuthStatus.Text = "Not signed in";
            labelAuthStatus.ForeColor = SystemColors.ControlText;
            labelAuthStatus.Font = new Font(labelAuthStatus.Font, FontStyle.Regular);
        }
    }

    private void UpdateTrayStatusIcon()
    {
        var serverHost = string.IsNullOrWhiteSpace(Settings.Default.JellyfinHost)
            ? "(not set)"
            : Settings.Default.JellyfinHost.Trim();
        var mpcPath = string.IsNullOrWhiteSpace(Settings.Default.MpcPath)
            ? "(not set)"
            : Settings.Default.MpcPath.Trim();

        var serverOk = _jellyfinClient.IsConnected;
        var mpcOk = !string.IsNullOrWhiteSpace(Settings.Default.MpcPath) && File.Exists(Settings.Default.MpcPath);

        Color ballColor;
        Color borderColor;
        string serverStatus;
        string overallStatus;

        if (_isConnecting)
        {
            ballColor = Color.FromArgb(234, 179, 8);
            borderColor = Color.FromArgb(161, 98, 7);
            serverStatus = "CONNECTING";
            overallStatus = "CONNECTING";
        }
        else if (serverOk && mpcOk)
        {
            ballColor = Color.FromArgb(34, 197, 94);
            borderColor = Color.FromArgb(21, 128, 61);
            serverStatus = "OK";
            overallStatus = "OK";
        }
        else
        {
            ballColor = Color.FromArgb(239, 68, 68);
            borderColor = Color.FromArgb(185, 28, 28);
            serverStatus = serverOk ? "OK" : "FAIL";
            overallStatus = "FAIL";
        }

        var mpcStatus = mpcOk ? "OK" : "FAIL";

        var oldIcon = _currentTrayIcon;
        var newIcon = CreateTriangleIcon(ballColor, borderColor);
        _currentTrayIcon = newIcon;
        trayIcon.Icon = newIcon;
        oldIcon?.Dispose();

        trayIcon.Text = BuildTrayTooltip(serverHost, serverStatus, mpcPath, mpcStatus, overallStatus);
    }

    private static string BuildTrayTooltip(
        string serverHost,
        string serverStatus,
        string mpcPath,
        string mpcStatus,
        string overallStatus)
    {
        const int maxLength = 127;
        var line3 = $"Status: {overallStatus}";

        var line1 = $"Server: {serverHost} [{serverStatus}]";
        var line2 = $"MPC: {mpcPath} [{mpcStatus}]";
        var full = $"{line1}\n{line2}\n{line3}";
        if (full.Length <= maxLength)
        {
            return full;
        }

        var mpcName = Path.GetFileName(mpcPath);
        if (string.IsNullOrEmpty(mpcName))
        {
            mpcName = mpcPath;
        }

        line2 = $"MPC: {mpcName} [{mpcStatus}]";
        full = $"{line1}\n{line2}\n{line3}";
        if (full.Length <= maxLength)
        {
            return full;
        }

        var availableForHost = Math.Max(8, maxLength - ($"Server:  [{serverStatus}]\n{line2}\n{line3}").Length);
        var trimmedHost = serverHost.Length > availableForHost
            ? string.Concat(serverHost.AsSpan(0, availableForHost - 3), "...")
            : serverHost;
        var result = $"Server: {trimmedHost} [{serverStatus}]\n{line2}\n{line3}";
        return result.Length <= maxLength ? result : result[..maxLength];
    }

    private static System.Drawing.Icon CreateTriangleIcon(Color fillColor, Color borderColor)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            PointF[] triangle =
            [
                new PointF(16f, 3f),
                new PointF(29f, 28f),
                new PointF(3f, 28f)
            ];

            using var brush = new SolidBrush(fillColor);
            using var pen = new Pen(borderColor, 2f) { LineJoin = LineJoin.Round };
            g.FillPolygon(brush, triangle);
            g.DrawPolygon(pen, triangle);

            PointF[] highlightTriangle =
            [
                new PointF(16f, 7f),
                new PointF(21f, 17f),
                new PointF(11f, 17f)
            ];
            using var highlight = new SolidBrush(Color.FromArgb(75, Color.White));
            g.FillPolygon(highlight, highlightTriangle);
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var temp = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private void ShowForm()
    {
        _visible = true;
        Visible = true;
        ShowInTaskbar = true;
        UpdateAuthStatusLabel();
        Show();
    }

    private async Task ConnectJellyfin()
    {
        _isConnecting = true;
        UpdateTrayStatusIcon();
        try
        {
            var host = Settings.Default.JellyfinHost;
            var token = Settings.Default.JellyfinToken;
            var user = Settings.Default.JellyfinUser;
            var pw = Settings.Default.JellyfinPw;

            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await _jellyfinClient.StartWithToken(host, token);
                }
                catch when (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pw))
                {
                    // Saved token expired or was revoked; fall back to username/password login
                    Settings.Default.JellyfinToken = string.Empty;
                    await _jellyfinClient.Start(host, user, pw);
                    Settings.Default.JellyfinToken = _jellyfinClient.AccessToken ?? string.Empty;
                    Settings.Default.Save();
                }
            }
            else
            {
                await _jellyfinClient.Start(host, user, pw);
                Settings.Default.JellyfinToken = _jellyfinClient.AccessToken ?? string.Empty;
                Settings.Default.Save();
            }

            connectionToolStripMenuItem.Text = $"Connected to {Settings.Default.JellyfinHost}";
            connectionToolStripMenuItem.Visible = true;
            _isConnecting = false;
            UpdateAuthStatusLabel();
            UpdateTrayStatusIcon();
        }
        catch
        {
            _isConnecting = false;
            connectionToolStripMenuItem.Visible = false;
            UpdateAuthStatusLabel();
            UpdateTrayStatusIcon();
            throw;
        }
    }

    private void trayIcon_DoubleClick(object sender, EventArgs e)
    {
        if (_visible)
        {
            HideForm();
        }
        else
        {
            ShowForm();
        }
    }

    private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        ShowForm();
    }

    private async void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _quickConnectCts?.Cancel();
        await _jellyfinClient.Stop();
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            _quickConnectCts?.Cancel();
            HideForm();
            e.Cancel = true;
        }
    }

    private void HideForm()
    {
        _visible = false;
        ShowInTaskbar = false;
        Hide();
    }

    private async void buttonSave_Click(object sender, EventArgs e)
    {
        _quickConnectCts?.Cancel();

        var credentialsChanged =
            !string.Equals(Settings.Default.JellyfinHost, textBoxJellyfinHost.Text, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Settings.Default.JellyfinUser, textBoxJellyfinUsername.Text, StringComparison.Ordinal) ||
            !string.Equals(Settings.Default.JellyfinPw, textBoxJellyfinPassword.Text, StringComparison.Ordinal);

        var castName = string.IsNullOrWhiteSpace(textBoxCastName.Text)
            ? "Jellyfin MPC Shim Tray"
            : textBoxCastName.Text.Trim();

        Settings.Default.JellyfinHost = textBoxJellyfinHost.Text;
        Settings.Default.JellyfinUser = textBoxJellyfinUsername.Text;
        Settings.Default.JellyfinPw = textBoxJellyfinPassword.Text;
        Settings.Default.CastDeviceName = castName;
        Settings.Default.MpcPath = textBoxMpcPath.Text;
        Settings.Default.MpcPort = (int)numericUpDownMpcPort.Value;
        Settings.Default.MpcFork = ((comboBoxMpcFork.SelectedValue as MpcFork?) ?? MpcFork.Auto).ToString();
        Settings.Default.MpcFullscreen = checkBoxMpcFullscreen.Checked;
        Settings.Default.MpcExtraArgs = textBoxMpcArgs.Text;

        if (credentialsChanged &&
            (!string.IsNullOrWhiteSpace(textBoxJellyfinUsername.Text) || !string.IsNullOrWhiteSpace(textBoxJellyfinPassword.Text)))
        {
            Settings.Default.JellyfinToken = string.Empty;
        }

        Settings.Default.Save();
        ApplyCastIdentity();
        try
        {
            await _jellyfinClient.Stop(notifyHandlers: false);
            await ConnectJellyfin();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error connecting to Jellyfin\r\r{ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await StartMpcClient();
        UpdateTrayStatusIcon();
        HideForm();
    }

    private void buttonCancel_Click(object? sender, EventArgs e)
    {
        _quickConnectCts?.Cancel();
        LoadSettingsToForm();
        UpdateAuthStatusLabel();
        HideForm();
    }

    private async void buttonQuickConnect_Click(object? sender, EventArgs e)
    {
        if (_quickConnectCts != null)
        {
            _quickConnectCts.Cancel();
            return;
        }

        var host = textBoxJellyfinHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            MessageBox.Show("Please enter the Jellyfin Server URL first.", "Quick Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _quickConnectCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        _isConnecting = true;
        UpdateTrayStatusIcon();
        buttonQuickConnect.Text = "Cancel Quick Connect";
        labelAuthStatus.Text = "Checking Quick Connect...";
        labelAuthStatus.ForeColor = SystemColors.ControlText;
        labelAuthStatus.Font = new Font(labelAuthStatus.Font, FontStyle.Regular);

        try
        {
            var enabled = await _jellyfinClient.IsQuickConnectEnabled(host, _quickConnectCts.Token);
            if (!enabled)
            {
                labelAuthStatus.Text = "Quick Connect disabled";
                MessageBox.Show("Quick Connect is not enabled on this Jellyfin server.", "Quick Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var session = await _jellyfinClient.InitiateQuickConnect(host, _quickConnectCts.Token);
            labelAuthStatus.Text = $"Code: {session.Code} (approve in Jellyfin)";

            while (!_quickConnectCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), _quickConnectCts.Token);
                var status = await _jellyfinClient.CheckQuickConnect(host, session.Secret, _quickConnectCts.Token);
                if (status.Authenticated)
                {
                    var castName = string.IsNullOrWhiteSpace(textBoxCastName.Text)
                        ? "Jellyfin MPC Shim Tray"
                        : textBoxCastName.Text.Trim();
                    Settings.Default.JellyfinHost = host;
                    Settings.Default.CastDeviceName = castName;
                    Settings.Default.Save();
                    ApplyCastIdentity();

                    labelAuthStatus.Text = "Connecting...";
                    await _jellyfinClient.Stop(notifyHandlers: false);
                    await _jellyfinClient.StartWithQuickConnect(host, session.Secret, _quickConnectCts.Token);

                    Settings.Default.JellyfinToken = _jellyfinClient.AccessToken ?? string.Empty;
                    Settings.Default.MpcPath = textBoxMpcPath.Text;
                    Settings.Default.MpcPort = (int)numericUpDownMpcPort.Value;
                    Settings.Default.MpcFork = ((comboBoxMpcFork.SelectedValue as MpcFork?) ?? MpcFork.Auto).ToString();
                    Settings.Default.MpcFullscreen = checkBoxMpcFullscreen.Checked;
                    Settings.Default.MpcExtraArgs = textBoxMpcArgs.Text;
                    Settings.Default.Save();

                    connectionToolStripMenuItem.Text = $"Connected to {Settings.Default.JellyfinHost}";
                    connectionToolStripMenuItem.Visible = true;
                    _isConnecting = false;
                    UpdateAuthStatusLabel();
                    UpdateTrayStatusIcon();

                    if (!string.IsNullOrWhiteSpace(Settings.Default.MpcPath) && File.Exists(Settings.Default.MpcPath))
                    {
                        await StartMpcClient();
                        UpdateTrayStatusIcon();
                        HideForm();
                    }

                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            UpdateAuthStatusLabel();
        }
        catch (Exception ex)
        {
            UpdateAuthStatusLabel();
            MessageBox.Show($"Quick Connect failed\r\r{ex.Message}", "Quick Connect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isConnecting = false;
            _quickConnectCts?.Dispose();
            _quickConnectCts = null;
            buttonQuickConnect.Text = "Quick Connect";
            UpdateTrayStatusIcon();
        }
    }

    private async void buttonSignOut_Click(object? sender, EventArgs e)
    {
        _quickConnectCts?.Cancel();
        _isConnecting = false;
        await _jellyfinClient.Stop(logout: true, notifyHandlers: false);
        Settings.Default.JellyfinToken = string.Empty;
        Settings.Default.Save();
        connectionToolStripMenuItem.Visible = false;
        UpdateAuthStatusLabel();
        UpdateTrayStatusIcon();
    }

    private void logsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        path = Path.Combine(path, "JellyfinMPCShim.Tray", "logs");
        Process.Start("explorer.exe", path);
    }

    private async void trayMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        groupsToolStripMenuItem.DropDownItems.Clear();
        if (!_jellyfinClient.IsConnected)
        {
            return;
        }

        var groups = await _jellyfinClient.SyncPlayGetGroups();
        if (groups == null)
        {
            return;
        }
        foreach (var group in groups)
        {
            var item = new ToolStripMenuItem
            {
                Text = group.GroupName,
                Tag = group,
                Checked = group.GroupId == _currentGroup
            };
            item.Click += SyncPlayGroutItemOnClick;
            groupsToolStripMenuItem.DropDownItems.Add(item);
        }
    }

    private void SyncPlayGroutItemOnClick(object? sender, EventArgs e)
    {
        if (_currentGroup != null)
        {
            _jellyfinClient.SyncPlayLeaveGroup();
        }

        if (sender is ToolStripMenuItem item && item.Tag is GroupInfoDto group)
        {
            if (!item.Checked && group.GroupId != null)
            {
                _jellyfinClient.SyncPlayJoinGroup(group.GroupId.Value);
            }
            item.Checked = !item.Checked;
        }
    }

    public Task HandlePlay(JellyfinWebsockeMessage<PlayRequest> message)
    {
        return Task.CompletedTask;
    }

    public Task HandlePlayState(JellyfinWebsockeMessage<PlaystateRequest> message)
    {
        return Task.CompletedTask;
    }

    public Task HandleSyncGroupUpdate(JellyfinWebsockeMessage<GroupUpdate> syncPlayGroupUpdateMessage)
    {
        switch (syncPlayGroupUpdateMessage.Data.Type)
        {
            case GroupUpdateType.UserJoined:
                break;
            case GroupUpdateType.UserLeft:
                break;
            case GroupUpdateType.GroupJoined:
                _currentGroup = syncPlayGroupUpdateMessage.Data.GroupId;
                foreach (ToolStripMenuItem item in groupsToolStripMenuItem.DropDownItems)
                {
                    if (item.Tag is GroupInfoDto group)
                    {
                        item.Checked = group.GroupId == _currentGroup;
                    }
                }
                break;
            case GroupUpdateType.GroupLeft:
            case GroupUpdateType.NotInGroup:
            case GroupUpdateType.GroupDoesNotExist:
                _currentGroup = null;
                foreach (ToolStripMenuItem item in groupsToolStripMenuItem.DropDownItems)
                {
                    item.Checked = false;
                }
                break;
            case GroupUpdateType.StateUpdate:
                break;
            case GroupUpdateType.PlayQueue:
                break;
            case GroupUpdateType.CreateGroupDenied:
                break;
            case GroupUpdateType.JoinGroupDenied:
                break;
            case GroupUpdateType.LibraryAccessDenied:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return Task.CompletedTask;
    }

    public Task HandleSyncPlayCommand(JellyfinWebsockeMessage<SendCommand> syncPlayCommandMessage)
    {
        return Task.CompletedTask;
    }

    public Task HandleStop()
    {
        //Shutdown application when Jellyfin connection is stopped
        _hostlLifetime.StopApplication();
        return Task.CompletedTask;
    }

    private void buttonSelectMpc_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.CheckFileExists = true;
        ofd.FileName = textBoxMpcPath.Text;
        ofd.InitialDirectory = !string.IsNullOrWhiteSpace(textBoxMpcPath.Text) ? Path.GetDirectoryName(textBoxMpcPath.Text) : null;
        ofd.Filter = "exe|*.exe";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            textBoxMpcPath.Text = ofd.FileName;
        }
    }

    private sealed record MpcForkItem(MpcFork Fork, string Label);
}
