using osu.Game.Online.API.Requests.Responses;

namespace LazerNova;

public class RankHistory
{
	public DateTime Date { get; set; }
	public int Rank { get; set; }
}

public class HitResultsCount
{
	public int Great { get; set; } = 0;
	public int Ok { get; set; } = 0;
	public int Meh { get; set; } = 0;
	public int Miss { get; set; } = 0;
}

public class User : APIUser
{
	public string Email { get; set; } = "";
	public string PasswordHash { get; set; } = "";
	public string AccessToken { get; set; } = "";
	public string RefreshToken { get; set; } = "";

	public HitResultsCount HitResultsCount { get; set; } = new();
	public string MostPlayedBeatmapsJson { get; set; } = "[]";

	public List<APIRelation> Relations { get; set; } = [];
	public List<APIKudosuHistory> KudosuHistory { get; set; } = [];

	public List<SoloScoreInfo> Scores { get; set; } = [];
	public List<SoloScoreInfo> FirstScores { get; set; } = [];
	public List<SoloScoreInfo> PinnedScores { get; set; } = [];

	public List<SoloScoreInfo> BestScores =>
	[
		.. Scores.OrderByDescending(x => x.PP).Take(100)
	];

	public List<SoloScoreInfo> RecentScores =>
	[
		.. Scores
			.Where(x => (DateTime.UtcNow - x.EndedAt).Hours < 24)
			.OrderByDescending(x => x.EndedAt)
	];

	public List<APIRecentActivity> RecentActivities { get; set; } = [];
	public List<RankHistory> RankHistory { get; set; } = [];

	public List<APIBeatmapSet> FavoriteBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> RankedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> LovedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> PendingBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GuestBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GraveyardBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> NominatedBeatmaps { get; set; } = [];
}

public class CachedBeatmap : APIBeatmap
{
	public string Content { get; set; } = "";
}