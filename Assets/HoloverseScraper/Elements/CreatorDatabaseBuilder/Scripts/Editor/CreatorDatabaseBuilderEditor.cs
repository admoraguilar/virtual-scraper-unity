using UnityEngine;
using UnityEditor;

namespace Holoverse.Scraper
{
	using UEditor = UnityEditor.Editor;

	[CustomEditor(typeof(CreatorDatabaseBuilder), true)]
	public class CreatorDatabaseBuilderEditor : UEditor
	{
		public new CreatorDatabaseBuilder target => (CreatorDatabaseBuilder)base.target;

		private void DrawCreatorsCollectionOperators()
		{
			EditorGUILayout.LabelField("creators.json");
			if(GUILayout.Button("Export To Local JSON")) { target.Editor_ExportCreatorsJSON(); }
			if(GUILayout.Button("Write To Database")) { target.Editor_WriteToCreatorsDB(); }
		}

		private void DrawVideosCollectionOperators()
		{
			EditorGUILayout.LabelField("videos.json");
			if(GUILayout.Button("Export To Local JSON")) { target.Editor_ExportVideosJSON(); }
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
