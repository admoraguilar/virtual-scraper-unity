﻿using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Midnight;
using Midnight.Concurrency;

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

		private YouTubeScraper _youtubeScraper = new YouTubeScraper();

		public async Task UpdateAsync()
		{
			universalId = universalName.RemoveSpecialCharacters().Replace(" ", "");
			wikiUrl = $"https://virtualyoutuber.fandom.com/wiki/{universalName.Replace(" ", "_")}";

			bool isMainAvatarUrlSet = false;
			for(int i = 0; i < socials.Length; i++) {
				Social social = socials[i];
				if(social.platform == Platform.YouTube) {
					social = socials[i] = await _youtubeScraper.GetChannelInfo(social.url);
					if(!isMainAvatarUrlSet) {
						isMainAvatarUrlSet = true;
						avatarUrl = social.avatarUrl;
					}
				}
			}
		}

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

#if UNITY_EDITOR
		public void Editor_AutoFill()
		{
			TaskExt.FireForget(UpdateAsync());
		}
#endif
	}
}