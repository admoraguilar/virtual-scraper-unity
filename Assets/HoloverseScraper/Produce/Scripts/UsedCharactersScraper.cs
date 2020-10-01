using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using System.Linq;

namespace Holoverse.Scraper
{
	public class UsedCharactersScraper : MonoBehaviour
	{
		private HashSet<char> _usedCharactersSet = new HashSet<char>();

		[ContextMenu("Run")]
		public void Run()
		{
			Debug.Log($"Start used characters scraper.");
			string folderPath = PathUtilities.CreateDataPath("HoloverseScraper", string.Empty, PathType.Data);
			List<string> ext = new List<string>() { "json" };
			IEnumerable<string> filePaths = Directory
				.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
				.Where(f => ext.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));
			foreach(string filePath in filePaths) {
				string text = File.ReadAllText(filePath);
				foreach(char ch in text) { _usedCharactersSet.Add(ch); }
			}

			Debug.Log($"Finished. Total characters used: {_usedCharactersSet.Count}");

			string allCharacters = new string(_usedCharactersSet.ToArray());
			File.WriteAllText(PathUtilities.CreateDataPath("HoloverseScraper", "usedCharacters.txt", PathType.Data), allCharacters);
		}
	}
}
