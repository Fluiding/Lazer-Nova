using Newtonsoft.Json;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;

namespace LazerNova.Endpoints;

public static class ChatEndpoints
{
	public static void Register()
	{
		Server.App.MapPost("/api/v2/chat/updates", () =>
		{
			return Results.Content(JsonConvert.SerializeObject(
				new GetUpdatesResponse
				{
					Presence = [],
					Messages = []
				}
			), "application/json");
		});

		Server.App.MapPost("/api/v2/chat/channels", () =>
		{
			return Results.Content(JsonConvert.SerializeObject(
				new List<Channel>()
			), "application/json");
		});

		Server.App.MapPost("/api/v2/chat/ack", () =>
		{
			return Results.Content(JsonConvert.SerializeObject(
				new ChatAckResponse()
			), "application/json");
		});
	}
}