using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Holoverse.Api.Data
{
	public class HoloverseDataClient
	{
		private const string _contentDatabaseName = "content";

		private const string _blogsCollectionName = "blogs";
		private const string _creatorsCollectionName = "creators";
		private const string _videosCollectionName = "videos";

		private const string _lastOperationTimestampFieldName = "lastOperationTimestamp";

		private const int _defaultFindResultsLimit = 500;

		private IMongoClient _client = null;
		private IMongoDatabase _contentDatabase = null;

		public HoloverseDataClient(HoloverseDataClientSettings settings)
		{
			_client = HoloverseDataFactory.GetMongoClient(settings.BuildConnectionString());
			_contentDatabase = _client.GetDatabase(_contentDatabaseName);
		}

		public async Task<IAsyncCursor<Creator>> FindMatchingCreatorsAsync(
			FilterDefinition<Creator> filter, int batchSize)
		{
			return await FindAsync(
				_contentDatabase.GetCollection<Creator>(_creatorsCollectionName),
				filter, batchSize
			);
		}

		public async Task UpsertCreatorAsync(Creator creator)
		{
			await UpsertAsync(
				_contentDatabase.GetCollection<Creator>(_creatorsCollectionName),
				HoloverseDataFilter.Creator.UniversalIdEquals(creator.universalId),
				creator
			);
		}

		public async Task UpsertManyCreatorsAsync(IEnumerable<Creator> creators)
		{
			await UpsertManyAsync(
				_contentDatabase.GetCollection<BsonDocument>(_creatorsCollectionName),
				(Creator creator) => HoloverseDataFilter.BsonDocument.StringEquals("universalId", creator.universalId),
				creators,
				DateTimeOffset.UtcNow.DateTime
			);
		}

		public async Task UpsertManyCreatorsAndDeleteDanglingAsync(IEnumerable<Creator> creators)
		{
			await UpsertManyAndDeleteDanglingAsync(
				_contentDatabase.GetCollection<BsonDocument>(_creatorsCollectionName),
				(Creator creator) => HoloverseDataFilter.BsonDocument.StringEquals("universalId", creator.universalId),
				creators
			);
		}

		public async Task<IAsyncCursor<T>> FindMatchingVideosAsync<T>(
			FilterDefinition<T> filter, int batchSize) where T : Video
		{
			return await FindAsync(
				_contentDatabase.GetCollection<T>(_videosCollectionName),
				filter, batchSize
			);
		}

		public async Task UpsertVideoAsync<T>(T video) where T : Video
		{
			await UpsertAsync(
				_contentDatabase.GetCollection<T>(_videosCollectionName),
				HoloverseDataFilter.Video<T>.IdEquals(video.id),
				video
			);
		}

		public async Task UpsertManyVideosAsync<T>(IEnumerable<T> videos) where T : Video
		{
			await UpsertManyAsync(
				_contentDatabase.GetCollection<BsonDocument>(_videosCollectionName),
				(T video) => HoloverseDataFilter.BsonDocument.StringEquals("id", video.id),
				videos,
				DateTimeOffset.UtcNow.DateTime
			);
		}

		public async Task UpsertManyVideosAndDeleteDanglingAsync<T>(IEnumerable<T> videos) where T : Video
		{
			await UpsertManyAndDeleteDanglingAsync(
				_contentDatabase.GetCollection<BsonDocument>(_videosCollectionName),
				(T video) => HoloverseDataFilter.BsonDocument.StringEquals("id", video.id),
				videos
			);
		}

		private async Task<IAsyncCursor<T>> FindAsync<T>(
			IMongoCollection<T> collection, FilterDefinition<T> filter,
			int batchSize, int resultsLimit = _defaultFindResultsLimit)
		{
			return await collection.FindAsync(
				filter, 
				new FindOptions<T>() {
					BatchSize = batchSize,
					Limit = resultsLimit
				}
			);
		}

		private async Task UpsertAsync<T>(
			IMongoCollection<T> collection, FilterDefinition<T> filter,
			T obj)
		{
			await collection.ReplaceOneAsync(
				filter, obj,
				new ReplaceOptions {
					IsUpsert = true
				}
			);
		}

		private async Task UpsertManyAsync<T>(
			IMongoCollection<BsonDocument> collection, Func<T, FilterDefinition<BsonDocument>> filter,
			IEnumerable<T> objs, DateTime timestamp)
		{
			List<WriteModel<BsonDocument>> bulkReplace = new List<WriteModel<BsonDocument>>();
			foreach(T obj in objs) {
				bulkReplace.Add(
					new ReplaceOneModel<BsonDocument>(filter(obj), ToBsonDocumentWithTimestamp(obj, timestamp)) {
						IsUpsert = true
					}
				);
			}
			await collection.BulkWriteAsync(bulkReplace);
		}

		private async Task UpsertManyAndDeleteDanglingAsync<T>(
			IMongoCollection<BsonDocument> collection, Func<T, FilterDefinition<BsonDocument>> filter,
			IEnumerable<T> objs)
		{
			DateTime timestamp = DateTimeOffset.UtcNow.DateTime;
			await UpsertManyAsync(collection, filter, objs, timestamp);
			await collection.DeleteManyAsync(!HoloverseDataFilter.BsonDocument.TimestampEquals(_lastOperationTimestampFieldName, timestamp));
		}

		private BsonDocument ToBsonDocumentWithTimestamp<T>(T obj, DateTime timestamp)
		{
			BsonDocument objBSON = obj.ToBsonDocument();
			objBSON.Add(new BsonElement(_lastOperationTimestampFieldName, timestamp));
			return objBSON;
		}
	}
}
