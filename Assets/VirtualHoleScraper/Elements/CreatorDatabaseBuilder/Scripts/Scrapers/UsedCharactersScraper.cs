using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using System.Linq;

namespace VirtualHole.Scraper
{
	public class UsedCharactersScraper : MonoBehaviour
	{
		[SerializeField]
		private TextAsset[] _textAssets = null;

		private HashSet<char> _usedCharactersSet = new HashSet<char>();

		[ContextMenu("Run")]
		public void Run()
		{
			Debug.Log($"Start used characters scraper.");

			if(_textAssets != null || _textAssets.Length > 0) {
				foreach(TextAsset textAsset in _textAssets) {
					foreach(char ch in textAsset.text) { _usedCharactersSet.Add(ch); }
				}
			} else {
				string folderPath = PathUtilities.CreateDataPath("VirtualHoleScraper", string.Empty, PathType.Data);
				List<string> ext = new List<string>() { "json" };
				IEnumerable<string> filePaths = Directory
					.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
					.Where(f => ext.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));
				foreach(string filePath in filePaths) {
					string text = File.ReadAllText(filePath);
					foreach(char ch in text) { _usedCharactersSet.Add(ch); }
				}
			}

			Debug.Log($"Finished. Total characters used: {_usedCharactersSet.Count}");

			string allCharacters = new string(_usedCharactersSet.ToArray());
			string outputFilePath = PathUtilities.CreateDataPath("VirtualHoleScraper", "usedCharacters.txt", PathType.Data);
			File.WriteAllText(AssetUtilities.CreatePath(outputFilePath), allCharacters);
		}
	}
}
