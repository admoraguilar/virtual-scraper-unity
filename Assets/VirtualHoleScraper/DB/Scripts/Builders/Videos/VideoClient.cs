using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Midnight;
using Midnight.Concurrency;

namespace VirtualHole.Scraper
{
	using DB;
	using DB.Contents;
	using DB.Contents.Videos;
	using DB.Contents.Creators;

	public class VideoClient
	{
		public string localJsonPath 
		{
			get => PathUtilities.CreateDataPath("VirtualHoleScraper", "videos.json", PathType.Data);
		}

		private VirtualHoleDBClient _dbClient = null;
		private YouTubeScraperFactory _youtubeScraperFactory = null;

		public VideoClient(VirtualHoleDBClient dbClient, YouTubeScraperFactory youTubeScraperFactory)
		{
			_dbClient = dbClient;
			_youtubeScraperFactory = youTubeScraperFactory;
		}

		public async Task WriteToDBAsync(
			IEnumerable<Video> videos, bool incremental = false, 
			CancellationToken cancellationToken = default)
		{
			using(StopwatchScope s = new StopwatchScope(
				nameof(VideoClient),
				"Writing to videos collection...",
				"Finished writing to videos collection!")) {
				await TaskExt.RetryAsync(
					() => WriteAsync(),
					TimeSpan.FromSeconds(1), 3,
					cancellationToken);
			}

			Task WriteAsync()
			{
				if(incremental) { return _dbClient.Contents.Videos.UpsertManyVideosAsync(videos, cancellationToken); } 
				else { return _dbClient.Contents.Videos.UpsertManyVideosAndDeleteDanglingAsync(videos, cancellationToken); }
			}
		}

		public async Task<List<Video>> ScrapeAsync(
			IEnumerable<Creator> creators, bool incremental = false,
			CancellationToken cancellationToken = default)
		{
			List<Video> videos = new List<Video>();
			await Concurrent.ForEachAsync(creators.ToList(), ProcessCreator, 5, cancellationToken);
			return videos;

			async Task ProcessCreator(Creator creator)
			{
				if(creator.IsGroup) {
					await Task.CompletedTask;
					return;
				}

				YouTubeScraper.ChannelVideoSettings channelVideoSettings = null;
				if(incremental) {
					channelVideoSettings = new YouTubeScraper.ChannelVideoSettings {
						anchorDate = DateTimeOffset.UtcNow.Date,
						isForward = true
					};
				}

				// YouTube
				foreach(Social youtube in creator.Socials.Where(s => s.Platform == Platform.YouTube)) {
					await Task.WhenAll(
						Task.Run(() => ProcessSocialVideo(
							youtube, "Videos", 
							(Social yt) => _youtubeScraperFactory.Get().GetChannelVideosAsync(creator, yt.Url, channelVideoSettings))),
						Task.Run(() => ProcessSocialVideo(
							youtube, "Scheduled",
							(Social yt) => _youtubeScraperFactory.Get().GetChannelUpcomingBroadcastsAsync(creator, yt.Url))),
						Task.Run(() => ProcessSocialVideo(
							youtube, "Live",
							(Social yt) => _youtubeScraperFactory.Get().GetChannelLiveBroadcastsAsync(creator, yt.Url)))
					);
				}

				async Task<List<T>> ProcessSocialVideo<T>(Social social, string socialPageName, Func<Social, Task<List<T>>> task)
				{
					using(StopwatchScope s = new StopwatchScope(
						nameof(VideoClient),
						$"Processing [{social.Platform} - {social.Name} - {socialPageName}]...",
						$"Finished processing [{social.Platform} - {social.Name} - {socialPageName}]!")) {
						return await TaskExt.RetryAsync(() => task(social), TimeSpan.FromSeconds(1), 3, cancellationToken);
					}
				}
			}
		}

		public IEnumerable<Video> LoadFromJson()
		{
			JsonSerializerSettings jsonSerializerSettings = JsonConfig.DefaultSettings;
			jsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;

			return JsonUtilities.LoadFromDisk<Video[]>(new JsonUtilities.LoadFromDiskParameters {
				filePath = localJsonPath,
				jsonSerializerSettings = jsonSerializerSettings
			});
		}

		public void SaveToJson(IEnumerable<Video> videos)
		{
			List<Video> list = new List<Video>(videos);

			JsonSerializerSettings jsonSerializerSettings = JsonConfig.DefaultSettings;
			jsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;

			JsonUtilities.SaveToDisk(list, new JsonUtilities.SaveToDiskParameters {
				filePath = localJsonPath,
				jsonSerializerSettings = jsonSerializerSettings
			});
		}
	}
}
