using System.Reflection;
using LiteDB;
using osu.Game.Online.API.Requests.Responses;

namespace LazerNova.Mappers;

public static class DBMapper
{
	private static readonly HashSet<string> IncludedUserFields =
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

	private static void ResolveMember(Type type, MemberInfo member, MemberMapper mapped)
	{
		if (type == typeof(APIUser) && !IncludedUserFields.Contains(member.Name))
			mapped.FieldName = null;

		if (typeof(APIBeatmap).IsAssignableFrom(type) &&
		    member.Name
			    is nameof(APIBeatmap.Metadata)
			    or nameof(APIBeatmap.Difficulty)
			    or nameof(APIBeatmap.Hash))
			mapped.FieldName = null;
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