using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data.YouTube;

	public partial class YouTubeScrapeOperation
	{
		[Serializable]
		public class Settings
		{
			public List<ChannelGroup> idols => _idols;
			[Space]
			[SerializeField]
			private List<ChannelGroup> _idols = new List<ChannelGroup>();

			public List<ChannelGroup> community => _community;
			[SerializeField]
			private List<ChannelGroup> _community = new List<ChannelGroup>();
		}

		[Serializable]
		public class ChannelGroup
		{
			public string name = string.Empty;
			public List<Channel> channels = new List<Channel>();
		}
	}
}
