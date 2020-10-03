using MongoDB.Driver;
using MongoDB.Bson.Serialization;

namespace Holoverse.Api.Data
{
	public static class HoloverseDataFactory
	{
		private static IMongoClient _client = null;

		public static IMongoClient GetMongoClient(string connectionString)
		{
			if(_client != null) { return _client; }

			BsonClassMap.RegisterClassMap<Video>(cm => {
				cm.AutoMap();
				cm.SetIsRootClass(true);
			});
			BsonClassMap.RegisterClassMap<Broadcast>();

			return _client = new MongoClient(connectionString);
		}
	}
}