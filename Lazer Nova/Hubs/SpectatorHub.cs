using Microsoft.AspNetCore.SignalR;

namespace lazer_echo.Hubs;

public class SpectatorHub : Hub
{
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