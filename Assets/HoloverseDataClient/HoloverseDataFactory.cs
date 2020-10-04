using System;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Holoverse.Api.Data
{
	public static class HoloverseDataFactory
	{
		private static IMongoClient _client = null;

		public static IMongoClient GetMongoClient(string connectionString)
		{
			if(_client != null) { return _client; }

			ConventionPack deserializeOnlyAvailableFields = new ConventionPack();
			deserializeOnlyAvailableFields.Add(new IgnoreExtraElementsConvention(true));
			ConventionRegistry.Register("Deserialize Only Available Fields", deserializeOnlyAvailableFields, t => true);

			ConventionPack serializeDateTimeOffsetAsString = new ConventionPack();
			serializeDateTimeOffsetAsString.AddMemberMapConvention(
				"Serialize DateTimeOffset As String",
				(BsonMemberMap m) => {
					if(m.MemberType == typeof(DateTimeOffset)) {
						m.SetSerializer(new DateTimeOffsetSerializer(BsonType.String));
					}
				}
			);
			ConventionRegistry.Register("Serialize DateTimeOffset As String", serializeDateTimeOffsetAsString, t => true);

			ConventionPack idNamingConvention = new ConventionPack();
			idNamingConvention.Add(new NoIdMemberConvention());
			idNamingConvention.Add(new NamedIdMemberConvention("_id"));
			ConventionRegistry.Register("Id Naming Convention", idNamingConvention, t => true);

			BsonClassMap.RegisterClassMap<Video>(cm => {
				cm.AutoMap();
				cm.SetIsRootClass(true);
			});
			BsonClassMap.RegisterClassMap<Broadcast>();

			return _client = new MongoClient(connectionString);
		}
	}
}