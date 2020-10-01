using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using Newtonsoft.Json;

namespace Holoverse.Scraper
{
	using Api.Data.YouTube;

	public class TestLoader : MonoBehaviour
	{
		[Header("Single")]
		public TextAsset videosJson = null;

		[Header("Multiple")]
		public TextAsset[] videoJsons = null;

		[Header("Debug")]
		public List<Video> loadedVideos = new List<Video>();

		[ContextMenu("Load Single")]
		public void Load()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			loadedVideos.AddRange(LoadVideoInfo(videosJson.text, -1));

			stopwatch.Stop();
			MLog.Log($"Load Single: {stopwatch.Elapsed}");
		}

		[ContextMenu("Load Multiple")]
		public void LoadMultiple()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			foreach(TextAsset videoJson in videoJsons) {
				loadedVideos.AddRange(LoadVideoInfo(videoJson.text, 50));
			}

			stopwatch.Stop();
			MLog.Log($"Load Multiple: {stopwatch.Elapsed}");
		}

		private IEnumerable<Video> LoadVideoInfo(string json, int amount)
		{
			List<Video> result = new List<Video>();

			byte[] byteArray = Encoding.UTF8.GetBytes(json);
			using(MemoryStream ms = new MemoryStream(byteArray)) {
				using(StreamReader sr = new StreamReader(ms)) {
					using(JsonReader reader = new JsonTextReader(sr)) {
						JsonSerializer serializer = new JsonSerializer();
						int count = 0;

						while(reader.Read()) {
							if(amount >= 0 && count >= amount) { break; }

							if(reader.TokenType == JsonToken.StartObject) {
								Video video = serializer.Deserialize<Video>(reader);
								if(video != null) {
									result.Add(video);
									count++;
								}
							}
						}
					}
				}
			}

			return result;
		}
	}
}
