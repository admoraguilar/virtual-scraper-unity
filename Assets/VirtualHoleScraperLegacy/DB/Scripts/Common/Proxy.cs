using System;
using System.Net;

namespace VirtualHole.LegacyScraper
{
	public struct Proxy
	{
		public static Proxy Parse(string value)
		{
			if(!TryParse(value, out Proxy result)) {
				throw new InvalidOperationException("String is not a parsable proxy.");
			}
			return result;
		}

		public static bool TryParse(string value, out Proxy resultProxy)
		{
			bool result = false;

			string[] split = value.Split(':');
			IPAddress address = null;
			int port = 0;

			if(IPAddress.TryParse(split[0], out address)) {
				if(split.Length > 1) {
					if(int.TryParse(split[1], out port)) {
						if(port > 0 && port < 65536) {
							result = true;
						}
					}
				}
			}

			if(result) { resultProxy = new Proxy(address.ToString(), port); }
			else { resultProxy = new Proxy("0.0.0.0", 0); }
			return result;
		}

		public string host;
		public int port;

		public Proxy(string host, int port)
		{
			this.host = host;
			this.port = port;
		}

		public override string ToString() => $"{host}:{port}";
	}
}