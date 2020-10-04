using UnityEngine;
using UnityEditor;

namespace Holoverse.Scraper
{
	using UEditor = UnityEditor.Editor;

	[CustomEditor(typeof(ContentDatabaseClientObject), true)]
	public class ContentDatabaseClientObjectEditor : UEditor
	{
		public new ContentDatabaseClientObject target => (ContentDatabaseClientObject)base.target;

		private void DrawCreatorsCollectionOperators()
		{
			EditorGUILayout.LabelField("creators.json");
			if(GUILayout.Button("Export To Local JSON")) { target.Editor_ExportCreatorsJSON(); }
			if(GUILayout.Button("Write To Creators Collection")) { target.Editor_WriteToCreatorsCollection(); }
		}

		private void DrawVideosCollectionOperators()
		{
			EditorGUILayout.LabelField("videos.json");
			if(GUILayout.Button("Export Videos Using Local Creators JSON")) { target.Editor_ExportVideosUsingLocalCreatorsJSON(); }
			if(GUILayout.Button("Write To Videos Collection")) { target.Editor_WriteToVideosCollection(); }
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Operators", EditorStyles.boldLabel);
			
			DrawCreatorsCollectionOperators();
			EditorGUILayout.Space();
			DrawVideosCollectionOperators();
		}
	}
}
