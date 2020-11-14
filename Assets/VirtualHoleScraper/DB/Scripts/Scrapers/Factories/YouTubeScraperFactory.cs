
namespace VirtualHole.Scraper
{
	public class YouTubeScraperFactory : ScraperFactory<YouTubeScraper>
	{
		public YouTubeScraperFactory(ProxyPool proxyPool) : base(proxyPool)
		{ }

		protected override YouTubeScraper InternalGet_Impl()
		{
			return new YouTubeScraper();
		}

		protected override YouTubeScraper InternalGet_Impl(Proxy proxy)
		{
			return new YouTubeScraper(HttpClientFactory.Get(proxy));
		}
	}
}
