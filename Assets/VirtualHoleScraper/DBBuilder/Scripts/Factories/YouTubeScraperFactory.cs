using System.Net.Http;
using System.Collections.Generic;

namespace VirtualHole.Scraper
{
	public static class YouTubeScraperFactory
	{
		private static Dictionary<string, YouTubeScraper> _lookup = new Dictionary<string, YouTubeScraper>();

		public static YouTubeScraper Get()
		{
			return Get("0");
		}

		public static YouTubeScraper Get(Proxy proxy)
		{
			return Get(proxy.ToString(), HttpClientFactory.Get(proxy));
		}

		private static YouTubeScraper Get(string id, HttpClient httpClient = null)
		{
			if(!_lookup.TryGetValue(id, out YouTubeScraper scraper)) {
				_lookup[id] = scraper = 
					httpClient != null ? new YouTubeScraper(httpClient) 
					: new YouTubeScraper(); ;
			}
			return scraper;
		}
	}
}
