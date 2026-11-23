using System.Globalization;
using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Endpoints;

public static class MiscEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/seasonal-backgrounds", ()
			=> Results.Content(
				JsonConvert.SerializeObject(new APISeasonalBackgrounds
				{
					EndDate = DateTimeOffset.Parse("2037-12-01T00:00:00Z", CultureInfo.InvariantCulture),
					Backgrounds =
					[
						new APISeasonalBackground { Url = $"{Server.Hostname}/test1.jpg" }
					]
				}),
				"application/json"
			));

		Server.App.MapGet("/api/v2/search", ExternalService.ProxyRequest);

		Server.App.MapGet("/api/v2/rankings/{**path}", ExternalService.ProxyRequest);

		Server.App.MapGet("/api/v2/news/{**path}", ExternalService.ProxyRequest);

		Server.App.MapGet("/api/v2/changelog/{**path}", ExternalService.ProxyRequest);

		Server.App.MapGet("/api/v2/wiki/{**path}", ExternalService.ProxyRequest);
	}
}