using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using Newtonsoft.Json;
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

		public async Task<IAsyncCursor<Creator>> GetCreatorsAsync(int batchSize)
		{
			return await _dataClient.FindMatchingCreatorsAsync(
				HoloverseDataFilter.Creator.All(),
				batchSize
			);
		}

		public void ExportCreatorsJSON(IEnumerable<Creator> creators)
		{
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = creatorsLocalJSONPath
			});
		}

		public async Task WriteToCreatorsCollectionAsync(IEnumerable<Creator> creators)
		{
			await _dataClient.UpsertManyCreatorsAndDeleteDanglingAsync(creators);
		}

		public async Task ExportVideosUsingLocalCreatorsJSONAsync()
		{
			MLog.Log($"[{nameof(ContentDatabaseClientObject)}] Start scraping videos.");

			Creator[] creators = null;
			JsonUtilities.LoadFromDisk(ref creators, new JsonUtilities.LoadFromDiskParameters {
				filePath = creatorsLocalJSONPath
			});

			List<Video> videos = await ScrapeVideosAsync(creators);

			JsonUtilities.SaveToDisk(videos, new JsonUtilities.SaveToDiskParameters {
				filePath = videosLocalJSONPath,
				jsonSerializerSettings = new JsonSerializerSettings {
					TypeNameHandling = TypeNameHandling.Auto
				}
			});

			MLog.Log($"[{nameof(ContentDatabaseClientObject)}] Finish scraping videos.");
		}

		public async Task GetAndWriteToVideosCollectionFromCreatorsCollection()
		{
			List<Video> videos = new List<Video>();

			using(IAsyncCursor<Creator> cursor = await GetCreatorsAsync(20)) {
				while(await cursor.MoveNextAsync()) {
					videos.AddRange(await ScrapeVideosAsync(cursor.Current));
				}
			}

			await _dataClient.UpsertManyVideosAndDeleteDanglingAsync(videos);
		}

		public async Task<List<Video>> ScrapeVideosAsync(IEnumerable<Creator> creators)
		{
			List<Video> videos = new List<Video>();
			await Concurrent.ForEachAsync(creators.ToList(), ProcessCreator, 5);
			return videos;

			async Task ProcessCreator(Creator creator)
			{
				if(creator.isGroup) { return; }

				// YouTube
				foreach(Social youtube in creator.socials.Where(s => s.platform == Platform.YouTube)) {
					MLog.Log($"[{nameof(ContentDatabaseClient)}] [YouTube: {youtube.name}] Scraping videos...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelVideos(creator, youtube.url),
						TimeSpan.FromSeconds(3)
					));

					MLog.Log($"[{nameof(ContentDatabaseClient)}] [YouTube: {youtube.name}] Scraping upcoming broadcasts...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelUpcomingBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3)
					));

					MLog.Log($"[{nameof(ContentDatabaseClient)}] [YouTube: {youtube.name}] Scraping now broadcasts...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelLiveBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3)
					));
				}
			}
		}
	}
}
