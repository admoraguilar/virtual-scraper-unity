using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Midnight;

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
		[SerializeField]
		private UObject _creatorObjectsFolderPath = null;

		[SerializeField]
		private List<CreatorObject> _toExcludeCreators = new List<CreatorObject>();

#if UNITY_EDITOR
		private List<CreatorObject> GetCreatorObjects()
		{
			Assert.IsNotNull(_creatorObjectsFolderPath);

			string assetPath = AssetDatabase.GetAssetPath(_creatorObjectsFolderPath);
			Assert.IsTrue(AssetDatabase.IsValidFolder(assetPath), $"[{nameof(CreatorDatabaseBuilder)}] '{assetPath}' is not a valid folder.");

			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}", new string[] { assetPath });

			List<CreatorObject> results = new List<CreatorObject>();
			foreach(string objGUID in objGUIDs) {
				string objPath = AssetDatabase.GUIDToAssetPath(objGUID);
				CreatorObject creatorObj = AssetDatabase.LoadAssetAtPath<CreatorObject>(objPath);
				if(creatorObj != null && !_toExcludeCreators.Contains(creatorObj)) { 
					results.Add(creatorObj);
				}
			}

			return results;
		}

		[MenuItem("CONTEXT/CreatorDatabaseBuilder/Export to JSON")]
		private static void ExportToJSON(MenuCommand command)
		{
			CreatorDatabaseBuilder builder = (CreatorDatabaseBuilder)command.context;

			List<CreatorObject> creatorObjs = builder.GetCreatorObjects();
			Creator[] creators = creatorObjs.Select(obj => obj.ToCreator()).ToArray();
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = PathUtilities.CreateDataPath("Holoverse", "creators.json", PathType.Data)
			});
		}
#endif
	}
}
