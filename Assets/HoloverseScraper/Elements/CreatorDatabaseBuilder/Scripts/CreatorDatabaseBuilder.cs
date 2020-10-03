using System;
using System.Linq;
using System.Diagnostics;
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

	[CreateAssetMenu(menuName = "Holoverse/Creator Database/Builder")]
	public class CreatorDatabaseBuilder : ScriptableObject
	{
		public UObject creatorObjectsFolderPath => _creatorObjectsFolderPath;
		[SerializeField]
		private UObject _creatorObjectsFolderPath = null;

		public CreatorObject[] toExcludeCreators => _toExcludeCreators;
		[SerializeField]
		private CreatorObject[] _toExcludeCreators = new CreatorObject[0];

		[SerializeField]
		private HoloverseDataClientSettings _dataClientSettings = new HoloverseDataClientSettings();

#if UNITY_EDITOR
		private string editor_creatorsJSONPath => PathUtilities.CreateDataPath("Holoverse", "creators.json", PathType.Data);
		private string editor_videosJSONPath => PathUtilities.CreateDataPath("Holoverse", "videos.json", PathType.Data);

		public void Editor_ExportCreatorsJSON()
		{
			List<CreatorObject> creatorObjs = Editor_GetCreatorObjects();
			Creator[] creators = creatorObjs.Select(obj => obj.ToCreator()).ToArray();
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = editor_creatorsJSONPath
			});
		}

		public void Editor_WriteToCreatorsDB()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				List<CreatorObject> creatorObjs = Editor_GetCreatorObjects();
				Creator[] creators = creatorObjs.Select(obj => obj.ToCreator()).ToArray();

				MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] Start writing creators to database.");

				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				HoloverseDataClient client = new HoloverseDataClient(_dataClientSettings);
				await client.UpsertManyCreatorsAndDeleteDanglingAsync(creators);

				stopwatch.Stop();

				MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] Finished writing creators to database: {stopwatch.Elapsed}.");
			}
		}

		public void Editor_ExportVideosJSON()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] Start scraping videos.");

				Creator[] creators = null;
				JsonUtilities.LoadFromDisk(ref creators, new JsonUtilities.LoadFromDiskParameters {
					filePath = editor_creatorsJSONPath
				});

				YouTubeScraper youtubeScraper = new YouTubeScraper();

				List<Video> videos = new List<Video>();
				foreach(Creator creator in creators) {
					if(creator.isGroup) { continue; }

					// YouTube
					foreach(Social youtube in creator.socials.Where(s => s.platform == Platform.YouTube)) {
						MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] [YouTube: {youtube.name}] Scraping videos...");
						videos.AddRange(await TaskExt.Retry(
							() => youtubeScraper.GetChannelVideos(creator, youtube.url),
							TimeSpan.FromSeconds(3)
						));

						MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] [YouTube: {youtube.name}] Scraping upcoming broadcasts...");
						videos.AddRange(await TaskExt.Retry(
							() => youtubeScraper.GetChannelUpcomingBroadcasts(creator, youtube.url),
							TimeSpan.FromSeconds(3)
						));

						MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] [YouTube: {youtube.name}] Scraping now broadcasts...");
						videos.AddRange(await TaskExt.Retry(
							() => youtubeScraper.GetChannelLiveBroadcasts(creator, youtube.url),
							TimeSpan.FromSeconds(3)
						));
					}
				}

				JsonUtilities.SaveToDisk(videos, new JsonUtilities.SaveToDiskParameters {
					filePath = editor_videosJSONPath,
					jsonSerializerSettings = new JsonSerializerSettings {
						TypeNameHandling = TypeNameHandling.Auto
					}
				});

				MLog.Log($"[{nameof(CreatorDatabaseBuilder)}] Finish scraping videos.");
			}
		}

		public List<CreatorObject> Editor_GetCreatorObjects()
		{
			Assert.IsNotNull(creatorObjectsFolderPath);

			string assetPath = AssetDatabase.GetAssetPath(creatorObjectsFolderPath);
			Assert.IsTrue(AssetDatabase.IsValidFolder(assetPath), $"[{nameof(CreatorDatabaseBuilder)}] '{assetPath}' is not a valid folder.");

			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}", new string[] { assetPath });

			List<CreatorObject> results = new List<CreatorObject>();
			foreach(string objGUID in objGUIDs) {
				string objPath = AssetDatabase.GUIDToAssetPath(objGUID);
				CreatorObject creatorObj = AssetDatabase.LoadAssetAtPath<CreatorObject>(objPath);
				if(creatorObj != null && !Array.Exists(toExcludeCreators, e => e == creatorObj)) {
					results.Add(creatorObj);
				}
			}

			return results;
		}
#endif
	}
}
