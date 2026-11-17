using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;

namespace LazerNova.Endpoints;

public static class BeatmapEndpoints
{
	public static void Register()
	{
		Server.App.MapGet("/api/v2/beatmapsets/{beatmapSetId:int}",
			async (int beatmapSetId, HttpRequest request) =>
			{
				var cache = Server.Database.GetCollection<APIBeatmapSet>("beatmapsets");
				var hit = cache.FindById(beatmapSetId);

				if (hit != null)
					return Results.Content(
						JsonConvert.SerializeObject(hit),
						"application/json"
					);

				var json = await ExternalService.ProxyRequest(request);
				var beatmap = JsonConvert.DeserializeObject<APIBeatmapSet>(json)!;

				cache.Insert(beatmap);
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

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", () =>
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

		Server.App.MapGet("/api/v2/beatmapsets/lookup", async (HttpRequest request, int? beatmap_id) =>
		{
			if (beatmap_id == null)
				return Results.BadRequest();

			var beatmaps = Server.Database.GetCollection<CachedBeatmap>("beatmaps");
			var beatmap = beatmaps.FindById(beatmap_id);

			if (beatmap == null)
				return Results.Content(
					await ExternalService.ProxyRequest(request),
					"application/json"
				);

			return Results.Content(
				JsonConvert.SerializeObject(beatmap.BeatmapSet),
				"application/json"
			);
		});

		Server.App.MapGet("/api/v2/comments", async (HttpRequest request)
			=> await ExternalService.ProxyRequest(request));
	}
}