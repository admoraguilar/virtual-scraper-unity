using System.Linq;
using System.Collections.Generic;

namespace Holoverse.Scraper
{
	using Api.Data.YouTube;

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

			public static bool IsAuthorIdMatch<T>(T video, Author author)
				where T : Video
			{
				return IsAuthorIdMatch(video, new Author[] { author });
			}

			public static bool IsAuthorIdMatch<T>(T video, IEnumerable<Author> authors)
				where T : Video
			{
				return authors.Any((Author author) => video.authorId.Contains(author.id));
			}

			public static bool IsAuthorMatch<T>(T video, Author author)
				where T : Video
			{
				return IsAuthorMatch(video, new Author[] { author });
			}

			public static bool IsAuthorMatch<T>(T video, IEnumerable<Author> authors)
				where T : Video
			{
				return authors.Any(
					(Author author) => {
						return video.authorId.Contains(author.id) ||
							   video.title.Contains(author.id) ||
							   video.description.Contains(author.id) ||
							   video.authorId.Contains(author.name) ||
							   video.title.Contains(author.name) ||
							   video.description.Contains(author.name) ||
							   video.description.Contains(author.url) ||
							   author.customKeywords.Any((string keyword) => {
								   return video.authorId.Contains(keyword) ||
										  video.title.Contains(keyword) ||
										  video.description.Contains(keyword);
							   });
					}
				);
			}
		}
	}
}