using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Holoverse.Scraper.Editor
{
	using UEditor = UnityEditor.Editor;

	[CanEditMultipleObjects()]
	[CustomEditor(typeof(CreatorObject), true)]
	public class CreatorObjectEditor : UEditor
	{
		public new CreatorObject target => (CreatorObject)base.target;
		public new IEnumerable<CreatorObject> targets => base.targets.Cast<CreatorObject>();

		private SerializedProperty _isHiddenProperty = null;
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
			foreach(CreatorObject affliation in Get(creatorObj)) { results.Add(affliation); }
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
			_isHiddenProperty = serializedObject.FindProperty("isHidden");
			_isHiddenOrig = _isHiddenProperty.boolValue;
		}

		public override void OnInspectorGUI()
		{
			// TODO: Can still improve the group toggling of isHidden for
			// multi-selection, or disabling it if at least one affiliation is
			// already hidden
			using(EditorGUI.ChangeCheckScope ccs1 = new EditorGUI.ChangeCheckScope()) {
				base.OnInspectorGUI();
				
				if(ccs1.changed && _isHiddenProperty.boolValue != _isHiddenOrig) {
					_isHiddenOrig = _isHiddenProperty.boolValue;

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

			EditorGUILayout.LabelField("Helper Methods");
			if(GUILayout.Button("Autofill")) {
				foreach(CreatorObject target in targets) {
					target.Editor_AutoFill();
				}
			}
		}
	}
}
