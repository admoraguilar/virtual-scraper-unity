using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Holoverse.Api.Data
{
	using CreatorModel = Creator;
	using VideoModel = Video;
	using BsonDocumentModel = BsonDocument;

	public static class HoloverseDataFilter
	{
		public static class Creator
		{
			private static FilterDefinitionBuilder<CreatorModel> builder =>
				Builders<CreatorModel>.Filter;

			public static FilterDefinition<CreatorModel> All() => builder.Empty;

			public static FilterDefinition<CreatorModel> UniversalIdEquals(string value) =>
				builder.Eq((CreatorModel creator) => creator.universalId, value);

			public static FilterDefinition<CreatorModel> DepthEquals(int value) =>
				builder.Eq((CreatorModel creator) => creator.depth, 0);
		}

		public static class Video<T> where T : VideoModel
		{
			private static FilterDefinitionBuilder<T> builder =>
				Builders<T>.Filter;

			private static Dictionary<string, FilterDefinition<T>> _creatorMatch =
				new Dictionary<string, FilterDefinition<T>>();

			public static FilterDefinition<T> All() => builder.Empty;

			public static FilterDefinition<T> CreatorMatch(CreatorModel creator)
			{
				if(_creatorMatch.TryGetValue(creator.universalId, out FilterDefinition<T> filter)) { return filter; }

				List<FilterDefinition<T>> filters = new List<FilterDefinition<T>>();

				filters.Add(builder.Eq((T video) => video.creatorUniversal, creator.universalName));
				filters.Add(builder.Eq((T video) => video.creatorIdUniversal, creator.universalId));

				IEnumerable<string> creatorNames = creator.socials.Select(s => s.name).Append(creator.universalName);
				IEnumerable<string> creatorIds = creator.socials.Select(s => s.id).Append(creator.universalId);
				IEnumerable<string> creatorUrls = creator.socials.Select(s => s.url);
				IEnumerable<string> creatorCustomKeywords = creator.socials.SelectMany(s => s.customKeywords).Concat(creator.customKeywords);

				AddValueMatchFilters((T video) => video.title, creatorNames);
				AddValueMatchFilters((T video) => video.description, creatorNames);

				AddValueMatchFilters((T video) => video.title, creatorIds);
				AddValueMatchFilters((T video) => video.description, creatorIds);
				
				AddValueMatchFilters((T video) => video.description, creatorUrls);

				AddValueMatchFilters((T video) => video.title, creatorCustomKeywords);
				AddValueMatchFilters((T video) => video.creator, creatorCustomKeywords);
				AddValueMatchFilters((T video) => video.description, creatorCustomKeywords);

				return _creatorMatch[creator.universalId] = builder.Or(filters);

				void AddValueMatchFilters(Expression<Func<T, object>> field, IEnumerable<string> values)
				{
					foreach(string value in values) {
						filters.Add(builder.Regex(field, $"/{value}/i"));
					}
				}
			}

			public static FilterDefinition<T> IdEquals(string value) =>
				builder.Eq((T video) => video.id, value);
		}

		public static class BsonDocument
		{
			private static FilterDefinitionBuilder<BsonDocumentModel> builder =>
				Builders<BsonDocumentModel>.Filter;

			public static FilterDefinition<BsonDocumentModel> StringEquals(string fieldName, string value) =>
				builder.Eq(fieldName, value);

			public static FilterDefinition<BsonDocumentModel> TimestampEquals(string fieldName, DateTime value) =>
				builder.Eq(fieldName, value);
		}
	}
}
