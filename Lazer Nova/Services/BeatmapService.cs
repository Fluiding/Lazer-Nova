using System.Text;
using LazerNova.Mappers;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace LazerNova.Services;

public static class BeatmapService
{
	public static readonly List<int> ActiveScores = [];

	private static Task<Beatmap> DecodeBeatmap(string osuBeatmapContent)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(osuBeatmapContent));
		using var osuReader = new LineBufferedReader(stream, true);
		var beatmap = Decoder.GetDecoder<Beatmap>(osuReader).Decode(osuReader);
		return Task.FromResult(beatmap);
	}

	private static async Task<double> CalculatePP(SoloScoreInfo score, Beatmap beatmap)
	{
		var workingBeatmap = new FlatWorkingBeatmap(beatmap);
		var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();

		var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();
		var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);

		var difficultyAttributes = ruleset
			.CreateDifficultyCalculator(workingBeatmap)
			.Calculate(mods);

		var perfCalculator = ruleset.CreatePerformanceCalculator();
		if (perfCalculator == null)
			throw new Exception("No performance calculator.");

		var perfAttributes = await perfCalculator.CalculateAsync(
			scoreInfo, difficultyAttributes,
			CancellationToken.None
		);

		return perfAttributes.Total;
	}

	public static async Task<IResult> GetBeatmapLeaderboard(HttpRequest request, int beatmapId)
	{
		var query = request.Query.ToDictionary(
			x => x.Key,
			x => x.Value.ToString()
		);

		query["limit"] = "100";
		var queryString = QueryString.Create(query!).ToString();

		var path = $"{request.PathBase}{request.Path}{queryString}";
		using var response = await ExternalService.GetAsync($"https://osu.ppy.sh{path}");

		if (!response.IsSuccessStatusCode)
		{
			var error = await response.Content.ReadAsStringAsync();
			Console.WriteLine(error);

			return Results.Content(error,
				"application/json",
				statusCode: (int)response.StatusCode
			);
		}

		var json = await response.Content.ReadAsStringAsync();
		var scores = JsonConvert.DeserializeObject<APIScoresCollection>(json)!;

		var user = UserService.GetUserFromRequest(request)!;
		var score = user.Scores.FirstOrDefault(x => x.BeatmapID == beatmapId);

		if (score != null)
		{
			var position = scores.Scores.FindIndex(x => x.TotalScore < score.TotalScore);

			if (position != -1)
			{
				scores.Scores.Insert(position, score);
				scores.Scores.RemoveAt(scores.Scores.Count - 1);
			}
			else
			{
				var bestScore = scores.Scores[0].TotalScore;
				position = (int)Math.Max(101, score.TotalScore /
					bestScore * scores.ScoresCount);
			}

			scores.UserScore = new APIScoreWithPosition
			{
				Position = position,
				Score = score
			};
		}

		var settings = new JsonSerializerSettings
		{
			ContractResolver = new SoloScoreInfoMapper()
		};

		var result = JsonConvert.SerializeObject(scores, settings);
		return Results.Content(result, "application/json");
	}

	public static async Task<IResult> SubmitScore(int beatmapId, int scoreId, HttpRequest request)
	{
		var user = UserService.GetUserFromRequest(request)!;
		var now = DateTimeOffset.UtcNow;
		ActiveScores.Remove(scoreId);

		CachedBeatmap apiBeatmap;
		var cache = Server.Database.GetCollection<CachedBeatmap>("beatmaps");
		var hit = cache.FindById(beatmapId);

		if (hit != null)
		{
			apiBeatmap = hit;
		}
		else
		{
			using var response1 =
				await Server.Http.GetAsync($"https://osu.direct/api/osu/{beatmapId}/raw");

			using var response2 =
				await ExternalService.GetAsync($"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}");

			var apiBeatmapJson = await response2.Content.ReadAsStringAsync();
			apiBeatmap = JsonConvert.DeserializeObject<CachedBeatmap>(apiBeatmapJson)!;
			apiBeatmap.Content = await response1.Content.ReadAsStringAsync();

			cache.Insert(apiBeatmap);
		}

		using var requestReader = new StreamReader(request.Body);
		var body = await requestReader.ReadToEndAsync();
		var score = JsonConvert.DeserializeObject<SoloScoreInfo>(body)!;

		var beatmap = await DecodeBeatmap(apiBeatmap.Content);
		var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();
		var pp = await CalculatePP(score, beatmap);

		// Populate SoloScoreInfo
		score.ID = user.Scores.Count > 0 ? user.Scores.Max(x => x.ID) + 1 : 0;
		score.Ranked = apiBeatmap.Status == BeatmapOnlineStatus.Ranked;
		score.Preserve = score.Passed;
		score.Processed = true;
		score.EndedAt = now;
		score.CreatedAt = now;
		score.BeatmapID = beatmapId;
		score.RulesetID = ruleset.RulesetInfo.OnlineID;
		score.UserID = user.Id;
		score.Beatmap = apiBeatmap;
		score.PP = pp;

		score.User = await UserService.UpdateUserAfterScore(
			score, beatmap, ruleset, apiBeatmap, now, request);

		var settings = new JsonSerializerSettings
		{
			ContractResolver = new SoloScoreInfoMapper()
		};

		return Results.Content(
			JsonConvert.SerializeObject(score, settings),
			"application/json"
		);
	}
}