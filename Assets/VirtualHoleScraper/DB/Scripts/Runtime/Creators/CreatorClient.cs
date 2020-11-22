using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Midnight;

namespace VirtualHole.Scraper
{
	using DB;
	using DB.Common;
	using DB.Contents;
	using DB.Contents.Creators;

	using DBCreatorClient = DB.Contents.Creators.CreatorClient;

	public class CreatorClient
	{
		public string localJsonPath
		{
			get => PathUtilities.CreateDataPath("VirtualHoleScraper", "creators.json", PathType.Data);
		}

		private YouTubeScraperFactory _youtubeScraperFactory => _scraperClient.youtube;
		private ScraperClient _scraperClient = null;

		private DBCreatorClient _dbCreatorClient => _dbClient.Contents.Creators;
		private VirtualHoleDBClient _dbClient = null;

		public CreatorClient(ScraperClient scraperClient, VirtualHoleDBClient dbClient)
		{
			_scraperClient = scraperClient;
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
				FindResults<Creator> findResults = await _dbCreatorClient.FindCreatorsAsync(findSettings, cancellationToken);
				while(await findResults.MoveNextAsync(cancellationToken)) {
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
				await _dbCreatorClient.UpsertManyCreatorsAndDeleteDanglingAsync(creators, cancellationToken);
			}

		}

		public IEnumerable<Creator> LoadFromJson()
		{
			return JsonUtilities.LoadFromDisk<Creator[]>(new JsonUtilities.LoadFromDiskParameters() {
				filePath = localJsonPath,
				jsonSerializerSettings = JsonConfig.DefaultSettings
			});
		}

		public void SaveToJson(IEnumerable<Creator> creators)
		{
			JsonUtilities.SaveToDisk(creators, new JsonUtilities.SaveToDiskParameters() {
				filePath = localJsonPath,
				jsonSerializerSettings = JsonConfig.DefaultSettings
			});
		}

		public async Task AutoFillAsync(CreatorObject creatorObj)
		{
			using(StopwatchScope stopwatchScope = new StopwatchScope(
				nameof(CreatorObject),
				$"Start auto fill [{creatorObj.universalName}] info",
				$"Finished auto filling [{creatorObj.universalName}] info")) {
				creatorObj.universalId = creatorObj.universalName.RemoveSpecialCharacters().Replace(" ", "");
				creatorObj.wikiUrl = $"https://virtualyoutuber.fandom.com/wiki/{creatorObj.universalName.Replace(" ", "_")}";

				bool isMainAvatarUrlSet = false;
				for(int i = 0; i < creatorObj.socials.Length; i++) {
					Social social = creatorObj.socials[i];
					if(social.Platform == Platform.YouTube) {
						social = creatorObj.socials[i] = await _youtubeScraperFactory.Get().GetChannelInfoAsync(social.Url);
						if(!isMainAvatarUrlSet) {
							isMainAvatarUrlSet = true;
							creatorObj.avatarUrl = social.AvatarUrl;
						}
					}
				}
			}
		}

		public void Autofill(CreatorObject creatorObj)
		{
			Creator creator = LoadFromJson().FirstOrDefault(c => c.UniversalId == creatorObj.universalId);
			if(creator == null) {
				MLog.Log(nameof(CreatorObject), $"Can't find [{creatorObj.universalId}] data on json.");
				return;
			}

			creatorObj.socials = creator.Socials;
		}
	}
}
