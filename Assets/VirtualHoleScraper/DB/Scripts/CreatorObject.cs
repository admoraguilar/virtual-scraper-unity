using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Midnight;

namespace VirtualHole.Scraper
{
	using DB.Contents;
	using DB.Contents.Creators;

	[CreateAssetMenu(menuName = "VirtualHole/DB/Creator Object")]
	public class CreatorObject : ScriptableObject
	{
		public static implicit operator Creator(CreatorObject obj) => obj.AsCreator();

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

		public Creator AsCreator()
		{
			return new Creator() {
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

		public CreatorObject[] GetAffiliations()
		{
			HashSet<CreatorObject> results = new HashSet<CreatorObject>();
			foreach(CreatorObject creator in affiliations) {
				creator.GetAffiliations().ForEach(a => results.Add(a));
				results.Add(creator);
			}
			return results.OrderByDescending((CreatorObject obj) => obj.depth).ToArray();
		}

		//public async Task AutofillAsync(YouTubeScraper scraper)
		//{
		//	using(StopwatchScope stopwatchScope = new StopwatchScope(
		//		nameof(CreatorObject),
		//		$"Start auto fill [{universalName}] info",
		//		$"Finished auto filling [{universalName}] info")) {
		//		universalId = universalName.RemoveSpecialCharacters().Replace(" ", "");
		//		wikiUrl = $"https://virtualyoutuber.fandom.com/wiki/{universalName.Replace(" ", "_")}";

		//		bool isMainAvatarUrlSet = false;
		//		for(int i = 0; i < socials.Length; i++) {
		//			Social social = socials[i];
		//			if(social.Platform == Platform.YouTube) {
		//				social = socials[i] = await scraper.GetChannelInfoAsync(social.Url);
		//				if(!isMainAvatarUrlSet) {
		//					isMainAvatarUrlSet = true;
		//					avatarUrl = social.AvatarUrl;
		//				}
		//			}
		//		}
		//	}
		//}

		//public void Autofill(string json)
		//{
		//	CreatorClient creatorClient = new CreatorClient(null);
			
		//	Creator creator = creatorClient.LoadFromJson().FirstOrDefault(c => c.UniversalId == universalId);
		//	if(creator == null) { 
		//		MLog.Log(nameof(CreatorObject), $"Can't find [{universalId}] data on json.");
		//		return;
		//	}

		//	socials = creator.Socials;
		//}
	}
}