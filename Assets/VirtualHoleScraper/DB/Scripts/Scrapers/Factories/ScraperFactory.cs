using System;
using System.Collections.Generic;
using Midnight;

namespace VirtualHole.Scraper
{
	public abstract class ScraperFactory<T>
	{
		private static Dictionary<string, T> _cache = new Dictionary<string, T>();

		public bool isUseProxy { get; set; } = true;
		protected ProxyPool _proxyPool { get; private set; } = null;

		public ScraperFactory(ProxyPool proxyPool)
		{
			_proxyPool = proxyPool;
		}

		public T Get()
		{
			if(isUseProxy || _proxyPool == null) {
				Proxy proxy = _proxyPool.Get();
				MLog.Log(nameof(ContentClient), $"Proxy: {proxy}");
				return InternalGet(proxy); 
			} else { 
				return InternalGet(); 
			}
		}

		protected T InternalGet() 
		{
			return FromCacheGetOrSet("0", () => InternalGet_Impl());
		}

		protected abstract T InternalGet_Impl();

		protected T InternalGet(Proxy proxy)
		{
			return FromCacheGetOrSet(proxy.ToString(), () => InternalGet_Impl(proxy));
		}

		protected abstract T InternalGet_Impl(Proxy proxy);

		private T FromCacheGetOrSet(string id, Func<T> factory)
		{
			if(!_cache.TryGetValue(id, out T instance)) {
				_cache[id] = instance = factory();
			}
			return instance;
		}

		public void SetProxies(string source)
		{
			if(_proxyPool == null) { return; }
			_proxyPool.Set(source);
		}
	}
}
