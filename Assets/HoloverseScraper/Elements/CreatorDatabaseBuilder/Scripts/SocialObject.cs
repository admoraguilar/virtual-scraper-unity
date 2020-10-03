using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	[CreateAssetMenu(menuName = "Holoverse/Creator Database/Social Object")]
	public class SocialObject : ScriptableObject
	{
		public static implicit operator Social(SocialObject obj) => obj.ToSocial();

		public new string name;
		public Platform platform;
		public string id;
		public string url;
		public string avatarUrl;

		public string[] customKeywords = new string[0];

		public Social ToSocial()
		{
			return new Social {
				name = name,
				platform = platform,
				id = id,
				url = url,
				avatarUrl = avatarUrl,

				customKeywords = customKeywords
			};
		}
	}
}