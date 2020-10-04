using System.Threading;
using UnityEditor;
using UnityEngine;

// Shows a progress bar for the given number of seconds.
public class EditorUtilityDisplayProgressBar : EditorWindow
{
	public float secs = 5f;
	[MenuItem("Examples/Progress Bar Usage")]
	static void Init()
	{
		var window = GetWindow(typeof(EditorUtilityDisplayProgressBar));
		window.Show();
	}

	void OnGUI()
	{
		secs = EditorGUILayout.Slider("Time to wait:", secs, 1.0f, 20.0f);
		if(GUILayout.Button("Display bar")) {
			var step = 0.1f;
			for(float t = 0; t < secs; t += step) {
				EditorUtility.DisplayProgressBar("Simple Progress Bar", "Doing some work...", t / secs);
				// Normally some actual computation would be here;
				// for this example just sleep.
				Thread.Sleep((int)(step * 1000.0f));
			}
			EditorUtility.ClearProgressBar();
		}
	}
}