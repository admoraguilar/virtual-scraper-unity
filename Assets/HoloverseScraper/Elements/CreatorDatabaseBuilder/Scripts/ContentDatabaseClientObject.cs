using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "Holoverse/Content Database/Client Object")]
	public class ContentDatabaseClientObject : ScriptableObject
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

		private ContentDatabaseClient client 
		{
			get 
			{
				if(_client == null) {
					_client = new ContentDatabaseClient(
						_connectionString, _userName,
						_password);

					_client.isUseProxy = isUseProxy;
					_client.SetProxies(proxyList);
				}

				return _client;
			}	
		}
		private ContentDatabaseClient _client = null;

		public void ExportCreatorsJSON(IEnumerable<Creator> creators) => 
			client.ExportCreatorsJSON(creators);

		public async Task WriteToCreatorsCollectionAsync(
			IEnumerable<Creator> creators, CancellationToken cancellationToken = default) =>
				await client.WriteToCreatorsCollectionAsync(creators, cancellationToken);

		public async Task ExportVideosUsingLocalCreatorsJSONAsync(
			bool incremental = false, CancellationToken cancellationToken = default) =>
				await client.ExportVideosUsingLocalCreatorsJSONAsync(incremental, cancellationToken);

		public async Task WriteToVideosCollectionUsingLocalJson(
			bool incremental = false, CancellationToken cancellationToken = default) =>
				await client.WriteToVideosCollectionUsingLocalJson(incremental, cancellationToken);

		public async Task GetAndWriteToVideosCollectionFromCreatorsCollection(
			bool incremental = false, CancellationToken cancellationToken = default) =>
				await client.GetAndWriteToVideosCollectionFromCreatorsCollection(incremental, cancellationToken);

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
#endif
	}
}
