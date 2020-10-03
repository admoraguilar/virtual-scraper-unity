using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Midnight;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Holoverse.Scraper
{
	using Api.Data;

	using ExChannel = YoutubeExplode.Channels.Channel;
	using ExVideo = YoutubeExplode.Videos.Video;
	using ExBroadcast = YoutubeExplode.Videos.Broadcast;

	public class YouTubeScraper
	{
		private YoutubeClient _client = null;

		public YouTubeScraper()
		{
			_client = new YoutubeClient();
		}

		public async Task<Creator> GetChannelInfo(string channelUrl)
		{
			ExChannel creator = await _client.Channels.GetAsync(channelUrl);
			return new Creator {
				wikiUrl = creator.Url,
				universalId = creator.Id,
				universalName = creator.Title,
				avatarUrl = creator.LogoUrl
			};
		}

		public async Task<List<Video>> GetChannelVideos(Creator creator, string channelUrl)
		{
			List<Video> results = new List<Video>();

			IReadOnlyList<ExVideo> videos = await _client.Channels.GetUploadsAsync(channelUrl);
			DateTimeOffset lastVideoDate = default;
			foreach(ExVideo video in videos) {
				// We process the video date because sometimes
				// the dates are messed up, so we run a correction to
				// fix it
				ExVideo processedVideo = video;
				if(lastVideoDate != default && processedVideo.UploadDate.Subtract(lastVideoDate).TotalDays > 60) {
					MLog.Log($"Wrong date detected! Fixing {processedVideo.Title}...");
					processedVideo = await _client.Videos.GetAsync(processedVideo.Url);
				}
				lastVideoDate = processedVideo.UploadDate;

				results.Add(new Video {
					title = video.Title,
					platform = Platform.YouTube,
					id = video.Id,
					url = video.Url,

					creator = video.Author,
					creatorId = video.ChannelId,
					creatorUniversal = creator.universalName,
					creatorIdUniversal = creator.universalId,

					creationDate = video.UploadDate,
					tags = video.Keywords.ToArray(),

					entryTimestamp = DateTimeOffset.UtcNow,

					thumbnailUrl = video.Thumbnails.MediumResUrl,
					description = video.Description,
					duration = video.Duration,
					viewCount = video.Engagement.ViewCount
				});
			}

			return results;
		}

		public async Task<List<Broadcast>> GetChannelLiveBroadcasts(Creator creator, string channelUrl)
		{
			return await GetChannelBroadcasts(
				creator, channelUrl, 
				BroadcastType.Now
			);
		}

		public async Task<List<Broadcast>> GetChannelUpcomingBroadcasts(Creator creator, string channelUrl)
		{
			return await GetChannelBroadcasts(
				creator, channelUrl, 
				BroadcastType.Upcoming
			);
		}

		private async Task<List<Broadcast>> GetChannelBroadcasts(
			Creator creator, string channelUrl, 
			BroadcastType type)
		{
			List<Broadcast> results = new List<Broadcast>();

			IReadOnlyList<ExVideo> broadcasts = await _client.Channels.GetBroadcastsAsync(channelUrl, type);
			foreach(ExBroadcast broadcast in broadcasts.Select(v => v as ExBroadcast)) {
				results.Add(new Broadcast {
					title = broadcast.Title,
					platform = Platform.YouTube,
					id = broadcast.Id,
					url = broadcast.Url,

					creator = broadcast.Author,
					creatorId = broadcast.ChannelId,
					creatorUniversal = creator.universalName,
					creatorIdUniversal = creator.universalId,

					creationDate = broadcast.UploadDate,
					tags = broadcast.Keywords.ToArray(),

					entryTimestamp = DateTimeOffset.UtcNow,

					thumbnailUrl = broadcast.Thumbnails.MediumResUrl,
					description = broadcast.Description,
					duration = broadcast.Duration,
					viewCount = broadcast.Engagement.ViewCount,

					isLive = broadcast.IsLive,
					viewerCount = broadcast.ViewerCount,
					schedule = broadcast.Schedule
				});
			}

			return results;
		}
	}
}
