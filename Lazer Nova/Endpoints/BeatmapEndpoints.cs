using Newtonsoft.Json;
using osu.Game.Online.Rooms;

namespace LazerNova.Endpoints;

public static class BeatmapEndpoints
{
	public static void Register()
	{
		Server.App.MapPost("/api/v2/beatmapsets/{beatmapId}", () => { return Results.InternalServerError(); });

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", () =>
		{
			return Results.Content(JsonConvert.SerializeObject(
				new APIScoreToken
				{
					ID = 69
				}
			), "application/json");
		});

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores/{scoreId}", () =>
		{
			// var calculator = new OsuPerformanceCalculator();
			// var attributes = await calculator.CalculateAsync(score, );
			return Results.InternalServerError();
		});
	}
}