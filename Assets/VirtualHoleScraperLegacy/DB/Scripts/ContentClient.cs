using Midnight;

namespace VirtualHole.Scraper
{
	using DB;

	public class ContentBuilderSettings
	{
		public string connectionString { get; set; } = string.Empty;
		public string userName { get; set; } = string.Empty;
		public string password { get; set; } = string.Empty;
		public ProxyPool proxyPool { get; set; } = null;
	}

	public class ContentClient
	{
		public CreatorClient creators { get; private set; } = null;
		public VideoClient videos { get; private set; } = null;

		private ContentBuilderSettings _settings = null;
		private ProxyPool _proxyPool = null;
		private VirtualHoleDBClient _dbClient = null;

		public bool isUseProxy
		{
			get => _isUseProxy;
			set {
				_isUseProxy = value;
				_youTubeScraperFactory.isUseProxy = _isUseProxy;
			}
		}
		private bool _isUseProxy = false;

		private YouTubeScraperFactory _youTubeScraperFactory = null;

		public ContentClient(ContentBuilderSettings settings)
		{
			_settings = settings;
			_proxyPool = _settings.proxyPool;

			_dbClient = new VirtualHoleDBClient(_settings.connectionString, _settings.userName, _settings.password);
			_youTubeScraperFactory = new YouTubeScraperFactory(_proxyPool);

			creators = new CreatorClient(_dbClient);
			videos = new VideoClient(_dbClient, _youTubeScraperFactory);
		}

		public void SetProxies(string source)
		{
			_youTubeScraperFactory.SetProxies(source);
		}
	}
}
