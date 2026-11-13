using Microsoft.AspNetCore.SignalR;
using osu.Game.Online.Spectator;

namespace LazerNova.Hubs;

public class SpectatorHub : Hub
{
	public Task BeginPlaySessionV2(long? scoreToken, SpectatorState state)
	{
		Console.WriteLine(scoreToken + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
		return Task.CompletedTask;
	}

	public Task SendFrameDataV2(long? scoreToken, FrameDataBundle data)
	{
		return Task.CompletedTask;
	}

	public Task EndPlaySessionV2(long? scoreToken, SpectatedUserState finalState)
	{
		return Task.CompletedTask;
	}

	public Task StartWatchingUser(int userId)
	{
		return Task.CompletedTask;
	}

	public Task EndWatchingUser(int userId)
	{
		return Task.CompletedTask;
	}
}