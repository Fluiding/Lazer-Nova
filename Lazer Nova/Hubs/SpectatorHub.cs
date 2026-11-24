using Microsoft.AspNetCore.SignalR;
using osu.Game.Online.Spectator;

namespace LazerNova.Hubs;

public class SpectatorHub : Hub<ISpectatorClient>
{
	public override Task OnConnectedAsync()
	{
		Console.WriteLine($"Connected user: {Context.UserIdentifier}");
		return base.OnConnectedAsync();
	}

	public static async Task NotifyScoreProcessed(
		IHubContext<SpectatorHub, ISpectatorClient> hub,
		int userId,
		long scoreId)
	{
		await hub.Clients.User(userId.ToString())
			.UserScoreProcessed(userId, scoreId);
	}

	public Task BeginPlaySessionV2(long? scoreToken, SpectatorState state)
	{
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