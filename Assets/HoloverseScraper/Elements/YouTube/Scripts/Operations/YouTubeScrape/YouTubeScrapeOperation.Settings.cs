using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	public partial class YouTubeScrapeOperation
	{
		[Serializable]
		public class Settings
		{
			public List<CreatorGroup> idols => _idols;
			[Space]
			[SerializeField]
			private List<CreatorGroup> _idols = new List<CreatorGroup>();

			public List<CreatorGroup> community => _community;
			[SerializeField]
			private List<CreatorGroup> _community = new List<CreatorGroup>();
		}

		[Serializable]
		public class CreatorGroup
		{
			public string name = string.Empty;
			public List<Creator> creators = new List<Creator>();
		}
	}
}
