using Microsoft.AspNetCore.SignalR;
using osu.Game.Online.Metadata;

// ReSharper disable UnusedMember.Global

namespace lazer_echo.Hubs;

public class MetadataHub : Hub
{
	public Task<BeatmapUpdates> GetChangesSince(int queueId)
	{
		return Task.FromResult<BeatmapUpdates>(null!);
	}

	public Task UpdateActivity(object? activity)
	{
		return Task.CompletedTask;
	}

	public Task UpdateStatus(object? status)
	{
		return Task.CompletedTask;
	}

	public Task BeginWatchingUserPresence()
	{
		return Task.CompletedTask;
	}

	public Task EndWatchingUserPresence()
	{
		return Task.CompletedTask;
	}

	public Task RefreshFriends()
	{
		return Task.CompletedTask;
	}

	// Only for multiplayer
	// public Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(long id)
	// {
	// 	return null;
	// }
	//
	// public Task EndWatchingMultiplayerRoom(long id)
	// {
	// 	return Task.CompletedTask;
	// }
}