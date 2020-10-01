using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Midnight;
using Midnight.Concurrency;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Holoverse.Scraper
{
	using Api.Data.YouTube;

	using ExChannel = YoutubeExplode.Channels.Channel;
	using ExVideo = YoutubeExplode.Videos.Video;
	using ExBroadcast = YoutubeExplode.Videos.Broadcast;

	public partial class YouTubeScrapeOperation
	{
		private YoutubeClient _client = null;
		private Settings _settings = null;
		private AggregateMap _map = null;

		public YouTubeScrapeOperation(Settings settings)
		{
			_client = new YoutubeClient();
			_settings = settings;
			_map = new AggregateMap(
				PathUtilities.CreateDataPath("Holoverse", "", PathType.Data),
				settings
			);
		}

		public async Task Execute()
		{
			MLog.Log("Executing scraping operation....");

			List<string> channelUrls = new List<string>();
			channelUrls.AddRange(
				_settings.idols.SelectMany((ChannelGroup cg) => {
					return cg.channels.Select((Channel ch) => ch.url);
				})
			);
			channelUrls.AddRange(
				_settings.community.SelectMany((ChannelGroup cg) => {
					return cg.channels.Select((Channel ch) => ch.url);
				})
			);

			foreach(string channelUrl in channelUrls) {
				MLog.Log($"VIDEOS: {channelUrl}");
				List<Video> videos = await TaskExt.Retry(() => GetChannelVideos(channelUrl), TimeSpan.FromSeconds(3));
				videos.ForEach((Video video) => {
					MLog.Log($"Scraping ARCHIVE video: {video.channel} | {video.title}");
					_map.Add(video);
				});

				MLog.Log($"UPCOMING broadcast: {channelUrl}");
				List<Broadcast> upcomingBroadcasts = await TaskExt.Retry(() => GetChannelUpcomingBroadcasts(channelUrl), TimeSpan.FromSeconds(3));
				upcomingBroadcasts.ForEach((Broadcast broadcast) => {
					MLog.Log($"Scraping UPCOMING broadcast: {broadcast.channel} | {broadcast.title}");
					_map.Add(broadcast);
				});

				MLog.Log($"NOW broadcast: {channelUrl}");
				List<Broadcast> liveBroadcasts = await TaskExt.Retry(() => GetChannelLiveBroadcasts(channelUrl), TimeSpan.FromSeconds(3));
				liveBroadcasts.ForEach((Broadcast broadcast) => {
					MLog.Log($"Scraping NOW broadcast: {broadcast.channel} | {broadcast.title}");
					_map.Add(broadcast);
				});
			}
		}

		public void Save()
		{
			MLog.Log("Saving scraped data...");
			_map.Save();
		}

		public async Task<Channel> GetChannelInfo(string channelUrl)
		{
			ExChannel channel = await _client.Channels.GetAsync(channelUrl);
			return new Channel {
				url = channel.Url,
				id = channel.Id,
				name = channel.Title,
				avatarUrl = channel.LogoUrl
			};
		}

		public async Task<List<Video>> GetChannelVideos(string channelUrl)
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
					url = processedVideo.Url,
					id = processedVideo.Id,
					title = processedVideo.Title,
					description = processedVideo.Description,
					duration = processedVideo.Duration.ToString(),
					viewCount = processedVideo.Engagement.ViewCount,
					mediumResThumbnailUrl = processedVideo.Thumbnails.MediumResUrl,
					channel = processedVideo.Author,
					channelId = processedVideo.ChannelId,
					uploadDate = processedVideo.UploadDate.ToString()
				});
			}

			return results;
		}

		public async Task<List<Broadcast>> GetChannelLiveBroadcasts(string channelUrl)
		{
			return await GetChannelBroadcasts(channelUrl, BroadcastType.Now);
		}

		public async Task<List<Broadcast>> GetChannelUpcomingBroadcasts(string channelUrl)
		{
			return await GetChannelBroadcasts(channelUrl, BroadcastType.Upcoming);
		}

		private async Task<List<Broadcast>> GetChannelBroadcasts(string channelUrl, BroadcastType type)
		{
			List<Broadcast> results = new List<Broadcast>();

			IReadOnlyList<ExVideo> broadcasts = await _client.Channels.GetBroadcastsAsync(channelUrl, type);
			foreach(ExBroadcast broadcast in broadcasts.Select(v => v as ExBroadcast)) {
				results.Add(new Broadcast {
					url = broadcast.Url,
					id = broadcast.Id,
					title = broadcast.Title,
					description = broadcast.Description,
					duration = broadcast.Duration.ToString(),
					viewCount = broadcast.Engagement.ViewCount,
					mediumResThumbnailUrl = broadcast.Thumbnails.MediumResUrl,
					channel = broadcast.Author,
					channelId = broadcast.ChannelId,
					uploadDate = broadcast.UploadDate.ToString(),
					IsLive = broadcast.IsLive,
					viewerCount = broadcast.ViewerCount,
					schedule = broadcast.Schedule.ToString()
				});
			}

			return results;
		}
	}
}
