using LazerNova.Mappers;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
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

	public static async Task<IResult> GetUserRequest(string id, string? key = "id")
	{
		var user = await SearchUser(id, key);

		var settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore
		};

		return Results.Content(
			JsonConvert.SerializeObject(user, settings),
			"application/json"
		);
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

	private static void UpdateUserPlaycounts(
		User user,
		SoloScoreInfo score,
		Beatmap beatmap,
		APIBeatmap apiBeatmap)
	{
		var mostPlayedContainer = JsonConvert.DeserializeObject
			<List<APIUserMostPlayedBeatmap>>(user.MostPlayedBeatmapsJson)!;

		var mostPlayed = mostPlayedContainer
			.FirstOrDefault(x => x.BeatmapID == score.BeatmapID);

		if (mostPlayed == null)
		{
			var json = JsonConvert.SerializeObject(new
			{
				beatmap_id = beatmap.BeatmapInfo.OnlineID,
				count = 1,
				beatmap = apiBeatmap,
				beatmapset = apiBeatmap.BeatmapSet
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

		user.ScoresRecentCount = user.RecentScores.Count;
	}

	private static void UpdateRecentActivities(SoloScoreInfo score,
		ScoreInfo scoreInfo,
		Ruleset ruleset,
		Beatmap beatmap,
		User user,
		DateTimeOffset now)
	{
		if (scoreInfo.Position >= 1000)
			return;

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
			Mode = ruleset.ShortName,
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

	private static void UpdateUserRankHistory(User user, int rank)
	{
		var rankHistory = user.RankHistoryData
			.FirstOrDefault(x => x.Date == DateTime.UtcNow.Date);

		if (rankHistory == null)
		{
			user.RankHistoryData.Add(new RankHistory
			{
				Date = DateTime.UtcNow.Date,
				Rank = rank
			});

			user.RankHistory.Data = user.RankHistory.Data
				.Append(rank).TakeLast(90).ToArray();
		}
		else
		{
			user.RankHistory.Data[-1] = rank;
		}
	}

	private static double GetRequiredScore(int level)
	{
		return level <= 100
			? 5000.0 / 3.0 *
			(4 * Math.Pow(level, 3) - 3 * Math.Pow(level, 2) - level) +
			1.25 * Math.Pow(1.8, level - 60)
			: 26931190827.0 + 99999999999.0 * (level - 100);
	}

	private static UserStatistics.LevelInfo CalculateProfileLevel(APIUser user)
	{
		var level = 1;
		while (user.Statistics.TotalScore >= GetRequiredScore(level + 1))
			level++;

		var current = GetRequiredScore(level);
		var next = GetRequiredScore(level + 1);

		var progress = (int)Math.Floor(
			(user.Statistics.TotalScore - current) / (next - current) * 100
		);

		return new UserStatistics.LevelInfo
		{
			Current = level,
			Progress = Math.Clamp(progress, 0, 99)
		};
	}

	private static double CalculateUserAccuracy(User user)
	{
		double weightedAccuracy = 0;
		double totalWeight = 0;

		for (var i = 0; i < user.BestScores.Count; i++)
		{
			var weight = Math.Pow(0.95, i);
			weightedAccuracy += user.BestScores[i].Accuracy * weight;
			totalWeight += weight;
		}

		return weightedAccuracy / totalWeight * 100;
	}

	private static void AddGradeToUser(ScoreRank rank, APIUser user, int amount = 1)
	{
		if (rank == ScoreRank.XH)
			user.Statistics.GradesCount.SSPlus += amount;
		else if (rank == ScoreRank.X)
			user.Statistics.GradesCount.SS += amount;
		else if (rank == ScoreRank.SH)
			user.Statistics.GradesCount.SPlus += amount;
		else if (rank == ScoreRank.S)
			user.Statistics.GradesCount.S += amount;
		else if (rank == ScoreRank.A)
			user.Statistics.GradesCount.A += amount;
	}

	public static async Task<User> UpdateUserAfterScore(
		SoloScoreInfo score,
		Beatmap beatmap,
		Ruleset ruleset,
		APIBeatmap apiBeatmap,
		DateTimeOffset now,
		HttpRequest request)
	{
		var mods = score.Mods.Select(apiMod => apiMod.ToMod(ruleset)).ToArray();
		var scoreInfo = score.ToScoreInfo(mods, beatmap.BeatmapInfo);

		var token = AuthService.GetToken(request);
		var users = Server.Database.GetCollection<User>("users");
		var user = users.FindOne(x => x.AccessToken == token);

		var existingScore = user.Scores
			.FirstOrDefault(x => x.BeatmapID == score.BeatmapID);

		user.Statistics.PlayCount++;
		user.Statistics.PlayTime += (int)(apiBeatmap.Length / 1000 / 60);
		user.Statistics.TotalScore += score.TotalScore;
		user.BeatmapPlayCountsCount += existingScore == null ? 1 : 0;

		user.Statistics.TotalHits += score.Statistics
			.Where(x => x.Key.IsHit())
			.Sum(x => x.Value);

		UpdateUserPlaycounts(user, score, beatmap, apiBeatmap);

		if (!score.Passed)
		{
			user.RulesetsStatistics["osu"] = user.Statistics;
			users.Update(user);
			return user;
		}

		if (existingScore != null && existingScore.TotalScore < score.TotalScore)
		{
			AddGradeToUser(existingScore.Rank, user, -1);
			user.Scores.Remove(existingScore);
		}

		if (scoreInfo.Position == 1)
			user.FirstScores.Add(score);

		user.ScoresBestCount = user.BestScores.Count;
		user.ScoresFirstCount = user.FirstScores.Count;

		user.Statistics.MaxCombo = Math.Max(user.Statistics.MaxCombo, score.MaxCombo);
		user.Statistics.RankedScore += apiBeatmap.Status == BeatmapOnlineStatus.Ranked
			? score.TotalScore : 0;

		var settings = new JsonSerializerSettings
		{
			ContractResolver = new SoloScoreInfoMapper()
		};

		var serializedScoreJson = JsonConvert.SerializeObject(score, settings);
		var serializedScore = JsonConvert.DeserializeObject<SoloScoreInfo>(serializedScoreJson)!;
		user.Scores.Add(serializedScore);

		var pp = CalculateProfilePP(user);
		var rank = await ExternalService.GetRankFromPP(pp);

		user.Statistics.PP = (decimal)pp;
		user.Statistics.GlobalRank = rank;
		user.Statistics.GlobalRankPercent += 0;
		user.Statistics.CountryRank = 1; // temp
		user.Statistics.IsRanked = true;
		user.Statistics.Level = CalculateProfileLevel(user);
		user.Statistics.Accuracy = CalculateUserAccuracy(user);

		AddGradeToUser(score.Rank, user);
		UpdateUserRankHistory(user, rank);
		UpdateRecentActivities(score, scoreInfo, ruleset, beatmap, user, now);

		user.RulesetsStatistics["osu"] = user.Statistics;
		users.Update(user);
		return user;
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
			IsSupporter = true,
			SupportLevel = 3,
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
				RankHistory = new APIRankHistory
				{
					Mode = "osu",
					Data = new int[90]
				}
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