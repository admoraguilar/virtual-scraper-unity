using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Midnight;

namespace VirtualHole.Scraper
{
	using DB.Contents.Videos;
	using DB.Contents.Creators;
	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "VirtualHole/DB/Content Client Object")]
	public class ContentClientObject : ScriptableObject
	{
		public string proxyListTxtPath
		{
			get => PathUtilities.CreateDataPath("VirtualHoleScraper/config", "proxy-list.txt", PathType.Data);
		}

		[SerializeField]
		public string _connectionString = string.Empty;

		[SerializeField]
		public string _userName = string.Empty;

		[SerializeField]
		public string _password = string.Empty;

		public bool isUseProxy
		{
			get => _isUseProxy;
			set {
				_isUseProxy = value;
				client.isUseProxy = _isUseProxy;
			}
		}
		[Space]
		[SerializeField]
		private bool _isUseProxy = false;

		private ContentClient client => Get();
		private ContentClient _client = null;

		public ContentClient Get()
		{
			if(_client != null) { return _client; }

			_client = new ContentClient(new ContentBuilderSettings() {
				connectionString = _connectionString,
				userName = _userName,
				password = _password,
				proxyPool = new ProxyPool()
			});

			string proxyListText = File.ReadAllText(proxyListTxtPath);
			if(!string.IsNullOrEmpty(proxyListText)) {
				_client.SetProxies(proxyListText);
				_client.isUseProxy = isUseProxy;
			} else {
				_client.isUseProxy = false;
			}

			return _client;
		}

		public void SaveCreatorsToJson(IEnumerable<Creator> creators)
		{
			client.creators.SaveToJson(creators);
		}

		public async Task WriteToCreatorsDBAsync(
			IEnumerable<Creator> creators, CancellationToken cancellationToken = default)
		{
			await client.creators.WriteToDBAsync(creators, cancellationToken);
		}

		public async Task WriteToVideosJsonUsingCreatorsJsonAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			IEnumerable<Creator> creators = client.creators.LoadFromJson();
			List<Video> videos = await client.videos.ScrapeAsync(creators, incremental, cancellationToken);
			client.videos.SaveToJson(videos);
		}

		public async Task WriteToVideosDBUsingCreatorsDBAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			IEnumerable<Creator> creators = await client.creators.GetAllFromDBAsync(cancellationToken);

			MLog.Log($"Found creators: {creators.Count()}");
			List<Video> videos = new List<Video>();
			videos.AddRange(await client.videos.ScrapeAsync(creators, incremental, cancellationToken));

			if(videos.Count > 0) {
				await client.videos.WriteToDBAsync(videos, incremental, cancellationToken);
			}
		}

		public async Task WriteToVideosDBUsingJsonAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			IEnumerable<Video> videos = client.videos.LoadFromJson();
			await client.videos.WriteToDBAsync(videos, incremental, cancellationToken);
		}
				
#if UNITY_EDITOR
		public enum Editor_CreatorObjectsListMode
		{
			Include,
			Exclude
		};

		public UObject editor_creatorObjectsFolderPath => _editor_creatorObjectsFolderPath;
		[Header("Editor")]
		[SerializeField]
		private UObject _editor_creatorObjectsFolderPath = null;

		public CreatorObject[] editor_creatorsList => _editor_creatorsList;
		[SerializeField]
		private CreatorObject[] _editor_creatorsList = new CreatorObject[0];

		public Editor_CreatorObjectsListMode editor_creatorListMode => _editor_creatorListMode;
		[SerializeField]
		private Editor_CreatorObjectsListMode _editor_creatorListMode = Editor_CreatorObjectsListMode.Include;

		private void OnValidate()
		{
			isUseProxy = _isUseProxy;
		}
#endif
	}
}
