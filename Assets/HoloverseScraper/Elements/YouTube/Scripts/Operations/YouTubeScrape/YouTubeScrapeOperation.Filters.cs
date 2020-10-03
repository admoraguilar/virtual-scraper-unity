using System.Linq;
using System.Collections.Generic;

namespace Holoverse.Scraper
{
	using Api.Data;

	public partial class YouTubeScrapeOperation
	{
		public static class Filters
		{
			public static bool ContainsTextInTitle<T>(T video, string text)
				where T : Video
			{
				return video.title.Contains(text);
			}

			public static bool IsLive(Broadcast broadcast)
			{
				return broadcast.isLive;
			}

			public static bool IsCreatorIdMatch<T>(T video, Creator creator)
				where T : Video
			{
				return IsCreatorIdMatch(video, new Creator[] { creator });
			}

			public static bool IsCreatorIdMatch<T>(T video, IEnumerable<Creator> authors)
				where T : Video
			{
				return authors.Any((Creator creator) => video.creatorId.Contains(creator.universalId));
			}

			public static bool IsCreatorMatch<T>(T video, Creator creator)
				where T : Video
			{
				return IsCreatorMatch(video, new Creator[] { creator });
			}

			public static bool IsCreatorMatch<T>(T video, IEnumerable<Creator> authors)
				where T : Video
			{
				return authors.Any(
					(Creator creator) => {
						return video.creatorId.Contains(creator.universalId) ||
							   video.title.Contains(creator.universalId) ||
							   video.description.Contains(creator.universalId) ||
							   video.creatorId.Contains(creator.universalName) ||
							   video.title.Contains(creator.universalName) ||
							   video.description.Contains(creator.universalName) ||
							   video.description.Contains(creator.wikiUrl) ||
							   creator.customKeywords.Any((string keyword) => {
								   return video.creatorId.Contains(keyword) ||
										  video.title.Contains(keyword) ||
										  video.description.Contains(keyword);
							   });
					}
				);
			}
		}
	}
}