using Holoverse.Api.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using Midnight;

namespace Holoverse.Scraper
{
	[Serializable]
	public class ContentDatabaseClientSettings
	{
		public bool isUseProxy = false;
		
		[TextArea(5, 5)]
		public string proxyList = string.Empty;

		[Space]
		public HoloverseDataClientSettings dataClient = new HoloverseDataClientSettings();

		public (string, int) GetRandomProxy()
		{
			IReadOnlyList<string> proxies = TextFileUtilities.GetNLSV(proxyList);
			string proxy = proxies[UnityEngine.Random.Range(0, proxies.Count)];
			return (proxy.Split(':')[0], int.Parse(proxy.Split(':')[1]));
		}
	}
}
