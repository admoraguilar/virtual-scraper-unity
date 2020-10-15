using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Midnight;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Holoverse.Scraper
{
	using Api.Data.Contents;
	using Api.Data.Contents.Creators;
	using Api.Data.Contents.Videos;

	using ExChannel = YoutubeExplode.Channels.Channel;
	using ExVideo = YoutubeExplode.Videos.Video;
	using ExBroadcast = YoutubeExplode.Videos.Broadcast;

	public class YouTubeScraper
	{
		public class ChannelVideoSettings
		{
			public DateTimeOffset anchorDate = DateTimeOffset.MinValue;
			public bool isForward = true;
		}

		private YoutubeClient _client = null;

		public YouTubeScraper()
		{
			_client = new YoutubeClient();
		}

		public YouTubeScraper(HttpClient httpClient)
		{
			_client = new YoutubeClient(httpClient);
		}

		public async Task<Social> GetChannelInfoAsync(string channelUrl)
		{
			ExChannel channel = await _client.Channels.GetAsync(channelUrl);
			return new Social {
				name = channel.Title,
				platform = Platform.YouTube,
				id = channel.Id,
				url = channel.Url,
				avatarUrl = channel.LogoUrl
			};
		}

		public async Task<List<Video>> GetChannelVideosAsync(
			Creator creator, string channelUrl,
			ChannelVideoSettings settings = null)
		{
			List<Video> results = new List<Video>();

			IReadOnlyList<ExVideo> videos = await _client.Channels.GetUploadsAsync(channelUrl);
			DateTimeOffset uploadDateAnchor = default;
			foreach(ExVideo video in videos) {
				// We process the video date because sometimes
				// the dates are messed up, so we run a correction to
				// fix it
				if(uploadDateAnchor != default && video.UploadDate.Subtract(uploadDateAnchor).TotalDays > 60) {
					//MLog.Log(
					//	nameof(YouTubeScraper),
					//	$"Wrong date detected from [L: {uploadDateAnchor} | C:{video.UploadDate}]! " +
					//	$"Fixing {video.Title}..."
					//);
					
					// Disabled: We don't do a full checking of a the video anymore for
					// full accuracy because it's too slow of a process especially if there's
					// too many discrepancies in dates
					//processedVideo = await _client.Videos.GetAsync(processedVideo.Url);

					// Hack: as the videos we're scraping are always descending
					// we just put a date that's a bit behind the upload date anchor
					// this is so if we put things the videos in order they'd still be
					// in order albeit now with accurate dates
					uploadDateAnchor = uploadDateAnchor.AddDays(-1);
				} else {
					uploadDateAnchor = video.UploadDate;
				}

				if(settings != null) {
					if(settings.isForward && settings.anchorDate > uploadDateAnchor) { continue; }
					if(!settings.isForward && settings.anchorDate < uploadDateAnchor) { continue; }
				}

				results.Add(new Video {
					title = video.Title,
					platform = Platform.YouTube,
					id = video.Id,
					url = video.Url,

					creator = video.Author,
					creatorId = video.ChannelId,
					creatorUniversal = creator.universalName,
					creatorIdUniversal = creator.universalId,

					creationDate = uploadDateAnchor,
					tags = video.Keywords.ToArray(),

					thumbnailUrl = video.Thumbnails.MediumResUrl,
					description = video.Description,
					duration = video.Duration,
					viewCount = video.Engagement.ViewCount
				});
			}

			return results;
		}

		public async Task<List<Broadcast>> GetChannelLiveBroadcastsAsync(Creator creator, string channelUrl)
		{
			return await GetChannelBroadcastsAsync(
				creator, channelUrl,
				BroadcastType.Now);
		}

		public async Task<List<Broadcast>> GetChannelUpcomingBroadcastsAsync(Creator creator, string channelUrl)
		{
			return await GetChannelBroadcastsAsync(
				creator, channelUrl,
				BroadcastType.Upcoming);
		}

		private async Task<List<Broadcast>> GetChannelBroadcastsAsync(
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
