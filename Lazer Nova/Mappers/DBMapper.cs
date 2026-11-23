using System.Reflection;
using LiteDB;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Mappers;

public static class DBMapper
{
	private static readonly HashSet<string> IncludedAPIUserFields =
	[
		nameof(APIUser.AvatarUrl),
		nameof(APIUser.CountryCode),
		nameof(APIUser.Id),
		nameof(APIUser.Active),
		nameof(APIUser.IsBot),
		nameof(APIUser.WasRecentlyOnline),
		nameof(APIUser.IsSupporter),
		nameof(APIUser.LastVisit),
		nameof(APIUser.PMFriendsOnly),
		nameof(APIUser.Colour),
		nameof(APIUser.ProfileHue),
		nameof(APIUser.Username)
	];

	private static readonly HashSet<string> ExcludedAPIBeatmapFields =
	[
		nameof(APIBeatmap.Metadata),
		nameof(APIBeatmap.Difficulty),
		nameof(APIBeatmap.Hash)
	];

	private static void ResolveMember(Type type, MemberInfo member, MemberMapper mapped)
	{
		if ((type == typeof(APIUser) && !IncludedAPIUserFields.Contains(member.Name)) ||
		    (typeof(APIBeatmap).IsAssignableFrom(type) && ExcludedAPIBeatmapFields.Contains(member.Name)))
		{
			mapped.FieldName = null;
		}
	}

	public static BsonMapper GetBsonMapper()
	{
		var instance = new BsonMapper
		{
			IncludeFields = true,
			ResolveMember = ResolveMember
		};

		instance.Entity<APIBeatmapSet>().Id(x => x.OnlineID);
		instance.Entity<CachedBeatmap>().Id(x => x.OnlineID);

		return instance;
	}
}