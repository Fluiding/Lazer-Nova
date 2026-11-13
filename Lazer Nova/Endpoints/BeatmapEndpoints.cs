using LazerNova.Models;
using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.Rooms;

namespace LazerNova.Endpoints;

public static class BeatmapEndpoints
{
	private static List<int> ActiveScores { get; set; } = [];

	public static void Register()
	{
		Server.App.MapGet("/api/v2/beatmapsets/{beatmapId:int}", async (int beatmapId) =>
		{
			var cache = Server.Database.GetCollection<CachedBeatmapSet>("beatmapsets");

			var existing = cache.FindById(beatmapId);
			if (existing != null) return Results.Content(existing.Json, "application/json");

			var response = await ExternalService.GetAsync($"/api/v2/beatmapsets/{beatmapId}");
			var json = await response.Content.ReadAsStringAsync();
			var cachedBeatmap = new CachedBeatmapSet
			{
				Id = beatmapId,
				Json = json
			};

			cache.Insert(cachedBeatmap);
			return Results.Content(json, "application/json");
		});

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", () =>
		{
			BeatmapServices.ActiveScoreTokens.Sort();
			var id = BeatmapServices.ActiveScoreTokens.LastOrDefault() + 1;
			ActiveScores.Add(id);

			return Results.Content(JsonConvert.SerializeObject(
				new APIScoreToken
				{
					ID = id
				}
			), "application/json");
		});

		Server.App.MapPut("/api/v2/beatmaps/{beatmapId}/solo/scores/{scoreId}", () =>
		{
			// var calculator = new OsuPerformanceCalculator();
			// var attributes = await calculator.CalculateAsync(score, );
			return Results.InternalServerError();
		});
	}
}