using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Midnight;
using Midnight.Concurrency;
using System.Security.Cryptography;

namespace Holoverse.Scraper
{
	using UEditor = UnityEditor.Editor;

	[CustomEditor(typeof(ContentDatabaseClientObject), true)]
	public class ContentDatabaseClientObjectEditor : UEditor
	{
		public new ContentDatabaseClientObject target => (ContentDatabaseClientObject)base.target;

		private const string _updateCreatorsMetricKey = "Update Creator Details";
		private const string _writeCreatorMetricKey = "Write Creator";
		private const string _writeVideosMetricKey = "Write Videos";
		private const string _writeVideosUsingLocalMetricKey = "Write Videos Using Local";
		private Dictionary<string, string> _metrics = new Dictionary<string, string> {
			{ _updateCreatorsMetricKey, "-" },
			{ _writeCreatorMetricKey, "-" },
			{ _writeVideosMetricKey, "-" },
			{ _writeVideosUsingLocalMetricKey, "-" }
		};

		private void DrawHelperOperators()
		{
			EditorGUILayout.LabelField("Helpers");
			if(GUILayout.Button("Update Creator Objects Details")) { UpdateCreatorObjectsDetails(); }
		}

		private void DrawCreatorsCollectionOperators()
		{
			EditorGUILayout.LabelField("creators.json");
			if(GUILayout.Button("Export To Local JSON")) { ExportCreatorsJSON(); }
			if(GUILayout.Button("Write To Creators Collection")) { WriteToCreatorsCollection(); }
		}

		private void DrawVideosCollectionOperators()
		{
			EditorGUILayout.LabelField("videos.json");
			if(GUILayout.Button("Export Videos Using Local Creators JSON")) { ExportVideosUsingLocalCreatorsJSON(); }
			if(GUILayout.Button("Write To Videos Collection Using Local JSON")) { WriteToVideosCollectionUsingLocalJSON(); }
			if(GUILayout.Button("Write To Videos Collection")) { WriteToVideosCollection(); }
		}

		private void DrawMetrics()
		{
			EditorGUILayout.LabelField("Metrics");
			using(new EditorGUILayout.VerticalScope()) {
				foreach(KeyValuePair<string, string> kvp in _metrics) {
					using(new EditorGUILayout.HorizontalScope()) {
						EditorGUILayout.PrefixLabel($"{kvp.Key}: ");
						EditorGUILayout.LabelField($"{kvp.Value}");
					}
				}
			}
		}

		public void UpdateCreatorObjectsDetails()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				List<CreatorObject> creatorObjs = GetCreatorObjects();

				using(StopwatchScope stopwatch = new StopwatchScope()) {
					using(ProgressScope progress = new ProgressScope(
						new ProgressScope.Parameters {
							name = "Updating all creator objects"
						})) 
					{
						int index = 0;
						foreach(CreatorObject creatorObj in creatorObjs) {
							progress.Report((float)index / creatorObjs.Count, $"Updating {creatorObj.universalName}...");
							await creatorObj.UpdateAsync();
							EditorUtility.SetDirty(creatorObj);
							index++;
						}

						AssetDatabase.Refresh();
						EditorPrefs.SetString(
							_updateCreatorsMetricKey, 
							_metrics[_updateCreatorsMetricKey] = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}"
						);
					}
				}	
			}
		}

		public void ExportCreatorsJSON()
		{
			using(StopwatchScope stopwatch = new StopwatchScope()) {
				target.client.ExportCreatorsJSON(GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray());
				_metrics["Export Creators to JSON"] = stopwatch.elapsed.Duration().ToString();
			}
		}

		public void WriteToCreatorsCollection()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				using(StopwatchScope stopwatch = new StopwatchScope()) {
					using(ProgressScope progress = new ProgressScope(
						new ProgressScope.Parameters {
							name = "Writing to creators collection",
							description = $"Running...",
							options = Progress.Options.Indefinite
						}))
					{
						progress.Report(.8f);
						await target.client.WriteToCreatorsCollectionAsync(GetCreatorObjects().Select(obj => obj.ToCreator()).ToArray());
						EditorPrefs.SetString(
							_writeCreatorMetricKey, 
							_metrics[_writeCreatorMetricKey] = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}"
						);
					}
				}
			}
		}

		public void ExportVideosUsingLocalCreatorsJSON()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				using(StopwatchScope stopwatch = new StopwatchScope()) {
					using(ProgressScope progress = new ProgressScope(
						new ProgressScope.Parameters {
							name = "Export videos using local creators JSON",
							description = $"Running...",
							options = Progress.Options.Indefinite
						})) {
						progress.Report(.8f);
						await target.client.ExportVideosUsingLocalCreatorsJSONAsync();
						_metrics["Export Videos to JSON"] = stopwatch.elapsed.Duration().ToString();
					}
				}
			}
		}

		public void WriteToVideosCollectionUsingLocalJSON()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				using(StopwatchScope stopwatch = new StopwatchScope()) {
					using(ProgressScope progress = new ProgressScope(
						new ProgressScope.Parameters {
							name = "Writing to videos collection using local creators JSON",
							description = $"Running...",
							options = Progress.Options.Indefinite
						})) {
						progress.Report(.8f);
						await target.client.WriteToVideosCollectionUsingLocalJson();
						EditorPrefs.SetString(
							_writeVideosUsingLocalMetricKey,
							_metrics[_writeVideosUsingLocalMetricKey] = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}"
						);
					}
				}
			}
		}

		public void WriteToVideosCollection()
		{
			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				using(StopwatchScope stopwatch = new StopwatchScope()) {
					using(ProgressScope progress = new ProgressScope(
						new ProgressScope.Parameters {
							name = "Writing to videos collection",
							description = $"Running...",
							options = Progress.Options.Indefinite
						})) 
					{
						float step = 0f;

						target.client.onScrapeVideosProgressDetail += UpdateProgress;
						target.client.onGetAndWriteToVideosCollectionFromCreatorsCollectionDetail += UpdateProgress;
						await target.client.GetAndWriteToVideosCollectionFromCreatorsCollection();
						EditorPrefs.SetString(
							_writeVideosMetricKey, 
							_metrics[_writeVideosMetricKey] = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}"
						);

						void UpdateProgress(string description)
						{
							progress.Report(step, description);
							step = step > 1f ? 0f : step + .1f;
						}
					}
				}
			}
		}

		private List<CreatorObject> GetCreatorObjects()
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
				if(creatorObj == null) { continue; }

				if(target.editor_creatorsList.Length > 0) {
					switch(target.editor_creatorListMode) {
						case ContentDatabaseClientObject.Editor_CreatorObjectsListMode.Include:
							if(Array.Exists(target.editor_creatorsList, e => e == creatorObj)) {
								results.Add(creatorObj);
							}
							break;
						case ContentDatabaseClientObject.Editor_CreatorObjectsListMode.Exclude:
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

		private void OnEnable()
		{
			foreach(string key in _metrics.Keys.ToArray()) {
				_metrics[key] = EditorPrefs.GetString(key);
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Operators", EditorStyles.boldLabel);

			DrawHelperOperators();
			EditorGUILayout.Space();
			DrawCreatorsCollectionOperators();
			EditorGUILayout.Space();
			DrawVideosCollectionOperators();

			EditorGUILayout.Space();
			DrawMetrics();
		}
	}
}
