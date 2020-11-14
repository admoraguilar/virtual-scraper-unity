using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Midnight.Concurrency;

namespace VirtualHole.Scraper
{
	using UEditor = UnityEditor.Editor;

	[CustomEditor(typeof(ContentClientObject), true)]
	public class ContentBuilderObjectEditor : UEditor
	{
		public new ContentClientObject target => (ContentClientObject)base.target;

		private bool _isRunning = false;
		private CancellationTokenSource _cts = null;

		private void DrawHelperOperators()
		{
			EditorGUILayout.LabelField("Helpers");
			if(GUILayout.Button("Autofill Creater Object Infos")) { AutoFillCreaterObjectInfos(); }
			if(GUILayout.Button("Autofill Creator Object Infos From Json")) { AutofillCreatorObjectInfosFromJson(); }
		}

		private void DrawCreatorsCollectionOperators()
		{
			EditorGUILayout.LabelField("Creators");
			if(GUILayout.Button("Save Creators To JSON")) { SaveCreatorsToJson(); }
			if(GUILayout.Button("Write To Creators DB")) { WriteToCreatorsDB(); }
		}

		private void DrawVideosCollectionOperators()
		{
			EditorGUILayout.LabelField("Videos");
			if(GUILayout.Button("[Full] Write to Json Using Creators JSON")) { WriteToVideosJsonUsingCreatorsJson(); }
			if(GUILayout.Button("[Incremental] Write to Json Using Creators JSON")) { WriteToVideosJsonUsingCreatorsJson(true); }

			EditorGUILayout.Space();
			if(GUILayout.Button("Write to DB Using Json")) { WriteToVideosDBUsingJson(); }
			if(GUILayout.Button("[Full] Write to DB Using Creators DB")) { WriteToVideosDBUsingCreatorsDB(); }
			if(GUILayout.Button("[Incremental Write to DB Using Creators DB")) { WriteToVideosDBUsingCreatorsDB(true); }
		}

		private void DrawCommands()
		{
			EditorGUILayout.LabelField("Commands");
			if(GUILayout.Button("Cancel")) { CancellationTokenSourceFactory.CancelToken(ref _cts); }
		}

		public void AutoFillCreaterObjectInfos()
		{
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				List<CreatorObject> creatorObjs = GetCreatorObjects();
				await Concurrent.ForEachAsync(creatorObjs, Process, 5, cancellationToken);
				AssetDatabase.Refresh();

				async Task Process(CreatorObject creatorObj)
				{
					await creatorObj.AutoFillInfoAsync();
					EditorUtility.SetDirty(creatorObj);
				}
			}
		}

		public void AutofillCreatorObjectInfosFromJson()
		{
			List<CreatorObject> creatorObjs = GetCreatorObjects();

			foreach(CreatorObject creatorObj in creatorObjs) {
				creatorObj.AutoFillFromJson();
				EditorUtility.SetDirty(creatorObj);
			}

			AssetDatabase.Refresh();
		}

		public void SaveCreatorsToJson()
		{
			target.SaveCreatorsToJson(GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray());
		}

		public void WriteToCreatorsDB()
		{
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				await target.WriteToCreatorsDBAsync(
					GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray(),
					cancellationToken);
			}
		}

		public void WriteToVideosJsonUsingCreatorsJson(bool incremental = false)
		{
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				await target.WriteToVideosJsonUsingCreatorsJsonAsync(incremental, cancellationToken);
			}
		}

		public void WriteToVideosDBUsingJson(bool incremetal = false)
		{
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				await target.WriteToVideosDBUsingJsonAsync(incremetal, cancellationToken);
			}
		}

		public void WriteToVideosDBUsingCreatorsDB(bool incremental = false)
		{
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				await target.WriteToVideosDBUsingCreatorsDBAsync(incremental, cancellationToken);
			}
		}

		private List<CreatorObject> GetCreatorObjects()
		{
			Assert.IsNotNull(target.editor_creatorObjectsFolderPath);

			string assetPath = AssetDatabase.GetAssetPath(target.editor_creatorObjectsFolderPath);
			Assert.IsTrue(AssetDatabase.IsValidFolder(assetPath), $"[{nameof(ContentClientObject)}] '{assetPath}' is not a valid folder.");

			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}", new string[] { assetPath });

			List<CreatorObject> results = new List<CreatorObject>();

			foreach(string objGUID in objGUIDs) {
				string objPath = AssetDatabase.GUIDToAssetPath(objGUID);
				CreatorObject creatorObj = AssetDatabase.LoadAssetAtPath<CreatorObject>(objPath);
				if(creatorObj == null) { continue; }

				if(target.editor_creatorsList.Length > 0) {
					switch(target.editor_creatorListMode) {
						case ContentClientObject.Editor_CreatorObjectsListMode.Include:
							if(Array.Exists(target.editor_creatorsList, e => e == creatorObj)) {
								results.Add(creatorObj);
							}
							break;
						case ContentClientObject.Editor_CreatorObjectsListMode.Exclude:
							if(!Array.Exists(target.editor_creatorsList, e => e == creatorObj)) {
								results.Add(creatorObj);
							}
							break;
					}
				} else {
					results.Add(creatorObj);
				}
			}

			return results;
		}

		private void RunTask(Func<CancellationToken, Task> taskFactory)
		{
			CancellationTokenSourceFactory.CancelAndCreateCancellationTokenSource(ref _cts);

			_isRunning = true;
			TaskExt.FireForget(Execute(), (Exception e) => { _isRunning = false; });

			async Task Execute()
			{
				await taskFactory(_cts.Token);
				_isRunning = false;
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Operators", EditorStyles.boldLabel);

			using(new EditorGUI.DisabledGroupScope(_isRunning)) {
				DrawHelperOperators();
				EditorGUILayout.Space();
				DrawCreatorsCollectionOperators();
				EditorGUILayout.Space();
				DrawVideosCollectionOperators();
			}

			EditorGUILayout.Space();
			DrawCommands();
		}
	}
}
