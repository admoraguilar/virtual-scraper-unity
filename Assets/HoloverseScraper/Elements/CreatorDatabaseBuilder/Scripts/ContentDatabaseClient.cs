using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using MongoDB.Driver;
using Midnight;
using Midnight.Concurrency;

namespace Holoverse.Scraper
{
	using Api.Data;

	public class ContentDatabaseClient
	{
		private string creatorsLocalJSONPath => PathUtilities.CreateDataPath("Holoverse", "creators.json", PathType.Data);
		private string videosLocalJSONPath => PathUtilities.CreateDataPath("Holoverse", "videos.json", PathType.Data);

		private HoloverseDataClient _dataClient = null;
		private YouTubeScraper _youtubeScraper = null;

		public ContentDatabaseClient(HoloverseDataClientSettings settings)
		{
			_dataClient = new HoloverseDataClient(settings);
			_youtubeScraper = new YouTubeScraper();
		}

		public async Task<IAsyncCursor<Creator>> GetCreatorsAsync(
			int batchSize, int resultsLimit = int.MaxValue, 
			CancellationToken cancellationToken = default)
		{
			return await _dataClient.FindMatchingCreatorsAsync(
				HoloverseDataFilter.Creator.All(),
				batchSize, resultsLimit,
				cancellationToken
			);
		}

		public void ExportCreatorsJSON(IEnumerable<Creator> creators)
		{
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = creatorsLocalJSONPath
			});
		}

		public async Task WriteToCreatorsCollectionAsync(
			IEnumerable<Creator> creators, CancellationToken cancellationToken = default)
		{
			await _dataClient.UpsertManyCreatorsAndDeleteDanglingAsync(
				creators, cancellationToken);
		}

		public async Task ExportVideosUsingLocalCreatorsJSONAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			Creator[] creators = null;
			JsonUtilities.LoadFromDisk(ref creators, new JsonUtilities.LoadFromDiskParameters {
				filePath = creatorsLocalJSONPath
			});

			List<Video> videos = await ScrapeVideosAsync(
				creators, incremental, 
				cancellationToken);

			JsonUtilities.SaveToDisk(videos, new JsonUtilities.SaveToDiskParameters {
				filePath = videosLocalJSONPath,
				jsonSerializerSettings = new JsonSerializerSettings {
					TypeNameHandling = TypeNameHandling.Auto
				}
			});
		}

		public async Task WriteToVideosCollectionUsingLocalJson(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			MLog.Log(nameof(ContentDatabaseClient), $"Start loading local json videos...");
			List<Video> videos = new List<Video>();
			JsonUtilities.LoadFromDisk(ref videos, new JsonUtilities.LoadFromDiskParameters {
				filePath = videosLocalJSONPath
			});
			MLog.Log(nameof(ContentDatabaseClient), $"Finish loading local json videos...");

			await WriteToVideosCollectionAsync(
				videos, incremental, 
				cancellationToken);
		}

		public async Task GetAndWriteToVideosCollectionFromCreatorsCollection(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			List<Video> videos = new List<Video>();

			MLog.Log(nameof(ContentDatabaseClient), $"Start scraping videos...");
			using(IAsyncCursor<Creator> cursor = await GetCreatorsAsync(20, int.MaxValue, cancellationToken)) {
				while(await cursor.MoveNextAsync()) {
					cancellationToken.ThrowIfCancellationRequested();
					videos.AddRange(await ScrapeVideosAsync(
						cursor.Current, incremental,
						cancellationToken));
				}
			}
			MLog.Log(nameof(ContentDatabaseClient), $"Finished scraping videos...");

			await WriteToVideosCollectionAsync(videos, incremental, cancellationToken);
		}

		private async Task<List<Video>> ScrapeVideosAsync(
			IEnumerable<Creator> creators, bool incremental = false,
			CancellationToken cancellationToken = default)
		{
			List<Video> videos = new List<Video>();
			await Concurrent.ForEachAsync(
				creators.ToList(), ProcessCreator,
				5, cancellationToken);
			return videos;

			async Task ProcessCreator(Creator creator)
			{
				if(creator.isGroup) { return; }

				YouTubeScraper.ChannelVideoSettings channelVideoSettings = null;
				if(incremental) {
					channelVideoSettings = new YouTubeScraper.ChannelVideoSettings {
						anchorDate = DateTimeOffset.UtcNow.Date,
						isForward = true
					};
				}

				// YouTube
				foreach(Social youtube in creator.socials.Where(s => s.platform == Platform.YouTube)) {
					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.name}] Scraping videos...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => _youtubeScraper.GetChannelVideos(creator, youtube.url, channelVideoSettings),
						TimeSpan.FromSeconds(3), 50, cancellationToken
					));
					cancellationToken.ThrowIfCancellationRequested();

					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.name}] Scraping upcoming broadcasts...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => _youtubeScraper.GetChannelUpcomingBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3), 50, cancellationToken
					));
					cancellationToken.ThrowIfCancellationRequested();

					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.name}] Scraping now broadcasts...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => _youtubeScraper.GetChannelLiveBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3), 50, cancellationToken
					));
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
		}

		private async Task WriteToVideosCollectionAsync(
			IEnumerable<Video> videos, bool incremental = false,
			CancellationToken cancellationToken = default)
		{
			MLog.Log(nameof(ContentDatabaseClient), $"Writing to videos collection...");
			if(incremental) { await _dataClient.UpsertManyVideosAsync(videos, cancellationToken); }
			else { await _dataClient.UpsertManyVideosAndDeleteDanglingAsync(videos, cancellationToken); }
			MLog.Log(nameof(ContentDatabaseClient), $"Finished writing to videos collection!");
		}
	}
}
