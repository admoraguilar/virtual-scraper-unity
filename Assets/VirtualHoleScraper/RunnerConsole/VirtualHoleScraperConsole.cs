using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using Midnight.Tasks;

namespace VirtualHole.Scraper
{
	using DB.Contents.Videos;
	using DB.Contents.Creators;

	public class VirtualHoleScraperConsole : MonoBehaviour
	{
		public CreatorObject[] _creatorObjects = null;

		private List<Creator> _creators = new List<Creator>();
		private List<Video> _videos = new List<Video>();

		private ContentClient _contentClient = null;
		private CancellationTokenSource _cts = null;

		private async Task RunAsync(CancellationToken cancellationToken = default)
		{
			MLog.Log("Starting to scrape creator details..");
			while(true) {
				_videos = new List<Video>();
				_videos.AddRange(await _contentClient.videos.ScrapeAsync(_creators, false, cancellationToken));

				Debug.ClearDeveloperConsole();
				Console.Clear();

				GC.Collect();

				MLog.Log($"Finished: [Creators: {_creators.Count}] | [Videos: {_videos.Count}]");
				await Task.Delay(TimeSpan.FromSeconds(10));
			}
		}

		private void Start()
		{
			string proxyListPath = PathUtilities.CreateDataPath("VirtualHoleScraper/config", "proxy-list.txt", PathType.Data);
			string proxyList = File.ReadAllText(proxyListPath);

			ContentClientSettings settings = new ContentClientSettings() {
				connectionString = "mongodb+srv://<username>:<password>@us-east-1-free.41hlb.mongodb.net/test",
				password = "holoverse-editor",
				userName = "RBqYN3ugVTb2stqD",
				proxyPool = new ProxyPool(proxyList)
			};

			_contentClient = new ContentClient(settings);
			_creatorObjects.ForEach(co => _creators.Add(co.AsCreator()));

			CancellationTokenSourceExt.CancelAndCreate(ref _cts);
			Task.Run(() => RunAsync(_cts.Token));
		}
	}
}
