using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Midnight.Concurrency;

namespace VirtualHole.Scraper.Editor
{
	using UEditor = UnityEditor.Editor;

	[CanEditMultipleObjects()]
	[CustomEditor(typeof(CreatorObject), true)]
	public class CreatorObjectEditor : UEditor
	{
		public new CreatorObject target => (CreatorObject)base.target;
		public new IEnumerable<CreatorObject> targets => base.targets.Cast<CreatorObject>();

		private SerializedProperty _isHiddenProp = null;
		private bool _isHiddenOrig = false;

		private List<CreatorObject> GetAllCreatorObjects()
		{
			string[] objGUIDs = AssetDatabase.FindAssets($"t:{nameof(CreatorObject)}");
			return new List<CreatorObject>(
				objGUIDs.Select(objGUID => AssetDatabase.LoadAssetAtPath<CreatorObject>(
					AssetDatabase.GUIDToAssetPath(objGUID)
				)
			));
		}

		private IEnumerable<CreatorObject> GetAffiliations(CreatorObject creatorObj)
		{
			HashSet<CreatorObject> results = new HashSet<CreatorObject>();
			foreach(CreatorObject affliation in Get(creatorObj)) { 
				results.Add(affliation); 
			}
			return results;

			IEnumerable<CreatorObject> Get(CreatorObject child)
			{
				return Enumerable.Concat(
					child.affiliations,
					child.affiliations.SelectMany(a => Get(a))
				);
			}
		}

		private void OnEnable()
		{
			_isHiddenProp = serializedObject.FindProperty("isHidden");
			_isHiddenOrig = _isHiddenProp.boolValue;
		}

		public override void OnInspectorGUI()
		{
			// TODO: Can still improve the group toggling of isHidden for
			// multi-selection, or disabling it if at least one affiliation is
			// already hidden
			using(EditorGUI.ChangeCheckScope ccs1 = new EditorGUI.ChangeCheckScope()) {
				base.OnInspectorGUI();
				
				if(ccs1.changed && _isHiddenProp.boolValue != _isHiddenOrig) {
					_isHiddenOrig = _isHiddenProp.boolValue;

					foreach(CreatorObject creatorObj in GetAllCreatorObjects()) {
						IEnumerable<CreatorObject> affiliations = GetAffiliations(creatorObj);
						if(affiliations.Count() <= 0) { continue; }

						bool isHidden = affiliations.Any(a => a.isHidden);
						bool shouldSetDirty = creatorObj.isHidden != isHidden;
						creatorObj.isHidden = isHidden;

						EditorUtility.SetDirty(creatorObj);
					}

					AssetDatabase.Refresh();
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Helper Methods", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox("In order to use Autofill, universal name and social (youtube) must be filled first.", MessageType.Info);
			if(GUILayout.Button("Autofill Async")) {
				foreach(CreatorObject target in targets) {
					TaskExt.FireForget(target.AutoFillInfoAsync());
					EditorUtility.SetDirty(target);
					AssetDatabase.Refresh();
				}
			}

			if(GUILayout.Button("Autofill From Json")) {
				foreach(CreatorObject target in targets) {
					target.AutoFillFromJson();
					EditorUtility.SetDirty(target);
					AssetDatabase.Refresh();
				} 
			}
		}
	}
}
