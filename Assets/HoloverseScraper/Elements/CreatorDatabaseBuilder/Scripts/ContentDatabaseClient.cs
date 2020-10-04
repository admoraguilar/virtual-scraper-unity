using System;
using System.Linq;
using System.Diagnostics;
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
			using(new StopwatchScope(
				$"[{nameof(ContentDatabaseClient)}] Start getting creators from collection.",
				$"[{nameof(ContentDatabaseClient)}] Finished getting creators from collection.")) 
			{
				return await _dataClient.FindMatchingCreatorsAsync(
					HoloverseDataFilter.Creator.All(),
					batchSize
				);
			}
		}

		public async Task WriteToCreatorsCollectionAsync(IEnumerable<Creator> creators)
		{
			using(new StopwatchScope(
				$"[{nameof(ContentDatabaseClient)}] Start writing creators to collection.",
				$"[{nameof(ContentDatabaseClient)}] Finished writing creators to collection.")) 
			{
				await _dataClient.UpsertManyCreatorsAndDeleteDanglingAsync(creators);
			}
		}

		public async Task<List<Video>> ScrapeVideosAsync(IEnumerable<Creator> creators)
		{
			List<Video> videos = new List<Video>();

			using(new StopwatchScope(
				$"[{nameof(ContentDatabaseClient)}] Start scraping videos of creators.",
				$"[{nameof(ContentDatabaseClient)}] Finished scraping videos of creators.")) {
				await Concurrent.ForEachAsync(creators.ToList(), ProcessCreator, 5);
			}

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
			using(new StopwatchScope(
				$"[{nameof(ContentDatabaseClient)}] Start gathering then writing videos from collection.",
				$"[{nameof(ContentDatabaseClient)}] Finished gathering then writing videos to collection.")) 
			{
				List<Video> videos = new List<Video>();

				using(IAsyncCursor<Creator> cursor = await GetCreatorsAsync(20)) {
					while(await cursor.MoveNextAsync()) {
						videos.AddRange(await ScrapeVideosAsync(cursor.Current));
					}
				}

				await WriteToVideosCollectionAsync(videos);
			}
		}

		public async Task WriteToVideosCollectionAsync(IEnumerable<Video> videos)
		{
			using(new StopwatchScope(
				$"[{nameof(ContentDatabaseClient)}] Start writing videos to collection.",
				$"[{nameof(ContentDatabaseClient)}] Finished writing videos to collection.")) {
				await _dataClient.UpsertManyVideosAndDeleteDanglingAsync(videos);
			}
		}
	}
}
