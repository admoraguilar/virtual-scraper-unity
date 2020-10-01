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
				return broadcast.IsLive;
			}

			public static bool IsChannelIdMatch<T>(T video, Channel channel)
				where T : Video
			{
				return IsChannelIdMatch(video, new Channel[] { channel });
			}

			public static bool IsChannelIdMatch<T>(T video, IEnumerable<Channel> channels)
				where T : Video
			{
				return channels.Any((Channel channel) => video.channelId.Contains(channel.id));
			}

			public static bool IsChannelMatch<T>(T video, Channel channel)
				where T : Video
			{
				return IsChannelMatch(video, new Channel[] { channel });
			}

			public static bool IsChannelMatch<T>(T video, IEnumerable<Channel> channels)
				where T : Video
			{
				return channels.Any(
					(Channel channel) => {
						return video.channelId.Contains(channel.id) ||
							   video.title.Contains(channel.id) ||
							   video.description.Contains(channel.id) ||
							   video.channelId.Contains(channel.name) ||
							   video.title.Contains(channel.name) ||
							   video.description.Contains(channel.name) ||
							   video.description.Contains(channel.url) ||
							   channel.customKeywords.Any((string keyword) => {
								   return video.channelId.Contains(keyword) ||
										  video.title.Contains(keyword) ||
										  video.description.Contains(keyword);
							   });
					}
				);
			}
		}
	}
}