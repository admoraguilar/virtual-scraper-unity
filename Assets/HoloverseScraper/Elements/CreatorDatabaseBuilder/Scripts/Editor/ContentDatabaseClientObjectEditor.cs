using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Midnight.Concurrency;
using Midnight;

namespace Holoverse.Scraper
{
	using UEditor = UnityEditor.Editor;

	[CustomEditor(typeof(ContentDatabaseClientObject), true)]
	public class ContentDatabaseClientObjectEditor : UEditor
	{
		public new ContentDatabaseClientObject target => (ContentDatabaseClientObject)base.target;

		private void DrawHelperOperators()
		{
			EditorGUILayout.LabelField("Helpers");
			if(GUILayout.Button("Update Creator Objects Details")) { Editor_UpdateCreatorObjectsDetails(); }
		}

		private void DrawCreatorsCollectionOperators()
		{
			EditorGUILayout.LabelField("creators.json");
			if(GUILayout.Button("Export To Local JSON")) { Editor_ExportCreatorsJSON(); }
			if(GUILayout.Button("Write To Creators Collection")) { Editor_WriteToCreatorsCollection(); }
		}

		private void DrawVideosCollectionOperators()
		{
			EditorGUILayout.LabelField("videos.json");
			if(GUILayout.Button("Export Videos Using Local Creators JSON")) { Editor_ExportVideosUsingLocalCreatorsJSON(); }
			if(GUILayout.Button("Write To Videos Collection")) { Editor_WriteToVideosCollection(); }
		}

		public void Editor_UpdateCreatorObjectsDetails()
		{
			List<CreatorObject> creatorObjs = Editor_GetCreatorObjects();

			int index = 0;
			foreach(CreatorObject creatorObj in creatorObjs) {
				EditorUtility.DisplayProgressBar(
					"Updating all creator objects",
					$"Updating {creatorObj.universalName}...",
					(float)index / creatorObjs.Count
				);
				TaskExt.RunSync(() => creatorObj.UpdateAsync());
				EditorUtility.SetDirty(creatorObj);
				index++;
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
		}

		public void Editor_ExportCreatorsJSON()
		{
			target.ExportCreatorsJSON(Editor_GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray());
		}

		public void Editor_WriteToCreatorsCollection()
		{
			TaskExt.FireForget(target.WriteToCreatorsCollectionAsync(Editor_GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray()));
		}

		public void Editor_ExportVideosUsingLocalCreatorsJSON()
		{
			TaskExt.FireForget(target.ExportVideosUsingLocalCreatorsJSONAsync());
		}

		public void Editor_WriteToVideosCollection()
		{
			TaskExt.FireForget(target.WriteToVideosCollectionAsync());
		}

		private List<CreatorObject> Editor_GetCreatorObjects()
		{
			Assert.IsNotNull(target.editor_creatorObjectsFolderPath);

			string assetPath = AssetDatabase.GetAssetPath(target.editor_creatorObjectsFolderPath);
			Assert.IsTrue(AssetDatabase.IsValidFolder(assetPath), $"[{nameof(ContentDatabaseClientObject)}] '{assetPath}' is not a valid folder.");

			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}", new string[] { assetPath });

			List<CreatorObject> results = new List<CreatorObject>();
			foreach(string objGUID in objGUIDs) {
				string objPath = AssetDatabase.GUIDToAssetPath(objGUID);
				CreatorObject creatorObj = AssetDatabase.LoadAssetAtPath<CreatorObject>(objPath);
				if(creatorObj != null && !Array.Exists(target.editor_toExcludeCreators, e => e == creatorObj)) {
					results.Add(creatorObj);
				}
			}

			return results;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Operators", EditorStyles.boldLabel);

			EditorGUILayout.Space();
			DrawHelperOperators();
			EditorGUILayout.Space();
			DrawCreatorsCollectionOperators();
			EditorGUILayout.Space();
			DrawVideosCollectionOperators();
		}
	}
}
