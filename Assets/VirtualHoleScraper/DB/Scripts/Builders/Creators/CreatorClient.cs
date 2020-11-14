using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Midnight;

namespace VirtualHole.Scraper
{
	using DB.Common;
	using DB.Contents.Creators;
	using VirtualHole.DB;

	public class CreatorClient
	{
		public string localJsonPath
		{
			get => PathUtilities.CreateDataPath("VirtualHole", "creators.json", PathType.Data);
		}

		private VirtualHoleDBClient _dbClient = null;

		public CreatorClient(VirtualHoleDBClient dbClient)
		{
			_dbClient = dbClient;
		}

		public async Task<IEnumerable<Creator>> GetAllFromDBAsync(CancellationToken cancellationToken = default)
		{
			List<Creator> results = new List<Creator>();

			using(StopwatchScope stopwatch = new StopwatchScope(
				nameof(CreatorClient),
				"Start getting all creators from DB",
				"Finished getting all creators from DB")) {
				FindCreatorsStrictSettings findSettings = new FindCreatorsStrictSettings { IsAll = true };
				FindResults<Creator> findResults = await _dbClient.Contents.Creators.FindCreatorsAsync(findSettings, cancellationToken);
				while(!await findResults.MoveNextAsync(cancellationToken)) {
					results.AddRange(findResults.Current);
				}
			}

			return results;
		}

		public async Task WriteToDBAsync(IEnumerable<Creator> creators, CancellationToken cancellationToken = default)
		{
			using(StopwatchScope stopwatchScope = new StopwatchScope(
				nameof(CreatorClient),
				"Start writing creators to DB",
				"Finished writing creators to DB")) {
				await _dbClient.Contents.Creators.UpsertManyCreatorsAndDeleteDanglingAsync(creators, cancellationToken);
			}
			
		}

		public IEnumerable<Creator> LoadFromJson()
		{
			return JsonUtilities.LoadFromDisk<Creator[]>(new JsonUtilities.LoadFromDiskParameters {
				filePath = localJsonPath
			});
		}

		public void SaveToJson(IEnumerable<Creator> creators)
		{
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters {
				filePath = localJsonPath
			});
		}
	}
}
