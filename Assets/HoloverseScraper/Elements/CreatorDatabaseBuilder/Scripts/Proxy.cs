
namespace Holoverse.Scraper
{
	public struct Proxy
	{
		public string host;
		public int port;

		public Proxy(string rawProxy)
		{
			string[] rawProxySplit = rawProxy.Split(':');
			host = rawProxySplit[0];
			port = int.Parse(rawProxySplit[1]);
		}

		public override string ToString() => $"{host}:{port}";
	}
}