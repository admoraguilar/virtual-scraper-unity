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
		public event Action<string> onScrapeVideosProgressDetail = delegate { };
		public event Action<string> onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail = delegate { };

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
		}

		public async Task WriteToVideosCollectionUsingLocalJson()
		{
			MLog.Log($"Start loading local json videos...");
			List<Video> videos = new List<Video>();
			JsonUtilities.LoadFromDisk(ref videos, new JsonUtilities.LoadFromDiskParameters {
				filePath = videosLocalJSONPath
			});
			MLog.Log($"Finish loading local json videos...");

			await WriteToVideosCollectionAsync(videos);
		}

		public async Task GetAndWriteToVideosCollectionFromCreatorsCollection()
		{
			List<Video> videos = new List<Video>();

			MLog.Log($"Start scraping videos...");
			onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail?.Invoke($"Start scraping videos...");
			using(IAsyncCursor<Creator> cursor = await GetCreatorsAsync(20)) {
				while(await cursor.MoveNextAsync()) {
					videos.AddRange(await ScrapeVideosAsync(cursor.Current));
				}
			}
			MLog.Log($"Finished scraping videos...");
			onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail?.Invoke($"Finished scraping videos...");
			
			onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail?.Invoke($"Writing to videos collection...");
			await WriteToVideosCollectionAsync(videos);
			onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail?.Invoke($"Finished writing to videos collection!");
		}

		public async Task<List<Video>> ScrapeVideosAsync(IEnumerable<Creator> creators)
		{
			List<Video> videos = new List<Video>();
			await Concurrent.ForEachAsync(creators.ToList(), ProcessCreator, 5);
			onScrapeVideosProgressDetail = delegate { };
			return videos;

			async Task ProcessCreator(Creator creator)
			{
				if(creator.isGroup) { return; }

				// YouTube
				foreach(Social youtube in creator.socials.Where(s => s.platform == Platform.YouTube)) {
					MLog.Log($"[YouTube: {youtube.name}] Scraping videos...");
					onScrapeVideosProgressDetail($"[YouTube: {youtube.name}] Scraping videos...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelVideos(creator, youtube.url),
						TimeSpan.FromSeconds(3), 50
					));

					MLog.Log($"[YouTube: {youtube.name}] Scraping upcoming broadcasts...");
					onScrapeVideosProgressDetail($"[YouTube: {youtube.name}] Scraping upcoming broadcasts...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelUpcomingBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3), 50
					));

					MLog.Log($"[YouTube: {youtube.name}] Scraping now broadcasts...");
					onScrapeVideosProgressDetail($"[YouTube: {youtube.name}] Scraping now broadcasts...");
					videos.AddRange(await TaskExt.Retry(
						() => _youtubeScraper.GetChannelLiveBroadcasts(creator, youtube.url),
						TimeSpan.FromSeconds(3), 50
					));
				}
			}
		}

		public async Task WriteToVideosCollectionAsync(IEnumerable<Video> videos)
		{
			MLog.Log($"Writing to videos collection...");
			await _dataClient.UpsertManyVideosAndDeleteDanglingAsync(videos);
			MLog.Log($"Finished writing to videos collection!");
		}
	}
}
