using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class MiscEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/seasonal-backgrounds", () => Results.Content(JsonConvert.SerializeObject(
			new APISeasonalBackgrounds
			{
				EndDate = DateTimeOffset.Parse("2037-12-01T00:00:00Z"),
				Backgrounds =
				[
					new APISeasonalBackground { Url = $"{Server.Hostname}/test1.jpg" }
				]
			}
		), "application/json"));
	}
}