
namespace VirtualHole.Scraper
{
	public class ScraperClient
	{
		public YouTubeScraperFactory youtube { get; set; }

		public ScraperClient(ProxyPool proxyPool)
		{
			youtube = new YouTubeScraperFactory(proxyPool);
		}
	}
}
