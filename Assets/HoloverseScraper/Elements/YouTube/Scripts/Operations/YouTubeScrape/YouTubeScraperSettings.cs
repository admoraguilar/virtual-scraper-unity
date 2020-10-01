using System.Threading.Tasks;
using UnityEngine;
using Midnight.Concurrency;
using Midnight;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Holoverse.Scraper
{
	[CreateAssetMenu(menuName = "Holoverse/Data/YouTube/Scraper Settings")]
	public partial class YouTubeScraperSettings : ScriptableObject
	{
		[SerializeField]
		private YouTubeScrapeOperation.Settings _settings = new YouTubeScrapeOperation.Settings();

		private async Task Execute()
		{
			MLog.Log("=====START SCRAPING====="); 

			YouTubeScrapeOperation operation = new YouTubeScrapeOperation(_settings);
			await operation.Execute();
			operation.Save();

			MLog.Log("=====END SCRAPING=====");
		}

#if UNITY_EDITOR
		[MenuItem("CONTEXT/YouTubeScraperSettings/Execute Locally")]
		private static void TestExecute(MenuCommand command)
		{
			YouTubeScraperSettings settings = (YouTubeScraperSettings)command.context;
			TaskExt.FireForget(settings.Execute());
		}

		[MenuItem("CONTEXT/YouTubeScraperSettings/Export to JSON")]
		private static void ExportToJSON(MenuCommand command)
		{
			YouTubeScraperSettings settings = (YouTubeScraperSettings)command.context;
			JsonUtilities.SaveToDisk(settings._settings, new JsonUtilities.SaveToDiskParameters {
				filePath = PathUtilities.CreateDataPath("Settings", "YouTubeScraperSettings.json", PathType.Data)
			});
		}
#endif
	}
}
