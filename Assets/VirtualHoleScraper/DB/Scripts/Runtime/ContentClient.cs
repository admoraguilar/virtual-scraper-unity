using Midnight;

namespace VirtualHole.Scraper
{
	using DB;

	public class ContentClientSettings
	{
		public string connectionString { get; set; } = string.Empty;
		public string userName { get; set; } = string.Empty;
		public string password { get; set; } = string.Empty;
		public ProxyPool proxyPool { get; set; } = null;
	}

	public class ContentClient
	{
		public CreatorClient creators { get; private set; } = null;

		private ScraperClient _scraperClient = null;
		private VirtualHoleDBClient _dbClient = null;

		public ContentClient(ContentClientSettings settings)
		{
			_scraperClient = new ScraperClient(settings.proxyPool);
			_dbClient = new VirtualHoleDBClient(settings.connectionString, settings.userName, settings.password);
			creators = new CreatorClient(_scraperClient, _dbClient);
		}
	}
}
