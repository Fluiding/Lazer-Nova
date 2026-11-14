using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.Rooms;

namespace LazerNova.Endpoints;

public static class BeatmapEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/beatmapsets/{beatmapSetId:int}",
			async (int beatmapSetId, HttpRequest request) =>
			{
				var cache = Server.Database.GetCollection<CachedBeatmapSet>("beatmapsets");

				var hit = cache.FindById(beatmapSetId);
				if (hit != null)
					return Results.Content(hit.Json, "application/json");

				var json = await ExternalService.ProxyRequest(request);
				var cachedBeatmap = new CachedBeatmapSet
				{
					Id = beatmapSetId,
					Json = json
				};

				cache.Insert(cachedBeatmap);
				return Results.Content(json, "application/json");
			});

		Server.App.MapGet("/api/v2/beatmapsets/search", async (HttpRequest request) =>
		{
			var json = await ExternalService.ProxyRequest(request);
			return Results.Content(json, "application/json");
		});

		Server.App.MapGet("/api/v2/beatmaps/{beatmapId}/scores", async (HttpRequest request) =>
		{
			var json = await ExternalService.ProxyRequest(request);
			return Results.Content(json, "application/json");
		});

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", async () =>
		{
			BeatmapService.ActiveScores.Sort();
			var id = BeatmapService.ActiveScores.LastOrDefault() + 1;
			BeatmapService.ActiveScores.Add(id);

			return Results.Content(JsonConvert.SerializeObject(
				new APIScoreToken { ID = id }
			), "application/json");
		});

		Server.App.MapPut("/api/v2/beatmaps/{beatmapId:int}/solo/scores/{scoreId}",
			async (int beatmapId, int scoreId, HttpRequest request)
				=> await BeatmapService.SubmitScore(beatmapId, scoreId, request));
	}
}