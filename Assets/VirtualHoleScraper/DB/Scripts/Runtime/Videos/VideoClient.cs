using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Midnight;
using Midnight.Tasks;

namespace VirtualHole.Scraper
{
	using DB;
	using DB.Contents;
	using DB.Contents.Videos;
	using DB.Contents.Creators;

	using DBVideoClient = DB.Contents.Videos.VideoClient;

	public class VideoClient
	{
		public string localJsonPath
		{
			get => PathUtilities.CreateDataPath("VirtualHoleScraper", "videos.json", PathType.Data);
		}

		private YouTubeScraperFactory _youTubeScraperFactory => _scraperClient.youtube;
		private ScraperClient _scraperClient = null;

		private DBVideoClient _dbVideoClient => _dbClient.Contents.Videos;
		private VirtualHoleDBClient _dbClient = null;

		public VideoClient(ScraperClient scraperClient, VirtualHoleDBClient dbClient)
		{
			_scraperClient = scraperClient;
			_dbClient = dbClient;
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
				if(incremental) { return _dbVideoClient.UpsertManyVideosAsync(videos, cancellationToken); } 
				else { return _dbVideoClient.UpsertManyVideosAndDeleteDanglingAsync(videos, cancellationToken); }
			}
		}

		public async Task<List<Video>> DummyScrapeAsync(
			IEnumerable<Creator> creators, bool isIncremental = false,
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
				if(isIncremental) {
					channelVideoSettings = new YouTubeScraper.ChannelVideoSettings {
						anchorDate = DateTimeOffset.UtcNow.Date,
						isForward = true
					};
				}

				foreach(Social youtube in creator.Socials.Where(s => s.Platform == Platform.YouTube)) {
					videos.AddRange(await ProcessSocialVideo(
						youtube, "Videos",
						(Social yt) => _youTubeScraperFactory.Get().GetChannelVideosAsync(creator, yt.Url, channelVideoSettings)));
				}

				async Task<List<T>> ProcessSocialVideo<T>(Social social, string socialPageName, Func<Social, Task<List<T>>> task)
				{
					using(StopwatchScope s = new StopwatchScope(
						nameof(VideoClient),
						$"Processing [{social.Platform} - {social.Name} - {socialPageName}]...",
						$"Finished processing [{social.Platform} - {social.Name} - {socialPageName}]!")) {
						return await TaskExt.RetryAsync(() => task(social), TimeSpan.FromSeconds(1), int.MaxValue, cancellationToken);
					}
				}
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
						Task.Run(async () => {
							videos.AddRange(await ProcessSocialVideo(
							   youtube, "Videos",
							   (Social yt) => _youTubeScraperFactory.Get().GetChannelVideosAsync(creator, yt.Url, channelVideoSettings)));
						}),

						// LEAK ON THESE METHODS!!! CHECK OUT YOUTUBE EXPLODE BROADCAST CLIENT IMPL!!!
						Task.Run(async () => {
							videos.AddRange(await ProcessSocialVideo(
							   youtube, "Scheduled",
							   (Social yt) => _youTubeScraperFactory.Get().GetChannelUpcomingBroadcastsAsync(creator, yt.Url)));
						}),
						Task.Run(async () => {
							videos.AddRange(await ProcessSocialVideo(
								youtube, "Live",
								(Social yt) => _youTubeScraperFactory.Get().GetChannelLiveBroadcastsAsync(creator, yt.Url)));
						})
					);
				}

				async Task<List<T>> ProcessSocialVideo<T>(Social social, string socialPageName, Func<Social, Task<List<T>>> task)
				{
					using(StopwatchScope s = new StopwatchScope(
						nameof(VideoClient),
						$"Processing [{social.Platform} - {social.Name} - {socialPageName}]...",
						$"Finished processing [{social.Platform} - {social.Name} - {socialPageName}]!")) {
						return await TaskExt.RetryAsync(() => task(social), TimeSpan.FromSeconds(1), 99, cancellationToken);
					}
				}
			}
		}

		public IEnumerable<Video> LoadFromJson()
		{
			return JsonUtilities.LoadFromDisk<Video[]>(new JsonUtilities.LoadFromDiskParameters() {
				filePath = localJsonPath,
				jsonSerializerSettings = GetSerializerSettings()
			});
		}

		public void SaveToJson(IEnumerable<Video> videos)
		{
			List<Video> list = new List<Video>(videos);
			JsonUtilities.SaveToDisk(list, new JsonUtilities.SaveToDiskParameters() {
				filePath = localJsonPath,
				jsonSerializerSettings = GetSerializerSettings()
			});
		}

		private JsonSerializerSettings GetSerializerSettings()
		{
			JsonSerializerSettings jsonSerializerSettings = JsonConfig.DefaultSettings;
			jsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
			return jsonSerializerSettings;
		}
	}
}
