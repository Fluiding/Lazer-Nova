using osu.Game.Online.API.Requests.Responses;

namespace LazerNova;

public class User : APIUser
{
	public string Email { get; set; } = "";
	public string PasswordHash { get; set; } = "";
	public string AccessToken { get; set; } = "";
	public string RefreshToken { get; set; } = "";
	public string MostPlayedBeatmapsJson { get; set; } = "[]";

	public List<APIRelation> Relations { get; set; } = [];
	public List<APIKudosuHistory> KudosuHistory { get; set; } = [];
	public List<APIBeatmapSet> FavoriteBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> RankedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> LovedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> PendingBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GuestBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GraveyardBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> NominatedBeatmaps { get; set; } = [];

	public List<SoloScoreInfo> Scores { get; set; } = [];
	public List<SoloScoreInfo> FirstScores { get; set; } = [];
	public List<SoloScoreInfo> PinnedScores { get; set; } = [];

	public List<SoloScoreInfo> BestScores
		=> [.. Scores.OrderByDescending(x => x.PP).Take(100)];

	public List<SoloScoreInfo> RecentScores
		=> [.. Scores.OrderByDescending(x => x.EndedAt).Take(100)];

	public List<APIRecentActivity> RecentActivities { get; set; } = [];
}

public class CachedBeatmap : APIBeatmap
{
	public string Content { get; set; } = "";
}