using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Midnight;

namespace VirtualHole.Scraper
{
	using DB.Contents;
	using DB.Contents.Creators;

	[CreateAssetMenu(menuName = "VirtualHole/DB Builder/Creator Object")]
	public class CreatorObject : ScriptableObject
	{
		public static implicit operator Creator(CreatorObject obj) => obj.ToCreator();

		public string universalName = string.Empty;
		public string universalId = string.Empty;
		public string wikiUrl = string.Empty;
		public string avatarUrl = string.Empty;
		public bool isHidden = false;

		[Space]
		public CreatorObject[] affiliations = new CreatorObject[0];
		public bool isGroup = false;
		public int depth = 0;

		[Space]
		public Social[] socials = new Social[0];
		public string[] customKeywords = new string[0];

		private YouTubeScraper _youtubeScraper = null;

		public async Task AutoFillInfoAsync()
		{
			universalId = universalName.RemoveSpecialCharacters().Replace(" ", "");
			wikiUrl = $"https://virtualyoutuber.fandom.com/wiki/{universalName.Replace(" ", "_")}";

			bool isMainAvatarUrlSet = false;
			for(int i = 0; i < socials.Length; i++) {
				Social social = socials[i];
				if(social.Platform == Platform.YouTube) {
					social = socials[i] = await _youtubeScraper.GetChannelInfoAsync(social.Url);
					if(!isMainAvatarUrlSet) {
						isMainAvatarUrlSet = true;
						avatarUrl = social.AvatarUrl;
					}
				}
			}
		}

		public CreatorObject[] GetAffiliations()
		{
			List<CreatorObject> results = new List<CreatorObject>();

			foreach(CreatorObject creator in affiliations) {
				foreach(CreatorObject affiliation in creator.GetAffiliations()) {
					AddAffiliation(affiliation);
				}
				AddAffiliation(creator);
				
				void AddAffiliation(CreatorObject value)
				{
					if(!results.Contains(value)) {
						results.Add(value);
					}
				}
			}

			return results.OrderByDescending((CreatorObject obj) => obj.depth).ToArray();
		}

		public Creator ToCreator()
		{
			return new Creator 
			{
				UniversalName = universalName,
				UniversalId = universalId,
				WikiUrl = wikiUrl,
				AvatarUrl = avatarUrl,

				IsHidden = isHidden,

				Affiliations = GetAffiliations().Select(a => a.universalId).ToArray(),
				IsGroup = isGroup,
				Depth = depth,

				Socials = socials,
				CustomKeywords = customKeywords
			};
		}
	}
}