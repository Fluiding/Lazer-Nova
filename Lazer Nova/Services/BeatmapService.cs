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

		var beatmapSets = Server.Database.GetCollection<APIBeatmapSet>("beatmapsets");
		var apiBeatmapSet = beatmapSets.FindById(beatmap.BeatmapInfo.BeatmapSet!.OnlineID);
		var pp = await CalculatePP(score, beatmap);

		// Populate SoloScoreInfo
		score.ID = user.Scores.Count > 0 ? user.Scores.Max(x => x.ID) + 1 : 0;
		score.Processed = true;
		score.Preserve = true;
		score.EndedAt = now;
		score.CreatedAt = now;
		score.BeatmapID = beatmapId;
		score.RulesetID = ruleset.RulesetInfo.OnlineID;
		score.UserID = user.Id;
		score.Beatmap = apiBeatmap;
		score.BeatmapSet = apiBeatmapSet;
		score.User = user;
		score.PP = pp;

		var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();
		var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);
		UserService.UpdateUserAfterScore(score, scoreInfo, beatmap, apiBeatmap, now, request);

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