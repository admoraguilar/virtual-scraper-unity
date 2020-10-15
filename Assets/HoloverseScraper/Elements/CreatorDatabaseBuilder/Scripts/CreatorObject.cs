using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Midnight;
using Midnight.Concurrency;

namespace Holoverse.Scraper
{
	using Api.Data.Contents;
	using Api.Data.Contents.Creators;
	using System.Collections.Generic;

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

		private YouTubeScraper _youtubeScraper = new YouTubeScraper();

		public async Task UpdateAsync()
		{
			universalId = universalName.RemoveSpecialCharacters().Replace(" ", "");
			wikiUrl = $"https://virtualyoutuber.fandom.com/wiki/{universalName.Replace(" ", "_")}";

			bool isMainAvatarUrlSet = false;
			for(int i = 0; i < socials.Length; i++) {
				Social social = socials[i];
				if(social.platform == Platform.YouTube) {
					social = socials[i] = await _youtubeScraper.GetChannelInfoAsync(social.url);
					if(!isMainAvatarUrlSet) {
						isMainAvatarUrlSet = true;
						avatarUrl = social.avatarUrl;
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
				universalName = universalName,
				universalId = universalId,
				wikiUrl = wikiUrl,
				avatarUrl = avatarUrl,

				isHidden = isHidden,

				affiliations = GetAffiliations().Select(a => a.universalId).ToArray(),
				isGroup = isGroup,
				depth = depth,

				socials = socials,
				customKeywords = customKeywords
			};
		}

#if UNITY_EDITOR
		public void Editor_AutoFill()
		{
			TaskExt.FireForget(UpdateAsync());
		}
#endif
	}
}