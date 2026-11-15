using System.Text;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
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

		string beatmapContent;
		string apiBeatmapJson;
		var cache = Server.Database.GetCollection<CachedBeatmap>("beatmaps");
		var hit = cache.FindById(beatmapId);

		if (hit != null)
		{
			beatmapContent = hit.Json;
			apiBeatmapJson = hit.APIJson;
		}
		else
		{
			using var response1 = await Server.Http.GetAsync($"https://osu.direct/api/osu/{beatmapId}/raw");
			using var response2 =
				await ExternalService.GetAsync($"https://osu.ppy.sh/api/v2/beatmaps/{beatmapId}");

			beatmapContent = await response1.Content.ReadAsStringAsync();
			apiBeatmapJson = await response2.Content.ReadAsStringAsync();

			cache.Insert(new CachedBeatmap
			{
				Id = beatmapId,
				Json = beatmapContent,
				APIJson = apiBeatmapJson
			});
		}

		using var requestReader = new StreamReader(request.Body);
		var body = await requestReader.ReadToEndAsync();
		var score = JsonConvert.DeserializeObject<SoloScoreInfo>(body)!;

		var beatmap = await DecodeBeatmap(beatmapContent);
		var ruleset = beatmap.BeatmapInfo.Ruleset.CreateInstance();
		var pp = await CalculatePP(score, beatmap);

		score.EndedAt = now;
		score.PP = pp;
		score.BeatmapID = beatmapId;
		score.RulesetID = ruleset.RulesetInfo.OnlineID;
		score.UserID = user.Id;

		var apiBeatmap = JsonConvert.DeserializeObject<APIBeatmap>(apiBeatmapJson)!;
		var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();
		var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);
		UserService.UpdateUserAfterScore(score, scoreInfo, beatmap, apiBeatmap, request);

		var scoreResponse = JsonConvert.DeserializeObject<MultiplayerScore>(body)!;
		scoreResponse.ID = scoreId;
		scoreResponse.User = user;
		scoreResponse.PP = pp;
		scoreResponse.BeatmapId = beatmapId;
		scoreResponse.EndedAt = now;
		scoreResponse.Position = 1; // temp

		return Results.Content(
			JsonConvert.SerializeObject(scoreResponse),
			"application/json"
		);
	}
}