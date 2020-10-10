using System.Net;
using System.Net.Http;

namespace Holoverse.Scraper
{
	public static class HttpClientFactory
	{
		public static HttpClient CreateProxyClient(Proxy proxy)
		{
			HttpClientHandler handler = new HttpClientHandler();
			handler.Proxy = new WebProxy(proxy.host, proxy.port);
			handler.UseCookies = false;

			if(handler.SupportsAutomaticDecompression) {
				handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			}

			HttpClient client = new HttpClient(handler, true);
			client.DefaultRequestHeaders.Add(
				"User-Agent",
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36"
			);

			return client;
		}
	}
}
