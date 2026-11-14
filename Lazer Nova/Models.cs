using osu.Game.Online.API.Requests.Responses;

namespace LazerNova;

public class User : APIUser
{
	public string Email { get; set; } = "";
	public string PasswordHash { get; set; } = "";
	public string AccessToken { get; set; } = "";
	public string RefreshToken { get; set; } = "";

	public List<APIRelation> Relations { get; set; } = [];
	public List<APIKudosuHistory> KudosuHistory { get; set; } = [];
	public List<APIBeatmapSet> FavoriteBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> RankedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> LovedBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> PendingBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GuestBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> GraveyardBeatmaps { get; set; } = [];
	public List<APIBeatmapSet> NominatedBeatmaps { get; set; } = [];
	public string MostPlayedBeatmapsJson { get; set; } = "[]";
}

public class CachedBeatmapSet
{
	public int Id { get; init; }
	public string Json { get; init; } = "";
}

public class CachedBeatmap
{
	public int Id { get; init; }
	public string Json { get; init; } = "";
	public string APIJson { get; init; } = "";
}