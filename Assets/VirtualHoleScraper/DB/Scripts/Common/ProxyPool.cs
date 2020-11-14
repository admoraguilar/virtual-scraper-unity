using System.Collections.Generic;
using Midnight;

namespace VirtualHole.Scraper
{
	public class ProxyPool
	{
		public ICollection<Proxy> proxies => _proxyList;
		private List<Proxy> _proxyList = new List<Proxy>();
		private Queue<Proxy> _proxyQueue = new Queue<Proxy>();

		public ProxyPool() 
		{ }

		public ProxyPool(string source)
		{
			Set(source);
		}

		public Proxy Get()
		{
			if(_proxyQueue.Count <= 0) {
				_proxyList.Shuffle();
				_proxyQueue = new Queue<Proxy>(_proxyList);
			}
			return _proxyQueue.Dequeue();
		}

		public void Set(string source)
		{
			_proxyList.Clear();

			IReadOnlyList<string> rawProxies = TextFileUtilities.GetNLSV(source);
			foreach(string rawProxy in rawProxies) {
				if(Proxy.TryParse(rawProxy, out Proxy proxy)) {
					_proxyList.Add(proxy);
				}
			}
		}
	}
}
