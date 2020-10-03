using System;

namespace Holoverse.Api.Data
{
	[Serializable]
	public class HoloverseDataClientSettings
	{
		public string connectionString = string.Empty;
		public string userName = string.Empty;
		public string password = string.Empty;

		public string BuildConnectionString()
		{
			return connectionString
				.Replace("<username>", userName)
				.Replace("<password>", password);
		}

		public override string ToString()
		{
			return BuildConnectionString();
		}
	}
}
