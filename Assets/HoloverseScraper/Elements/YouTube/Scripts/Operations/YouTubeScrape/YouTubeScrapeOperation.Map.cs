using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Holoverse.Scraper
{
	using Api.Data;

	public partial class YouTubeScrapeOperation
	{
		public class AggregateMap : BaseMap
		{
			public List<CreatorMap> authors { get; private set; } = new List<CreatorMap>();

			public AggregateMap(string saveDirectoryPath, Settings settings) : base(saveDirectoryPath)
			{
				authors.AddRange(
					settings.idols.SelectMany((CreatorGroup cg) => 
						cg.creators.Select((Creator ch) => {
							return new CreatorMap(Path.Combine(this.saveDirectoryPath, ch.universalId), ch);
						})
					)
				);
				authors.AddRange(
					settings.community.SelectMany((CreatorGroup cg) =>
						cg.creators.Select((Creator ch) => {
							return new CreatorMap(Path.Combine(this.saveDirectoryPath, ch.universalId), ch);
						})
					)
				);

				discover.filters.Add((Video video) => 
					Filters.IsCreatorIdMatch(
						video,
						settings.idols.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);

				community.filters.Add((Video video) =>
					Filters.IsCreatorIdMatch(
						video,
						settings.community.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);
				community.filters.Add((Video video) =>
					Filters.IsCreatorMatch(
						video,
						settings.idols.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);

				anime.filters.Add((Video video) =>
					Filters.IsCreatorIdMatch(
						video,
						settings.community
							.Where((CreatorGroup cg) => cg.name.Contains("Anime"))
							.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);
				anime.filters.Add((Video video) => Filters.ContainsTextInTitle(video, "【アニメ】"));

				live.filters.Add((Broadcast broadcast) =>
					Filters.IsCreatorIdMatch(
						broadcast,
						settings.idols.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);
				live.filters.Add((Broadcast broadcast) => Filters.IsLive(broadcast));

				schedule.filters.Add((Broadcast broadcast) =>
					Filters.IsCreatorIdMatch(
						broadcast,
						settings.idols.SelectMany((CreatorGroup cg) => cg.creators)
					)
				);
				schedule.filters.Add((Broadcast broadcast) => !Filters.IsLive(broadcast));
			}

			public override void Add(Video video)
			{
				base.Add(video);
				authors.ForEach((CreatorMap map) => map.Add(video));
			}

			public override void Add(Broadcast broadcast)
			{
				base.Add(broadcast);
				authors.ForEach((CreatorMap map) => map.Add(broadcast));
			}

			public override void Save()
			{
				base.Save();
				authors.ForEach((CreatorMap map) => map.Save());
			}
		}

		public class CreatorMap : BaseMap
		{
			public readonly Creator author = null;

			public CreatorMap(string saveDirectoryPath, Creator author) : base(saveDirectoryPath)
			{
				this.author = author;

				discover.filters.Add((Video video) => Filters.IsCreatorIdMatch(video, author));

				community.filters.Add((Video video) => !Filters.IsCreatorIdMatch(video, author));
				community.filters.Add((Video video) => Filters.IsCreatorMatch(video, author));

				anime.filters.Add((Video video) => Filters.IsCreatorMatch(video, author));
				anime.filters.Add((Video video) => Filters.ContainsTextInTitle(video, "【アニメ】"));

				live.filters.Add((Broadcast broadcast) => Filters.IsCreatorIdMatch(broadcast, author));
				live.filters.Add(Filters.IsLive);

				schedule.filters.Add((Broadcast broadcast) => Filters.IsCreatorIdMatch(broadcast, author));
				schedule.filters.Add((Broadcast broadcast) => !Filters.IsLive(broadcast));
			}
		}

		public abstract class BaseMap : Map
		{
			public Container<Video> discover { get; private set; } = new Container<Video>();
			public Container<Video> community { get; private set; } = new Container<Video>();
			public Container<Video> anime { get; private set; } = new Container<Video>();
			public Container<Broadcast> live { get; private set; } = new Container<Broadcast>();
			public Container<Broadcast> schedule { get; private set; } = new Container<Broadcast>();

			public BaseMap(string saveDirectoryPath) : base(saveDirectoryPath)
			{
				discover.savePath = Path.Combine(saveDirectoryPath, "discover.json");
				community.savePath = Path.Combine(saveDirectoryPath, "community.json");
				anime.savePath = Path.Combine(saveDirectoryPath, "anime.json");
				live.savePath = Path.Combine(saveDirectoryPath, "live.json");
				schedule.savePath = Path.Combine(saveDirectoryPath, "schedule.json");
			}

			public virtual void Add(Video video)
			{
				discover.Add(video);
				community.Add(video);
				anime.Add(video);
			}

			public virtual void Add(Broadcast broadcast)
			{
				live.Add(broadcast);
				schedule.Add(broadcast);
			}

			public virtual void Save()
			{
				discover.Replace(discover.OrderByDescending((Video video) => video.creationDate).ToArray());
				community.Replace(community.OrderByDescending((Video video) => video.creationDate).ToArray());
				anime.Replace(anime.OrderByDescending((Video video) => video.creationDate).ToArray());
				live.Replace(live.OrderByDescending((Broadcast broadcast) => broadcast.schedule).ToArray());
				schedule.Replace(schedule.OrderByDescending((Broadcast broadcast) => broadcast.schedule).ToArray());

				PostProcess(discover);
				PostProcess(community);
				PostProcess(anime);
				PostProcess(live);
				PostProcess(schedule);

				discover.Save();
				community.Save();
				anime.Save();
				live.Save();
				schedule.Save();

				void PostProcess<T>(Container<T> container)
					where T : Video
				{
					foreach(T video in container) {
						video.description = string.Empty;
					}
				}
			}
		}

		public abstract class Map
		{
			public readonly string saveDirectoryPath = string.Empty;

			public Map(string saveDirectoryPath)
			{
				this.saveDirectoryPath = saveDirectoryPath;
			}
		}
	}
}