using LazerNova.Mappers;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Scoring;
using osu.Game.Users;

// ReSharper disable UseCollectionExpression

namespace LazerNova.Services;

public static class UserService
{
	public static async Task<User?> SearchUser(string id, string? key = "id")
	{
		var users = Server.Database.GetCollection<User>("users");
		var user = key != "username"
			? users.FindById(int.Parse(id))
			: users.FindOne(x => x.Username == id);

		if (user != null)
			return user;

		var url = $"https://osu.ppy.sh/api/v2/users/{id}/?key={key}";
		using var response = await ExternalService.GetAsync(url);

		var json = await response.Content.ReadAsStringAsync();
		return JsonConvert.DeserializeObject<User>(json);
	}

	public static bool IsUserInternal(User user)
	{
		return Server.Database.GetCollection<User>("users")
			.Exists(x => x.Id == user.Id);
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
		var settings = new JsonSerializerSettings
		{
			ContractResolver = new SoloScoreInfoMapper()
		};

		var a = JsonConvert.SerializeObject(scores, settings);
		return Results.Content(
			JsonConvert.SerializeObject(scores, settings),
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
		{
			var mostPlayedBeatmaps = JsonConvert.DeserializeObject
				<List<APIUserMostPlayedBeatmap>>(user.MostPlayedBeatmapsJson)!;

			mostPlayedBeatmaps = Utilities.GetPaginatedList(mostPlayedBeatmaps, offset, limit);

			return Results.Content(
				JsonConvert.SerializeObject(mostPlayedBeatmaps),
				"application/json"
			);
		}

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

	private static double CalculateProfilePP(User user)
	{
		var weightedPP = user.Scores
			.Where(x => x.PP.HasValue)
			.OrderByDescending(x => x.PP!.Value)
			.Take(1000)
			.Select((x, i) => x.PP!.Value * Math.Pow(0.95, i))
			.Sum();

		var exponent = Math.Min(user.BeatmapPlayCountsCount, 1000);
		var bonusPP = 416.6667 * (1 - Math.Pow(0.995, exponent));

		return weightedPP + bonusPP;
	}

	private static void UpdateUserBeatmaps(
		User user,
		SoloScoreInfo score,
		ScoreInfo scoreInfo,
		Beatmap beatmap,
		APIBeatmap apiBeatmap)
	{
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

		if (scoreInfo.Position == 1)
			user.FirstScores.Add(score);

		user.ScoresBestCount = user.BestScores.Count;
		user.ScoresRecentCount = user.RecentScores.Count;
		user.ScoresFirstCount = user.FirstScores.Count;
	}

	private static void UpdateRecentActivities(SoloScoreInfo score,
		ScoreInfo scoreInfo,
		Beatmap beatmap,
		User user,
		DateTimeOffset now)
	{
		var recentActivityID = user.RecentActivities.Count > 0
			? user.RecentActivities.Max(x => x.ID)
			: 0;

		user.RecentActivities.Add(new APIRecentActivity
		{
			ID = recentActivityID,
			CreatedAt = now,
			Type = RecentActivityType.Rank,
			ScoreRank = score.Rank,
			Rank = scoreInfo.Position ?? 1,
			Mode = scoreInfo.Ruleset.ShortName,
			Beatmap = new APIRecentActivity.RecentActivityBeatmap
			{
				Title = beatmap.BeatmapInfo.GetDisplayTitle(),
				Url = $"/beatmaps/{beatmap.BeatmapInfo.OnlineID}"
			},
			User = new APIRecentActivity.RecentActivityUser
			{
				Username = user.Username,
				Url = $"/users/{user.Id}"
			}
		});
	}

	public static void UpdateUserAfterScore(
		SoloScoreInfo score,
		ScoreInfo scoreInfo,
		Beatmap beatmap,
		APIBeatmap apiBeatmap,
		DateTimeOffset now,
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

		var existingScore = user.Scores
			.FirstOrDefault(x => x.BeatmapID == score.BeatmapID);

		if (existingScore != null)
			user.Scores.Remove(existingScore);

		var settings = new JsonSerializerSettings
		{
			ContractResolver = new SoloScoreInfoMapper()
		};

		var serializedScore = JsonConvert.DeserializeObject<SoloScoreInfo>(
			JsonConvert.SerializeObject(score, settings))!;

		user.Scores.Add(serializedScore);

		user.BeatmapPlayCountsCount += existingScore == null ? 1 : 0;
		user.Statistics.PP = (decimal)CalculateProfilePP(user);
		user.Statistics.GlobalRank = 1; // temp
		user.Statistics.GlobalRankPercent += 0;
		user.Statistics.CountryRank = 1; // temp
		user.Statistics.IsRanked = true;
		user.RulesetsStatistics["osu"] = user.Statistics;

		UpdateRecentActivities(score, scoreInfo, beatmap, user, now);
		UpdateUserBeatmaps(user, score, scoreInfo, beatmap, apiBeatmap);

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

				Variants = [],
				RankHistory = new()
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