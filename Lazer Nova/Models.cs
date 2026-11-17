using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

public class ScoreContractResolver : DefaultContractResolver
{
	protected override JsonProperty CreateProperty(
		MemberInfo member,
		MemberSerialization memberSerialization)
	{
		var property = base.CreateProperty(member, memberSerialization);

		if (member.DeclaringType != typeof(SoloScoreInfo))
			return property;

		property.ShouldSerialize = member.Name switch
		{
			nameof(SoloScoreInfo.ID) or nameof(SoloScoreInfo.User) or nameof(SoloScoreInfo.Beatmap)
				or nameof(SoloScoreInfo.PP) or nameof(SoloScoreInfo.HasReplay) or nameof(SoloScoreInfo.Preserve)
				or nameof(SoloScoreInfo.Processed) => _ => true,
			_ => property.ShouldSerialize
		};

		return property;
	}
}