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
				Beatmap beatmap;
				var cache = Server.Database.GetCollection<Beatmap>("beatmaps");
				var hit = cache.FindById(beatmapId);

				if (hit != null)
					beatmap = hit;
				else
				{
					var osuContent = await ExternalService.DownloadOsuBeatmap(beatmapId);
					using var stream = new MemoryStream(Encoding.UTF8.GetBytes(osuContent));
					using var osuReader = new LineBufferedReader(stream, true);

					beatmap = Decoder.GetDecoder<Beatmap>(osuReader).Decode(osuReader);
					cache.Insert(beatmap);
				}

				var workingBeatmap = new FlatWorkingBeatmap(beatmap);
				var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();

				using var requestReader = new StreamReader(request.Body);
				var body = await requestReader.ReadToEndAsync();

				var score = JsonConvert.DeserializeObject<SoloScoreInfo>(body)!;
				var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();

				var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);

				var difficultyAttributes = ruleset
					.CreateDifficultyCalculator(workingBeatmap)
					.Calculate(mods);

				var perfCalculator = ruleset.CreatePerformanceCalculator();
				if (perfCalculator == null)
					throw new Exception("No performance calculator.");

				var perfAttributes = await perfCalculator.CalculateAsync(
					scoreInfo,
					difficultyAttributes,
					request.HttpContext.RequestAborted
				);

				var user = UserService.GetUserFromRequest(request)!;
				user.Statistics.PlayCount++;
				user.Statistics.PlayTime += (int)Math.Round(beatmap.BeatmapInfo.Length);
				user.Statistics.TotalScore += score.TotalScore;
				user.Statistics.RankedScore += score.Ranked ? score.TotalScore : 0;
				user.Statistics.TotalHits += scoreInfo.HitEvents.Count;
				user.Statistics.MaxCombo = Math.Max(user.Statistics.MaxCombo, score.MaxCombo);

				switch (score.Rank)
				{
					case ScoreRank.XH:
						user.Statistics.GradesCount.SSPlus++;
						break;
					case ScoreRank.X:
						user.Statistics.GradesCount.SS++;
						break;
					case ScoreRank.SH:
						user.Statistics.GradesCount.SPlus++;
						break;
					case ScoreRank.S:
						user.Statistics.GradesCount.S++;
						break;
					case ScoreRank.A:
						user.Statistics.GradesCount.A++;
						break;
				}

				// todo: add scores table to db
				user.BeatmapPlayCountsCount += 0;
				user.Statistics.PP += 0;
				user.Statistics.GlobalRank += 0;
				user.Statistics.GlobalRankPercent += 0;
				user.Statistics.CountryRank += 0;
				user.Statistics.IsRanked = true;
				user.RulesetsStatistics["osu"] = user.Statistics;
				// etc

				var scoreResponse = JsonConvert.DeserializeObject<MultiplayerScore>(body)!;
				scoreResponse.ID = scoreId;
				scoreResponse.User = user;
				scoreResponse.PP = perfAttributes.Total;
				scoreResponse.BeatmapId = beatmapId;
				scoreResponse.EndedAt = DateTimeOffset.UtcNow;


				return Results.Content(
					JsonConvert.SerializeObject(scoreResponse),
					"application/json"
				);
			});
	}
}