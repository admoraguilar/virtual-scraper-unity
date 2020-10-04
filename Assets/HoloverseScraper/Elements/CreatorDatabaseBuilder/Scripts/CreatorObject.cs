using System.Linq;
using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	[CreateAssetMenu(menuName = "Holoverse/Content Database/Creator Object")]
	public class CreatorObject : ScriptableObject
	{
		public static implicit operator Creator(CreatorObject obj) => obj.ToCreator();

		public string universalName = string.Empty;
		public string universalId = string.Empty;
		public string wikiUrl = string.Empty;
		public string avatarUrl = string.Empty;

		public bool isHidden = false;

		public CreatorObject[] affiliations = new CreatorObject[0];
		public bool isGroup = false;
		public int depth = 0;

		public Social[] socials = new Social[0];
		public string[] customKeywords = new string[0];

		public Creator ToCreator()
		{
			return new Creator {
				universalName = universalName,
				universalId = universalId,
				wikiUrl = wikiUrl,
				avatarUrl = avatarUrl,

				isHidden = isHidden,

				affiliations = affiliations.Select(a => a.ToCreator()).ToArray(),
				isGroup = isGroup,
				depth = depth,

				socials = socials,
				customKeywords = customKeywords
			};
		}
	}
}