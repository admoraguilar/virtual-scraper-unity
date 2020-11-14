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
	using DB.Common;
	using DB.Contents;
	using DB.Contents.Creators;
	using DB.Contents.Videos;

	public class ContentDatabaseClient
	{
		private string creatorsLocalJSONPath => PathUtilities.CreateDataPath("VirtualHole", "creators.json", PathType.Data);
		private string videosLocalJSONPath => PathUtilities.CreateDataPath("VirtualHole", "videos.json", PathType.Data);

		private VirtualHoleDBClient _dataClient = null;

		public bool isUseProxy { get; set; } = false;

		public ICollection<Proxy> proxies => _proxyList;
		private List<Proxy> _proxyList = new List<Proxy>();
		private Queue<Proxy> _proxyQueue = new Queue<Proxy>();

		private YouTubeScraper youtubeScraper
		{
			get {
				if(isUseProxy) {
					if(_proxyQueue.Count <= 0) {
						_proxyList.Shuffle();
						_proxyQueue = new Queue<Proxy>(_proxyList);
					}
					Proxy proxy = _proxyQueue.Dequeue();

					MLog.Log(nameof(ContentDatabaseClient), $"Proxy: {proxy}");
					_youtubeScraper = new YouTubeScraper(HttpClientFactory.CreateOrGetProxyClient(proxy));
				} else {
					if(_youtubeScraper == null) {
						_youtubeScraper = new YouTubeScraper();
					}
				}

				return _youtubeScraper;
			}
		}
		private YouTubeScraper _youtubeScraper = null;

		public ContentDatabaseClient(
			string connectionString, string userName,
			string password)
		{
			_dataClient = new VirtualHoleDBClient(connectionString, userName, password);
			_youtubeScraper = new YouTubeScraper();
		}

		public void SetProxies(string proxiesText)
		{
			_proxyList.Clear();

			IReadOnlyList<string> rawProxies = TextFileUtilities.GetNLSV(proxiesText);
			foreach(string rawProxy in rawProxies) {
				if(Proxy.TryParse(rawProxy, out Proxy proxy)) {
					_proxyList.Add(proxy);
				}
			}
		}

		public async Task<FindResults<Creator>> GetAllCreatorsAsync(
			CancellationToken cancellationToken = default)
		{
			return await _dataClient.Contents.Creators.FindCreatorsAsync(
				new FindCreatorsStrictSettings { IsAll = true }, cancellationToken);
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
			await _dataClient.Contents.Creators.UpsertManyCreatorsAndDeleteDanglingAsync(
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

		public async Task WriteToVideosCollectionUsingLocalJsonAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			MLog.Log(nameof(ContentDatabaseClient), $"Start loading local json videos...");
			List<Video> videos = new List<Video>();
			JsonUtilities.LoadFromDisk(ref videos, new JsonUtilities.LoadFromDiskParameters {
				filePath = videosLocalJSONPath,
				jsonSerializerSettings = new JsonSerializerSettings {
					TypeNameHandling = TypeNameHandling.Auto
				}
			});
			MLog.Log(nameof(ContentDatabaseClient), $"Finish loading local json videos...");

			await WriteToVideosCollectionAsync(
				videos, incremental, 
				cancellationToken);
		}

		public async Task GetAndWriteToVideosCollectionFromCreatorsCollectionAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			List<Video> videos = new List<Video>();

			MLog.Log(nameof(ContentDatabaseClient), $"Start scraping videos...");
			using(FindResults<Creator> cursor = await GetAllCreatorsAsync(cancellationToken)) {
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
					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.Name}] Scraping videos...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => youtubeScraper.GetChannelVideosAsync(creator, youtube.Url, channelVideoSettings),
						TimeSpan.FromSeconds(1), 3, cancellationToken
					));
					cancellationToken.ThrowIfCancellationRequested();

					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.Name}] Scraping upcoming broadcasts...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => youtubeScraper.GetChannelUpcomingBroadcastsAsync(creator, youtube.Url),
						TimeSpan.FromSeconds(1), 3, cancellationToken
					));
					cancellationToken.ThrowIfCancellationRequested();

					MLog.Log(nameof(ContentDatabaseClient), $"[YouTube: {youtube.Name}] Scraping now broadcasts...");
					videos.AddRange(await TaskExt.RetryAsync(
						() => youtubeScraper.GetChannelLiveBroadcastsAsync(creator, youtube.Url),
						TimeSpan.FromSeconds(1), 3, cancellationToken
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
			await TaskExt.RetryAsync(
				() => WriteAsync(),
				TimeSpan.FromSeconds(1), 3,
				cancellationToken);
			MLog.Log(nameof(ContentDatabaseClient), $"Finished writing to videos collection!");

			Task WriteAsync()
			{
				if(incremental) { return _dataClient.Contents.Videos.UpsertManyVideosAsync(videos, cancellationToken); }
				else { return _dataClient.Contents.Videos.UpsertManyVideosAndDeleteDanglingAsync(videos, cancellationToken); }
			}
		}
	}
}
