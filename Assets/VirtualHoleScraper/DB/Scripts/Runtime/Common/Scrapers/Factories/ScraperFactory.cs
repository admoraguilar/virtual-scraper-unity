using System;
using System.Collections.Generic;
using Midnight;

namespace VirtualHole.Scraper
{
	public abstract class ScraperFactory<T> : ScraperFactory
	{
		private Dictionary<string, T> _cache = new Dictionary<string, T>();

		public ScraperFactory(ProxyPool proxyPool) : base(proxyPool)
		{ }

		public T Get()
		{
			if(_proxyPool != null || isUseProxy) {
				Proxy proxy = _proxyPool.Get();
				MLog.Log(typeof(ScraperFactory<T>).GetPrettyClassName(), $"Proxy: {proxy}");
				return InternalGet(proxy); 
			} else {
				MLog.Log(typeof(ScraperFactory<T>).GetPrettyClassName(), $"Proxy: None");
				return InternalGet(); 
			}
		}

		protected T InternalGet(Proxy proxy)
		{
			return FromCacheGetOrSet(proxy.ToString(), () => InternalGet_Impl(proxy));
		}

		protected abstract T InternalGet_Impl(Proxy proxy);

		protected T InternalGet() 
		{
			return FromCacheGetOrSet("0", () => InternalGet_Impl());
		}

		protected abstract T InternalGet_Impl();

		private T FromCacheGetOrSet(string id, Func<T> factory)
		{
			if(!_cache.TryGetValue(id, out T instance)) {
				_cache[id] = instance = factory();
			}
			return instance;
		}
	}

	public abstract class ScraperFactory 
	{
		public bool isUseProxy 
		{
			get => _isUseProxy;
			set {
				if(_proxyPool == null && value) {
					throw new InvalidOperationException("Can't use proxies without a proxy pool.");
				}
				_isUseProxy = value;
			}
		}
		private bool _isUseProxy = false;

		protected ProxyPool _proxyPool { get; private set; } = null;

		public ScraperFactory(ProxyPool proxyPool)
		{
			_proxyPool = proxyPool;
			if(_proxyPool == null) { isUseProxy = false; }
		}
	}
}
