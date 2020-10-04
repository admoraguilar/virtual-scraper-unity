using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Midnight;

namespace Holoverse.Scraper
{
	using Api.Data;

	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "Holoverse/Content Database/Client Object")]
	public class ContentDatabaseClientObject : ScriptableObject
	{
		private string creatorsJsonPath => PathUtilities.CreateDataPath("Holoverse", "creators.json", PathType.Data);
		private string videosJSONPath => PathUtilities.CreateDataPath("Holoverse", "videos.json", PathType.Data);

		[SerializeField]
		private HoloverseDataClientSettings _dataClientSettings = new HoloverseDataClientSettings();

		private ContentDatabaseClient contentDBClient =>
			_contentDBClient == null ? _contentDBClient = new ContentDatabaseClient(_dataClientSettings) :
			_contentDBClient;
		private ContentDatabaseClient _contentDBClient = null;

		public void ExportCreatorsJSON(IEnumerable<Creator> creators)
		{
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = creatorsJsonPath
			});
		}

		public async Task WriteToCreatorsCollectionAsync(IEnumerable<Creator> creators)
		{
			await contentDBClient.WriteToCreatorsCollectionAsync(creators);
		}

		public async Task ExportVideosUsingLocalCreatorsJSONAsync()
		{
			MLog.Log($"[{nameof(ContentDatabaseClientObject)}] Start scraping videos.");

			Creator[] creators = null;
			JsonUtilities.LoadFromDisk(ref creators, new JsonUtilities.LoadFromDiskParameters {
				filePath = creatorsJsonPath
			});

			List<Video> videos = await contentDBClient.ScrapeVideosAsync(creators);

			JsonUtilities.SaveToDisk(videos, new JsonUtilities.SaveToDiskParameters {
				filePath = videosJSONPath,
				jsonSerializerSettings = new JsonSerializerSettings {
					TypeNameHandling = TypeNameHandling.Auto
				}
			});

			MLog.Log($"[{nameof(ContentDatabaseClientObject)}] Finish scraping videos.");
		}

		public async Task WriteToVideosCollectionAsync()
		{
			await contentDBClient.WriteToVideosCollectionAsync();
		}

#if UNITY_EDITOR
		public UObject editor_creatorObjectsFolderPath => _editor_creatorObjectsFolderPath;
		[Header("Editor")]
		[SerializeField]
		private UObject _editor_creatorObjectsFolderPath = null;

		public CreatorObject[] editor_toExcludeCreators => _editor_toExcludeCreators;
		[SerializeField]
		private CreatorObject[] _editor_toExcludeCreators = new CreatorObject[0];
#endif
	}
}
