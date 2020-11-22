using System.Net;
using System.Net.Http;
using System.Collections.Generic;

namespace VirtualHole.LegacyScraper
{
	public class HttpClientFactory
	{
		private static Dictionary<string, HttpClient> _lookup = new Dictionary<string, HttpClient>();

		public static HttpClient Get(Proxy proxy)
		{
			string proxyString = proxy.ToString();

			if(!_lookup.TryGetValue(proxyString, out HttpClient client)) {
				HttpClientHandler clientHandler = new HttpClientHandler();
				clientHandler.Proxy = new WebProxy(proxy.host, proxy.port);
				clientHandler.UseCookies = false;

				if(clientHandler.SupportsAutomaticDecompression) {
					clientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				}

				client = new HttpClient(clientHandler, true);
				client.DefaultRequestHeaders.Add(
					"User-Agent",
					"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36"
				);
				client.DefaultRequestHeaders.ConnectionClose = true;

				_lookup[proxyString] = client;
			}

			return client;
		}
	}
}
