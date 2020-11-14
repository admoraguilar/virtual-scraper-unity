using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualHole.Scraper
{
	using DB.Contents.Videos;
	using DB.Contents.Creators;
	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "VirtualHole/DB Builder/Content Builder Object")]
	public class ContentClientObject : ScriptableObject
	{
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
		[SerializeField]
		private bool _isUseProxy = false;

		public string proxyList
		{
			get => _proxyList;
			set {
				_proxyList = value;
				client.SetProxies(_proxyList);
			}
		}
		[TextArea(5, 5)]
		[SerializeField]
		private string _proxyList = string.Empty;

		private ContentClient client
		{
			get {
				if(_client == null) {
					_client = new ContentClient(
						new ContentBuilderSettings() {
							connectionString = _connectionString,
							userName = _userName,
							password = _password,
							proxyPool = new ProxyPool(proxyList)
						});
				}

				return _client;
			}
		}
		private ContentClient _client = null;

		public void ExportCreatorsJSON(IEnumerable<Creator> creators)
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
			await client.videos.ScrapeAsync(creators, incremental, cancellationToken);
		}

		public async Task WriteToVideosDBUsingCreatorsDBAsync(
			bool incremental = false, CancellationToken cancellationToken = default)
		{
			IEnumerable<Creator> creators = await client.creators.GetAllFromDBAsync(cancellationToken);

			List<Video> videos = new List<Video>();
			videos.AddRange(await client.videos.ScrapeAsync(creators, incremental, cancellationToken));

			await client.videos.WriteToDBAsync(videos, incremental, cancellationToken);
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
			proxyList = _proxyList;
		}
#endif
	}
}
