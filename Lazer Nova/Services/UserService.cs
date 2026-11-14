using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using osu.Game.Users;

// ReSharper disable UseCollectionExpression

namespace LazerNova.Services;

public static class UserService
{
	public static async Task<User?> SearchUser(HttpRequest request, string id, string? key)
	{
		var users = Server.Database.GetCollection<User>("users");
		var user = key != "username"
			? users.FindById(int.Parse(id))
			: users.FindOne(x => x.Username == id);

		if (user != null)
			return user;

		var json = await ExternalService.ProxyRequest(request);
		return JsonConvert.DeserializeObject<User>(json);
	}

	public static User? GetUserFromRequest(HttpRequest request)
	{
		var token = AuthService.GetToken(request);
		var users = Server.Database.GetCollection<User>("users");
		var existing = users.FindOne(x => x.AccessToken == token);

		return existing;
	}

	public static async Task<IResult> GetUserScores(HttpRequest request,
		int userId, string scoresType, int? offset, int? limit)
	{
		var users = Server.Database.GetCollection<User>("users");
		var user = users.FindById(userId);

		if (user == null)
		{
			var json = await ExternalService.ProxyRequest(request);
			return Results.Content(json, "application/json");
		}

		var scores = scoresType switch
		{
			"best" => user.BestScores,
			"firsts" => user.FirstScores,
			"recent" => user.RecentScores,
			"pinned" => user.PinnedScores,
			_ => []
		};
		scores = Utilities.GetPaginatedList(scores, offset, limit);

		return Results.Content(
			JsonConvert.SerializeObject(scores),
			"application/json"
		);
	}

	public static async Task<IResult> GetUserBeatmaps(HttpRequest request,
		int userId, string beatmapType, int? offset, int? limit)
	{
		var users = Server.Database.GetCollection<User>("users");
		var user = users.FindById(userId);

		if (user == null)
		{
			var json = await ExternalService.ProxyRequest(request);
			return Results.Content(json, "application/json");
		}

		if (beatmapType == "most_played")
			return Results.Content(user.MostPlayedBeatmapsJson, "application/json");

		var beatmapSets = beatmapType switch
		{
			"favourite" => user.FavoriteBeatmaps,
			"ranked" => user.RankedBeatmaps,
			"loved" => user.LovedBeatmaps,
			"pending" => user.PendingBeatmaps,
			"guest" => user.GuestBeatmaps,
			"graveyard" => user.GraveyardBeatmaps,
			"nominated" => user.NominatedBeatmaps,
			_ => []
		};
		beatmapSets = Utilities.GetPaginatedList(beatmapSets, offset, limit);

		return Results.Content(
			JsonConvert.SerializeObject(beatmapSets),
			"application/json"
		);
	}

	public static void UpdateUserAfterScore(
		SoloScoreInfo score,
		ScoreInfo scoreInfo,
		Beatmap beatmap,
		APIBeatmap apiBeatmap,
		HttpRequest request)
	{
		var token = AuthService.GetToken(request);
		var users = Server.Database.GetCollection<User>("users");
		var user = users.FindOne(x => x.AccessToken == token);

		user.Statistics.PlayCount++;
		user.Statistics.PlayTime += (int)Math.Round(beatmap.BeatmapInfo.Length / 1000 / 60);
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

		user.BeatmapPlayCountsCount += 0;
		user.Statistics.PP += 0;
		user.Statistics.GlobalRank += 0;
		user.Statistics.GlobalRankPercent += 0;
		user.Statistics.CountryRank += 0;
		user.Statistics.IsRanked = true;
		user.RulesetsStatistics["osu"] = user.Statistics;

		var mostPlayedContainer = JsonConvert.DeserializeObject
			<List<APIUserMostPlayedBeatmap>>(user.MostPlayedBeatmapsJson)!;

		var mostPlayed = mostPlayedContainer
			.FirstOrDefault(x => x.BeatmapID == score.BeatmapID);

		if (mostPlayed == null)
		{
			var beatmapSets = Server.Database.GetCollection<APIBeatmapSet>("beatmapsets");
			var beatmapSet = beatmapSets.FindById(beatmap.BeatmapInfo.BeatmapSet!.OnlineID);

			var json = JsonConvert.SerializeObject(new
			{
				beatmap_id = beatmap.BeatmapInfo.OnlineID,
				count = 1,
				beatmap = apiBeatmap,
				beatmapset = beatmapSet
			});

			mostPlayedContainer.Add(
				JsonConvert.DeserializeObject<APIUserMostPlayedBeatmap>(json)!
			);

			user.MostPlayedBeatmapsJson =
				JsonConvert.SerializeObject(mostPlayedContainer);
		}
		else
		{
			mostPlayed.PlayCount++;
		}

		var month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

		var monthlyPlayCount = user.MonthlyPlayCounts
			.FirstOrDefault(x => x.Date == month);

		if (monthlyPlayCount == null)
			_ = user.MonthlyPlayCounts.Append(new APIUserHistoryCount
			{
				Date = month,
				Count = 1
			});
		else
			monthlyPlayCount.Count++;

		user.BestScores.Add(score);
		user.RecentScores.Add(score);
		if (scoreInfo.Position == 1)
			user.FirstScores.Add(score);

		user.BestScores = user.BestScores
			.OrderByDescending(x => x.PP)
			.Take(100)
			.ToList();

		user.RecentScores = user.RecentScores
			.OrderByDescending(x => x.EndedAt)
			.Take(50)
			.ToList();

		user.ScoresBestCount = user.BestScores.Count;
		user.ScoresRecentCount = user.RecentScores.Count;
		user.ScoresFirstCount = user.FirstScores.Count;

		users.Update(user);
	}

	public static User GetNewUser(string username)
	{
		var users = Server.Database.GetCollection<User>("users");
		return new()
		{
			Id = users.Count() == 0 ? 2 : Math.Max(2, users.Max() + 1),
			JoinDate = DateTimeOffset.UtcNow,
			Username = username,
			PreviousUsernames = [],
			RankHighest = null,

			CountryCode = CountryCode.AQ,
			Colour = "99EB47",
			AvatarUrl = $"{Server.Hostname}/images.jpg",

			IsAdmin = false,
			IsSupporter = false,
			SupportLevel = 0,
			IsGMT = false,
			IsQAT = false,
			IsBNG = false,
			IsBot = false,
			Active = true,
			WasRecentlyOnline = true,

			PMFriendsOnly = false,

			Interests = "",
			Occupation = "",
			Title = "",
			Location = "",

			Twitter = "",
			Discord = "",
			Website = "",

			LastVisit = null,

			PostCount = 0,
			CommentsCount = 0,
			FollowerCount = 0,
			MappingFollowerCount = 0,

			FavouriteBeatmapsetCount = 0,
			GraveyardBeatmapsetCount = 0,
			LovedBeatmapsetCount = 0,
			RankedBeatmapsetCount = 0,
			PendingBeatmapsetCount = 0,
			GuestBeatmapsetCount = 0,
			NominatedBeatmapsetCount = 0,

			ScoresBestCount = 0,
			ScoresFirstCount = 0,
			ScoresRecentCount = 0,
			ScoresPinnedCount = 0,
			BeatmapPlayCountsCount = 0,

			PlayStyles = [],
			PlayMode = "osu",
			ProfileHue = null,

			ProfileOrder =
			[
				"me",
				"recent_activity",
				"top_ranks",
				"medals",
				"historical",
				"beatmaps",
				"kudosu"
			],

			Kudosu = new APIUser.KudosuCount
			{
				Total = 0,
				Available = 0
			},

			Statistics = new UserStatistics
			{
				IsRanked = false,
				GlobalRank = null,
				GlobalRankPercent = null,
				CountryRank = null,
				PP = null,

				RankedScore = 0,
				Accuracy = 0,
				PlayCount = 0,
				PlayTime = 0,
				TotalScore = 0,
				TotalHits = 0,
				MaxCombo = 0,
				ReplaysWatched = 0,

				GradesCount = new UserStatistics.Grades
				{
					SSPlus = 0,
					SS = 0,
					SPlus = 0,
					S = 0,
					A = 0
				},

				Variants = []
			},

			Rank = null,

			TournamentBanners = [],
			Badges = [],
			Achievements = [],
			MonthlyPlayCounts = [],
			ReplaysWatchedCounts = [],

			RulesetsStatistics = new Dictionary<string, UserStatistics>
			{
				["osu"] = new()
			},

			Groups = [],
			DailyChallengeStatistics = new(),
			MatchmakingStatistics = []
		};
	}
}