using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;

namespace LazerNova.Services;

public static class UserService
{
	public static DBUser? SearchUser(string id, string? key)
	{
		var users = Server.Database.GetCollection<DBUser>();
		var existing = users.FindOne(x => key == "id" ? x.Id == int.Parse(id) : x.Username == id);
		return existing ?? null;
	}

	public static DBUser? GetCurrentUser(HttpRequest request)
	{
		var token = AuthService.GetToken(request);
		var users = Server.Database.GetCollection<DBUser>();
		var existing = users.FindOne(x => x.AccessToken == token);

		return existing;
	}

	public static DBUser GetNewUser()
	{
		return new DBUser
		{
			Id = 42,
			JoinDate = DateTimeOffset.UtcNow,
			Username = "mrekk",
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
			WasRecentlyOnline = false,

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