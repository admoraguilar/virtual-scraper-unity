using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Holoverse.Scraper
{
	public abstract class DataMap
	{
		public readonly string saveDirectoryPath = string.Empty;

		public DataMap(string saveDirectoryPath)
		{
			this.saveDirectoryPath = saveDirectoryPath;
		}
	}
}
