using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Flurl;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using JellyfinMPCShim.Interfaces;
using JellyfinMPCShim.Models;
using JellyfinMPCShim.Models.WebsocketData;
using Microsoft.Extensions.Logging;
using Websocket.Client;

namespace JellyfinMPCShim;

internal class JellyfinClient : IJellyfinClient
{
    private readonly ILogger<JellyfinClient> _logger;
    private readonly JellyfinSdkSettings _jellyfinSdkSettings;
    private readonly JellyfinClientIdentity _identity;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private WebsocketClient? _wsc;
    private CancellationTokenSource? _keepAliveCts;
    private int _timeout = 60;
    private readonly List<IJellyfinMessageHandler> _messageHandlers = new();
    private string? _host;
    private CancellationTokenSource? _cts;
    private bool _isConnected;

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonGuidConverter(), new JsonNullableGuidConverter() }
    };

    private readonly Channel<ResponseMessage> _queue;

    public JellyfinClient(
        ILogger<JellyfinClient> logger,
        JellyfinSdkSettings jellyfinSdkSettings,
        JellyfinClientIdentity identity,
        JellyfinApiClient jellyfinApiClient,
        IHttpClientFactory httpClientFactory,
        string httpClientName = "Default")
    {
        _logger = logger;
        _jellyfinSdkSettings = jellyfinSdkSettings;
        _identity = identity;
        _jellyfinApiClient = jellyfinApiClient;
        _httpClientFactory = httpClientFactory;
        _httpClientName = httpClientName;
        var options = new BoundedChannelOptions(25) { FullMode = BoundedChannelFullMode.Wait };
        _queue = Channel.CreateBounded<ResponseMessage>(options);
    }

    public string? AccessToken => _jellyfinSdkSettings.AccessToken;

    public bool IsConnected => _isConnected;

    public Task Start(string host, string username, string password, CancellationToken stoppingToken = default)
    {
        return StartInternal(host, async ct =>
        {
            var authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(
                new AuthenticateUserByName { Username = username, Pw = password }, cancellationToken: ct);
            if (authenticationResult == null || string.IsNullOrEmpty(authenticationResult.AccessToken))
            {
                throw new InvalidOperationException("Authentication failed: server returned an empty access token.");
            }

            _jellyfinSdkSettings.SetAccessToken(authenticationResult.AccessToken);
            _logger.LogDebug("Logged in as {name} (ID {id})", authenticationResult.User?.Name, authenticationResult.User?.Id);
        }, stoppingToken);
    }

    public Task StartWithToken(string host, string accessToken, CancellationToken stoppingToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));
        }

        return StartInternal(host, async ct =>
        {
            _jellyfinSdkSettings.SetAccessToken(accessToken);
            using var request = CreateAuthorizedRequest(HttpMethod.Get, new Url(_host).AppendPathSegment("Users").AppendPathSegment("Me"));
            var httpClient = _httpClientFactory.CreateClient(_httpClientName);
            using var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Authenticated using saved access token.");
        }, stoppingToken);
    }

    public Task StartWithQuickConnect(string host, string secret, CancellationToken stoppingToken = default)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("Quick Connect secret cannot be empty.", nameof(secret));
        }

        return StartInternal(host, async ct =>
        {
            var url = new Url(_host).AppendPathSegment("Users").AppendPathSegment("AuthenticateWithQuickConnect");
            using var request = CreateAuthorizedRequest(HttpMethod.Post, url);
            request.Content = JsonContent.Create(new { Secret = secret });

            var httpClient = _httpClientFactory.CreateClient(_httpClientName);
            using var response = await httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<QuickConnectAuthResponse>(s_serializerOptions, ct);
            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken))
            {
                throw new InvalidOperationException("Quick Connect authentication failed: server returned an empty access token.");
            }

            _jellyfinSdkSettings.SetAccessToken(authResponse.AccessToken);
            _logger.LogDebug("Logged in via Quick Connect as {name} (ID {id})", authResponse.User?.Name, authResponse.User?.Id);
        }, stoppingToken);
    }

    public async Task<bool> IsQuickConnectEnabled(string host, CancellationToken cancellationToken = default)
    {
        var url = new Url(NormalizeHost(host)).AppendPathSegment("QuickConnect").AppendPathSegment("Enabled");
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url);
        var httpClient = _httpClientFactory.CreateClient(_httpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        return await response.Content.ReadFromJsonAsync<bool>(s_serializerOptions, cancellationToken);
    }

    public async Task<QuickConnectSession> InitiateQuickConnect(string host, CancellationToken cancellationToken = default)
    {
        var url = new Url(NormalizeHost(host)).AppendPathSegment("QuickConnect").AppendPathSegment("Initiate");
        using var request = CreateAuthorizedRequest(HttpMethod.Post, url);
        var httpClient = _httpClientFactory.CreateClient(_httpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QuickConnectSession>(s_serializerOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to parse Quick Connect initiation response.");
    }

    public async Task<QuickConnectSession> CheckQuickConnect(string host, string secret, CancellationToken cancellationToken = default)
    {
        var url = new Url(NormalizeHost(host))
            .AppendPathSegment("QuickConnect")
            .AppendPathSegment("Connect")
            .SetQueryParam("secret", secret);
        using var request = CreateAuthorizedRequest(HttpMethod.Get, url);
        var httpClient = _httpClientFactory.CreateClient(_httpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<QuickConnectSession>(s_serializerOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to parse Quick Connect status response.");
    }

    private async Task StartInternal(string host, Func<CancellationToken, Task> authenticateAsync, CancellationToken stoppingToken)
    {
        if (_isConnected)
        {
            throw new InvalidOperationException("JellyfinClient already started");
        }

        _host = NormalizeHost(host);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        try
        {
            _jellyfinSdkSettings.SetServerUrl(_host);
            _jellyfinSdkSettings.SetAccessToken(null);
            _logger.LogDebug("Connecting to {host}", _jellyfinSdkSettings.ServerUrl);

            var systemInfo = await _jellyfinApiClient.System.Info.Public.GetAsync(cancellationToken: _cts.Token);
            _logger.LogDebug("Connected to {host} {name} {version}", _jellyfinSdkSettings.ServerUrl, systemInfo?.ServerName,
                systemInfo?.Version);

            await authenticateAsync(_cts.Token);

            await _jellyfinApiClient.Sessions.Capabilities.Full.PostAsync(new ClientCapabilitiesDto
            {
                PlayableMediaTypes = new List<MediaType?> { MediaType.Video },
                SupportedCommands = new List<GeneralCommandType?>
                {
                    GeneralCommandType.MoveUp,
                    GeneralCommandType.MoveDown,
                    GeneralCommandType.MoveLeft,
                    GeneralCommandType.MoveRight,
                    GeneralCommandType.Select,
                    GeneralCommandType.Back,
                    GeneralCommandType.ToggleFullscreen,
                    GeneralCommandType.GoHome,
                    GeneralCommandType.GoToSettings,
                    GeneralCommandType.TakeScreenshot,
                    GeneralCommandType.VolumeUp,
                    GeneralCommandType.VolumeDown,
                    GeneralCommandType.ToggleMute,
                    GeneralCommandType.SetAudioStreamIndex,
                    GeneralCommandType.SetSubtitleStreamIndex,
                    GeneralCommandType.Mute,
                    GeneralCommandType.Unmute,
                    GeneralCommandType.SetVolume,
                    GeneralCommandType.DisplayContent,
                    GeneralCommandType.Play,
                    GeneralCommandType.PlayState,
                    GeneralCommandType.PlayNext,
                    GeneralCommandType.PlayMediaSource
                },
                SupportsMediaControl = true
            }, cancellationToken: _cts.Token);

            var uri = new Url(_host);
            uri.Scheme = uri.Scheme == "https" ? "wss" : "ws";
            uri.AppendPathSegment("socket").SetQueryParams(new
            {
                ApiKey = _jellyfinSdkSettings.AccessToken,
                deviceId = _identity.DeviceId
            });

            _logger.LogDebug("Starting websocket client {uri}", uri);
            var authHeader = ModernJellyfinAuthenticationProvider.BuildAuthorizationHeader(_jellyfinSdkSettings, _identity);
            _wsc = new WebsocketClient(uri.ToUri(), () =>
            {
                var client = new ClientWebSocket();
                client.Options.SetRequestHeader("Authorization", authHeader);
                return client;
            });

            _wsc.MessageReceived.Subscribe(info =>
            {
                _logger.LogDebug("Websocket message received {text}", info.Text);
                _queue.Writer.WriteAsync(info, _cts.Token);
            });
            _wsc.DisconnectionHappened.Subscribe(info =>
            {
                _logger.LogWarning("Websocket disconnection happened {type} {exception}", info.Type, info.Exception);
                if (IsConnected && info.Type != DisconnectionType.Exit)
                {
                    _ = Stop();
                }
            });

            _ = Task.Run(async () =>
            {
                await HandleWebsockeMessages(_cts.Token);
            }, _cts.Token);

            _isConnected = true;
            await _wsc.Start();
        }
        catch
        {
            _isConnected = false;
            _wsc?.Dispose();
            _wsc = null;
            _jellyfinSdkSettings.SetAccessToken(null);
            if (_cts != null)
            {
                await _cts.CancelAsync();
                _cts.Dispose();
                _cts = null;
            }

            throw;
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, Url url)
    {
        var request = new HttpRequestMessage(method, url.ToUri());
        request.Headers.TryAddWithoutValidation("Authorization",
            ModernJellyfinAuthenticationProvider.BuildAuthorizationHeader(_jellyfinSdkSettings, _identity));
        return request;
    }

    private static string NormalizeHost(string host) => host.Trim().TrimEnd('/');

    private async Task HandleWebsockeMessages(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var info = await _queue.Reader.ReadAsync(cancellationToken);
            try
            {
                await HandleWebsockeMessage(info, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error handling websocket message {type}, {text}", info.MessageType, info.Text);
            }
        }
    }

    public async Task Stop(bool logout = false, bool notifyHandlers = true)
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_keepAliveCts != null)
        {
            await _keepAliveCts.CancelAsync();
            _keepAliveCts.Dispose();
            _keepAliveCts = null;
        }

        _wsc?.Dispose();
        _wsc = null;

        try
        {
            if (logout && _isConnected && !string.IsNullOrEmpty(_jellyfinSdkSettings.AccessToken))
            {
                await _jellyfinApiClient.Sessions.Logout.PostAsync();
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error while logging out of Jellyfin session");
        }
        finally
        {
            _isConnected = false;
            _jellyfinSdkSettings.SetAccessToken(null);
            if (notifyHandlers)
            {
                foreach (var handler in _messageHandlers)
                {
                    try
                    {
                        await handler.HandleStop();
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "{messageHandler} HandleStop failed", handler);
                    }
                }
            }
        }
    }

    public Task<BaseItemDto?> GetItem(Guid itemId)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Session not started");
        }

        return _jellyfinApiClient.Items[itemId].GetAsync();
    }

    public Task CloseTranscode(string playSessionId)
    {
        throw new NotImplementedException();
        //TODO return _hlsSegmentClient.StopEncodingProcessAsync(_sdkClientSettings.DeviceId, playSessionId);
    }

    public Task<PlaybackInfoResponse?> GetPlaybackInfo(Guid itemId)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Session not started");
        }

        return _jellyfinApiClient.Items[itemId].PlaybackInfo.GetAsync();
    }

    public Url GetPlaybackUrl(Guid itemId, MediaSourceInfo mediaSource)
    {
        var url = new Url(_host);
        if (mediaSource.SupportsDirectStream ?? false)
        {
            return url
                .AppendPathSegment("Videos")
                .AppendPathSegment(itemId)
                .AppendPathSegment("stream")
                .SetQueryParams(new
                {
                    @static = true,
                    MediaSourceId = mediaSource.Id,
                    DeviceId = _identity.DeviceId,
                    ApiKey = _jellyfinSdkSettings.AccessToken
                });
        }

        throw new Exception("MediaSource doesn't support direct stream");
        //var transcodeUrl = Url.Parse(mediaSource.TranscodingUrl);
        //return url.AppendPathSegments(transcodeUrl.PathSegments).SetQueryParams(transcodeUrl.QueryParams);
    }

    public Task ReportPlaybackStopped(PlaybackStopInfo playbackStopInfo)
    {
        return _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(playbackStopInfo);
    }

    public Task ReportPlaybackStart(PlaybackStartInfo playbackStartInfo)
    {
        return _jellyfinApiClient.Sessions.Playing.PostAsync(playbackStartInfo);
    }

    public Task ReportPlaybackProgress(PlaybackProgressInfo playbackProgessInfo)
    {
        return _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(playbackProgessInfo);
    }

    public void AddMessageHandler(IJellyfinMessageHandler messageHandler)
    {
        _messageHandlers.Add(messageHandler);
    }

    public void RemoveMessageHandlen(IJellyfinMessageHandler messageHandler)
    {
        _messageHandlers.Remove(messageHandler);
    }

    public Task<List<GroupInfoDto>?> SyncPlayGetGroups()
    {
        return _jellyfinApiClient.SyncPlay.List.GetAsync();
    }

    public Task SyncPlayJoinGroup(Guid groupId)
    {
        return _jellyfinApiClient.SyncPlay.Join.PostAsync(new JoinGroupRequestDto { GroupId = groupId });
    }

    public Task SyncPlayLeaveGroup()
    {
        return _jellyfinApiClient.SyncPlay.Leave.PostAsync();
    }

    public Task SyncPlayBuffering(bool isPlaying, Guid playlistItemId, long positionTicks)
    {
        return _jellyfinApiClient.SyncPlay.Buffering.PostAsync(new BufferRequestDto
        {
            IsPlaying = isPlaying,
            PlaylistItemId = playlistItemId,
            PositionTicks = positionTicks,
            When = DateTimeOffset.Now
        });
    }

    public Task SyncPlayReady(bool isPlaying, Guid playlistItemId, long positionTicks)
    {
        return _jellyfinApiClient.SyncPlay.Ready.PostAsync(new ReadyRequestDto
        {
            IsPlaying = isPlaying,
            PlaylistItemId = playlistItemId,
            PositionTicks = positionTicks,
            When = DateTimeOffset.Now
        });
    }

    public Task SyncPlayPause()
    {
        return _jellyfinApiClient.SyncPlay.Pause.PostAsync();
    }

    public Task SyncPlayUnpause()
    {
        return _jellyfinApiClient.SyncPlay.Unpause.PostAsync();
    }

    public Task SyncPlayStop()
    {
        return _jellyfinApiClient.SyncPlay.Stop.PostAsync();
    }

    private async Task HandleWebsockeMessage(ResponseMessage info, CancellationToken cancellationToken)
    {
        if (info.MessageType != WebSocketMessageType.Text)
        {
            return;
        }

        var msg = JsonSerializer.Deserialize<JellyfinWebsockeMessage>(info.Text, s_serializerOptions);
        if (msg == null)
        {
            return;
        }

        _logger.LogDebug("Message {MessageId} Type {MessageType}", msg.MessageId, msg.MessageType);

        switch (msg.MessageType)
        {
            case SessionMessageType.ForceKeepAlive:
                var fkaMsg = JsonSerializer.Deserialize<JellyfinWebsockeMessage<int>>(info.Text, s_serializerOptions)!;
                _timeout = fkaMsg.Data;
                if (_keepAliveCts != null)
                {
                    await _keepAliveCts.CancelAsync();
                    _keepAliveCts.Dispose();
                }

                _keepAliveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _ = KeepAlive(_keepAliveCts.Token);
                return;
            case SessionMessageType.KeepAlive:
                return;
            case SessionMessageType.Play:
                var playMessage =
                    JsonSerializer.Deserialize<JellyfinWebsockeMessage<PlayRequest>>(info.Text, s_serializerOptions)!;
                foreach (var handler in _messageHandlers)
                {
                    try
                    {
                        await handler.HandlePlay(playMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "{messageHandler} HandlePlay failed", handler);
                    }
                }

                break;
            case SessionMessageType.Playstate:
                var playStateMessage = JsonSerializer.Deserialize<JellyfinWebsockeMessage<PlaystateRequest>>(info.Text,
                    s_serializerOptions)!;
                foreach (var handler in _messageHandlers)
                {
                    try
                    {
                        await handler.HandlePlayState(playStateMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "{messageHandler} HandlePlayState failed", handler);
                    }
                }

                break;
            case SessionMessageType.SyncPlayGroupUpdate:
                var syncPlayGroupUpdateMessage =
                    JsonSerializer.Deserialize<JellyfinWebsockeMessage<GroupUpdate>>(info.Text, s_serializerOptions)!;
                foreach (var handler in _messageHandlers)
                {
                    try
                    {
                        await handler.HandleSyncGroupUpdate(syncPlayGroupUpdateMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "{messageHandler} HandleSyncGroupUpdate failed", handler);
                    }
                }

                break;

            case SessionMessageType.SyncPlayCommand:
                var syncPlayCommandMessage =
                    JsonSerializer.Deserialize<JellyfinWebsockeMessage<SendCommand>>(info.Text, s_serializerOptions)!;
                foreach (var handler in _messageHandlers)
                {
                    try
                    {
                        await handler.HandleSyncPlayCommand(syncPlayCommandMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "{messageHandler} HandleSyncPlayCommand failed", handler);
                    }
                }

                break;
            default:
                _logger.LogDebug("Unhandled websocket message {type}, {text}", msg.MessageType, info.Text);
                break;
        }
    }

    public async Task KeepAlive(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            await Task.Delay(new TimeSpan(0, 0, _timeout / 2), stopToken);
            if (!stopToken.IsCancellationRequested && _wsc != null)
            {
                SendWebsockeMessage(new JellyfinWebsockeMessage { MessageType = SessionMessageType.KeepAlive });
            }
        }
    }

    private void SendWebsockeMessage(JellyfinWebsockeMessage message)
    {
        _wsc?.Send(JsonSerializer.Serialize(message));
    }

    private sealed class QuickConnectAuthResponse
    {
        public string? AccessToken { get; set; }
        public QuickConnectUser? User { get; set; }
    }

    private sealed class QuickConnectUser
    {
        public string? Name { get; set; }
        public Guid Id { get; set; }
    }
}
