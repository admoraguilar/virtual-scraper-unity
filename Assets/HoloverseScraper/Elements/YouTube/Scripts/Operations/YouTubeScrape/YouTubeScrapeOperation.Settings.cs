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
			public List<AuthorGroup> idols => _idols;
			[Space]
			[SerializeField]
			private List<AuthorGroup> _idols = new List<AuthorGroup>();

			public List<AuthorGroup> community => _community;
			[SerializeField]
			private List<AuthorGroup> _community = new List<AuthorGroup>();
		}

		[Serializable]
		public class AuthorGroup
		{
			public string name = string.Empty;
			public List<Author> authors = new List<Author>();
		}
	}
}
