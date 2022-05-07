using UnityEngine;

namespace VirtualHole.Scraper
{
	public class ConsoleFramerateHandler : MonoBehaviour
	{
		public int targetFramerate = 1;

		private void Start()
		{
			if(!Application.isEditor) {
				// Setting idle frame rate because this is headless and 
				// we don't really need to run at 60 FPS.
				// https://forum.unity.com/threads/headless-server-high-cpu-usage.441727/#post-2858772
				Application.targetFrameRate = targetFramerate;
			}
		}
	}
}
