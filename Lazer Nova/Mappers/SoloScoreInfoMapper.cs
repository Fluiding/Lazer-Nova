using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Mappers;

public class SoloScoreInfoMapper : DefaultContractResolver
{
	private static readonly HashSet<string>
		IncludedSoloScoreInfoFields =
		[
			nameof(SoloScoreInfo.ID),
			nameof(SoloScoreInfo.Rank),
			nameof(SoloScoreInfo.UserID),
			nameof(SoloScoreInfo.BeatmapID),
			nameof(SoloScoreInfo.RulesetID),
			nameof(SoloScoreInfo.BuildID),
			nameof(SoloScoreInfo.Accuracy),
			nameof(SoloScoreInfo.EndedAt),
			nameof(SoloScoreInfo.StartedAt),
			nameof(SoloScoreInfo.HasReplay),
			nameof(SoloScoreInfo.LegacyScoreId),
			nameof(SoloScoreInfo.LegacyTotalScore),
			nameof(SoloScoreInfo.MaxCombo),
			nameof(SoloScoreInfo.Passed),
			nameof(SoloScoreInfo.PP),
			nameof(SoloScoreInfo.TotalScore),
			nameof(SoloScoreInfo.Ranked),
			nameof(SoloScoreInfo.Preserve),
			nameof(SoloScoreInfo.Processed),
			nameof(SoloScoreInfo.MaximumStatistics),
			nameof(SoloScoreInfo.Mods),
			nameof(SoloScoreInfo.Statistics),
			nameof(SoloScoreInfo.User),
			nameof(SoloScoreInfo.Beatmap)
		],
		IncludedBeatmapFields =
		[
			nameof(APIBeatmap.OnlineID),
			nameof(APIBeatmap.OnlineBeatmapSetID),
			nameof(APIBeatmap.Status),
			nameof(APIBeatmap.Checksum),
			nameof(APIBeatmap.AuthorID),
			nameof(APIBeatmap.BeatmapSet),
			nameof(APIBeatmap.PlayCount),
			nameof(APIBeatmap.PassCount),
			nameof(APIBeatmap.RulesetID),
			nameof(APIBeatmap.StarRating),
			nameof(APIBeatmap.DrainRate),
			nameof(APIBeatmap.CircleSize),
			nameof(APIBeatmap.ApproachRate),
			nameof(APIBeatmap.OverallDifficulty),
			nameof(APIBeatmap.Length),
			nameof(APIBeatmap.HitLength),
			nameof(APIBeatmap.Convert),
			nameof(APIBeatmap.CircleCount),
			nameof(APIBeatmap.SliderCount),
			nameof(APIBeatmap.SpinnerCount),
			nameof(APIBeatmap.DifficultyName),
			nameof(APIBeatmap.LastUpdated),
			nameof(APIBeatmap.BPM)
		],
		IncludedBeatmapSetFields =
		[
			nameof(APIBeatmapSet.Covers),
			nameof(APIBeatmapSet.OnlineID),
			nameof(APIBeatmapSet.Status),
			nameof(APIBeatmapSet.Preview),
			nameof(APIBeatmapSet.PlayCount),
			nameof(APIBeatmapSet.FavouriteCount),
			nameof(APIBeatmapSet.HasExplicitContent),
			nameof(APIBeatmapSet.FeaturedInSpotlight),
			nameof(APIBeatmapSet.HasVideo),
			nameof(APIBeatmapSet.Title),
			nameof(APIBeatmapSet.TitleUnicode),
			nameof(APIBeatmapSet.Artist),
			nameof(APIBeatmapSet.ArtistUnicode),
			nameof(APIBeatmapSet.Author),
			nameof(APIBeatmapSet.AuthorID),
			nameof(APIBeatmapSet.AuthorString),
			nameof(APIBeatmapSet.Source),
			nameof(APIBeatmapSet.TrackId)
		],
		IncludedUserFields =
		[
			nameof(APIUser.Id),
			nameof(APIUser.AvatarUrl),
			nameof(APIUser.CountryCode),
			nameof(APIUser.Active),
			nameof(APIUser.IsBot),
			nameof(APIUser.WasRecentlyOnline),
			nameof(APIUser.IsSupporter),
			nameof(APIUser.LastVisit),
			nameof(APIUser.PMFriendsOnly),
			nameof(APIUser.Colour),
			nameof(APIUser.ProfileHue),
			nameof(APIUser.Username),
			nameof(APIUser.Team),
			nameof(APIUser.Cover),
			"country",
			"countryCodeString"
		];

	protected override JsonProperty CreateProperty(
		MemberInfo member,
		MemberSerialization memberSerialization)
	{
		var property = base.CreateProperty(member, memberSerialization);

		if (member.DeclaringType == typeof(SoloScoreInfo))
			property.ShouldSerialize =
				IncludedSoloScoreInfoFields.Contains(member.Name)
					? _ => true : _ => false;

		else if (member.DeclaringType == typeof(APIBeatmap))
			property.ShouldSerialize =
				IncludedBeatmapFields.Contains(member.Name)
					? _ => true : _ => false;

		else if (member.DeclaringType == typeof(APIBeatmapSet))
			property.ShouldSerialize =
				IncludedBeatmapSetFields.Contains(member.Name)
					? _ => true : _ => false;

		else if (member.DeclaringType == typeof(APIUser))
			property.ShouldSerialize =
				IncludedUserFields.Contains(member.Name)
					? _ => true : _ => false;

		return property;
	}
}