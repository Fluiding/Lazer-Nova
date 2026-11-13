using System.Text;
using LazerNova.Services;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Scoring;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace LazerNova.Endpoints;

public static class BeatmapEndpoints
{
	public static List<int> ActiveScores = [];

	public static void Register()
	{
		Server.App.MapGet("/api/v2/beatmapsets/{beatmapSetId:int}", async (int beatmapSetId) =>
		{
			var cache = Server.Database.GetCollection<CachedBeatmapSet>("beatmapsets");

			var hit = cache.FindById(beatmapSetId);
			if (hit != null)
				return Results.Content(hit.Json, "application/json");

			var response = await ExternalService.GetAsync($"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapSetId}");
			var json = await response.Content.ReadAsStringAsync();
			var cachedBeatmap = new CachedBeatmapSet
			{
				Id = beatmapSetId,
				Json = json
			};

			cache.Insert(cachedBeatmap);
			return Results.Content(json, "application/json");
		});

		Server.App.MapPost("/api/v2/beatmaps/{beatmapId}/solo/scores", () =>
		{
			ActiveScores.Sort();
			var id = ActiveScores.LastOrDefault() + 1;
			ActiveScores.Add(id);

			return Results.Content(JsonConvert.SerializeObject(
				new APIScoreToken { ID = id }
			), "application/json");
		});

		Server.App.MapPut("/api/v2/beatmaps/{beatmapId:int}/solo/scores/{scoreId}",
			async (int beatmapId, int scoreId, HttpRequest request) =>
			{
				var cache = Server.Database.GetCollection<Beatmap>("beatmaps");
				var hit = cache.FindById(beatmapId);
				if (hit != null)
					return Results.Content(
						JsonConvert.SerializeObject(hit),
						"application/json"
					);

				var osuContent = await ExternalService.DownloadOsuBeatmap(beatmapId);
				using var stream = new MemoryStream(Encoding.UTF8.GetBytes(osuContent));
				using var osuReader = new LineBufferedReader(stream, true);

				var beatmap = Decoder.GetDecoder<Beatmap>(osuReader).Decode(osuReader);
				var workingBeatmap = new FlatWorkingBeatmap(beatmap);
				var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();

				using var requestReader = new StreamReader(request.Body);
				var body = await requestReader.ReadToEndAsync();

				var score = JsonConvert.DeserializeObject<SoloScoreInfo>(body)!;
				var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();

				var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);
				var user = UserService.GetUserFromRequest(request);

				var difficultyAttributes = ruleset
					.CreateDifficultyCalculator(workingBeatmap)
					.Calculate(mods);

				var performanceCalculator = ruleset.CreatePerformanceCalculator();
				if (performanceCalculator == null)
					return Results.BadRequest("No performance calculator.");

				var performanceAttributes = await performanceCalculator.CalculateAsync(
					scoreInfo,
					difficultyAttributes,
					request.HttpContext.RequestAborted
				);

				var multiplayerScore = JsonConvert.DeserializeObject<MultiplayerScore>(body)!;
				multiplayerScore.ID = scoreId;
				multiplayerScore.User = user;
				multiplayerScore.PP = performanceAttributes.Total;
				multiplayerScore.BeatmapId = beatmapId;
				multiplayerScore.EndedAt = DateTimeOffset.UtcNow;

				return Results.Content(
					JsonConvert.SerializeObject(multiplayerScore),
					"application/json"
				);
			});
	}
}