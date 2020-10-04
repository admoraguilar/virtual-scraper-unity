using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Driver;
using Midnight;
using Midnight.Concurrency;

namespace Holoverse.Scraper
{
	using Api.Data;

	public class ContentDatabaseClient
	{
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

		public async Task WriteToCreatorsCollectionAsync(IEnumerable<Creator> creators)
		{
			await _dataClient.UpsertManyCreatorsAndDeleteDanglingAsync(creators);
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

		public async Task WriteToVideosCollectionAsync()
		{
			List<Video> videos = new List<Video>();

			using(IAsyncCursor<Creator> cursor = await GetCreatorsAsync(20)) {
				while(await cursor.MoveNextAsync()) {
					videos.AddRange(await ScrapeVideosAsync(cursor.Current));
				}
			}

			await WriteToVideosCollectionAsync(videos);
		}

		public async Task WriteToVideosCollectionAsync(IEnumerable<Video> videos)
		{
			await _dataClient.UpsertManyVideosAndDeleteDanglingAsync(videos);
		}
	}
}
