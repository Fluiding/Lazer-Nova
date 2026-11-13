using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class NotificationEndpoints
{
	public static void Register()
	{
		Server.App.Map("/notifications", NotificationService.HandleWebsocket);

		Server.App.MapGet("/api/v2/notifications", () => Results.Content(JsonConvert.SerializeObject(
			new APINotificationsBundle
			{
				HasMore = false,
				Notifications = [],
				Endpoint = $"{Server.Hostname.Replace("https", "wss")}/notifications"
			}
		), "application/json"));
	}
}