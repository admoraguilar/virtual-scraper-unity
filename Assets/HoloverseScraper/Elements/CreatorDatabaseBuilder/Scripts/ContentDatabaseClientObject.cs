using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Newtonsoft.Json;
using Midnight;
using Midnight.Concurrency;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
		[Header("Editor")]
		[SerializeField]
		private UObject _editor_creatorObjectsFolderPath = null;

		[SerializeField]
		private CreatorObject[] _editor_toExcludeCreators = new CreatorObject[0];

		public void Editor_ExportCreatorsJSON()
		{
			ExportCreatorsJSON(Editor_GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray());
		}

		public void Editor_WriteToCreatorsCollection()
		{
			TaskExt.FireForget(WriteToCreatorsCollectionAsync(Editor_GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray()));
		}

		public void Editor_ExportVideosUsingLocalCreatorsJSON()
		{
			TaskExt.FireForget(ExportVideosUsingLocalCreatorsJSONAsync());
		}

		public void Editor_WriteToVideosCollection()
		{
			TaskExt.FireForget(WriteToVideosCollectionAsync());
		}

		public List<CreatorObject> Editor_GetCreatorObjects()
		{
			Assert.IsNotNull(_editor_creatorObjectsFolderPath);

			string assetPath = AssetDatabase.GetAssetPath(_editor_creatorObjectsFolderPath);
			Assert.IsTrue(AssetDatabase.IsValidFolder(assetPath), $"[{nameof(ContentDatabaseClientObject)}] '{assetPath}' is not a valid folder.");

			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}", new string[] { assetPath });

			List<CreatorObject> results = new List<CreatorObject>();
			foreach(string objGUID in objGUIDs) {
				string objPath = AssetDatabase.GUIDToAssetPath(objGUID);
				CreatorObject creatorObj = AssetDatabase.LoadAssetAtPath<CreatorObject>(objPath);
				if(creatorObj != null && !Array.Exists(_editor_toExcludeCreators, e => e == creatorObj)) {
					results.Add(creatorObj);
				}
			}

			return results;
		}
#endif
	}
}
