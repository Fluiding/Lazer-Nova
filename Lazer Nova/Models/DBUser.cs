using osu.Game.Online.API.Requests.Responses;

namespace LazerNova;

public class DBUser : APIUser
{
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }

	public List<APIRelation> Relations { get; set; }
}