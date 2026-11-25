using Flurl;
using Jellyfin.Sdk.Generated.Models;
using JellyfinMPCShim.Models;

namespace JellyfinMPCShim.Interfaces;

public interface IJellyfinClient
{
    string? AccessToken { get; }
    Task Start(string host, string username, string password, CancellationToken stoppingToken = default);
    Task StartWithToken(string host, string accessToken, CancellationToken stoppingToken = default);
    Task<bool> IsQuickConnectEnabled(string host, CancellationToken cancellationToken = default);
    Task<QuickConnectSession> InitiateQuickConnect(string host, CancellationToken cancellationToken = default);
    Task<QuickConnectSession> CheckQuickConnect(string host, string secret, CancellationToken cancellationToken = default);
    Task StartWithQuickConnect(string host, string secret, CancellationToken stoppingToken = default);
    Task Stop(bool logout = false, bool notifyHandlers = true);
    Task<PlaybackInfoResponse?> GetPlaybackInfo(Guid itemId);
    Url GetPlaybackUrl(Guid itemId, MediaSourceInfo mediaSource);
    Task ReportPlaybackStopped(PlaybackStopInfo playbackStopInfo);
    Task ReportPlaybackStart(PlaybackStartInfo playbackStartInfo);
    Task ReportPlaybackProgress(PlaybackProgressInfo playbackProgessInfo);
    void AddMessageHandler(IJellyfinMessageHandler messageHandler);
    Task<List<GroupInfoDto>?> SyncPlayGetGroups();
    bool IsConnected { get; }
    Task SyncPlayJoinGroup(Guid groupId);
    Task SyncPlayLeaveGroup();
    void RemoveMessageHandlen(IJellyfinMessageHandler messageHandler);
    Task SyncPlayBuffering(bool isPlaying, Guid playlistItemId, long positionTicks);
    Task SyncPlayReady(bool isPlaying, Guid playlistItemId, long positionTicks);
    Task SyncPlayPause();
    Task SyncPlayUnpause();
    Task SyncPlayStop();
}
