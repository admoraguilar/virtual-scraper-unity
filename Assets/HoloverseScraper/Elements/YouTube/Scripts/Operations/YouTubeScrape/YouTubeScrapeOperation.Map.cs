using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Holoverse.Scraper
{
	using Api.Data.YouTube;

	public partial class YouTubeScrapeOperation
	{
		public class AggregateMap : BaseMap
		{
			public List<AuthorMap> authors { get; private set; } = new List<AuthorMap>();

			public AggregateMap(string saveDirectoryPath, Settings settings) : base(saveDirectoryPath)
			{
				authors.AddRange(
					settings.idols.SelectMany((AuthorGroup cg) => 
						cg.authors.Select((Author ch) => {
							return new AuthorMap(Path.Combine(this.saveDirectoryPath, ch.id), ch);
						})
					)
				);
				authors.AddRange(
					settings.community.SelectMany((AuthorGroup cg) =>
						cg.authors.Select((Author ch) => {
							return new AuthorMap(Path.Combine(this.saveDirectoryPath, ch.id), ch);
						})
					)
				);

				discover.filters.Add((Video video) => 
					Filters.IsAuthorIdMatch(
						video,
						settings.idols.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);

				community.filters.Add((Video video) =>
					Filters.IsAuthorIdMatch(
						video,
						settings.community.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);
				community.filters.Add((Video video) =>
					Filters.IsAuthorMatch(
						video,
						settings.idols.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);

				anime.filters.Add((Video video) =>
					Filters.IsAuthorIdMatch(
						video,
						settings.community
							.Where((AuthorGroup cg) => cg.name.Contains("Anime"))
							.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);
				anime.filters.Add((Video video) => Filters.ContainsTextInTitle(video, "【アニメ】"));

				live.filters.Add((Broadcast broadcast) =>
					Filters.IsAuthorIdMatch(
						broadcast,
						settings.idols.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);
				live.filters.Add((Broadcast broadcast) => Filters.IsLive(broadcast));

				schedule.filters.Add((Broadcast broadcast) =>
					Filters.IsAuthorIdMatch(
						broadcast,
						settings.idols.SelectMany((AuthorGroup cg) => cg.authors)
					)
				);
				schedule.filters.Add((Broadcast broadcast) => !Filters.IsLive(broadcast));
			}

			public override void Add(Video video)
			{
				base.Add(video);
				authors.ForEach((AuthorMap map) => map.Add(video));
			}

			public override void Add(Broadcast broadcast)
			{
				base.Add(broadcast);
				authors.ForEach((AuthorMap map) => map.Add(broadcast));
			}

			public override void Save()
			{
				base.Save();
				authors.ForEach((AuthorMap map) => map.Save());
			}
		}

		public class AuthorMap : BaseMap
		{
			public readonly Author author = null;

			public AuthorMap(string saveDirectoryPath, Author author) : base(saveDirectoryPath)
			{
				this.author = author;

				discover.filters.Add((Video video) => Filters.IsAuthorIdMatch(video, author));

				community.filters.Add((Video video) => !Filters.IsAuthorIdMatch(video, author));
				community.filters.Add((Video video) => Filters.IsAuthorMatch(video, author));

				anime.filters.Add((Video video) => Filters.IsAuthorMatch(video, author));
				anime.filters.Add((Video video) => Filters.ContainsTextInTitle(video, "【アニメ】"));

				live.filters.Add((Broadcast broadcast) => Filters.IsAuthorIdMatch(broadcast, author));
				live.filters.Add(Filters.IsLive);

				schedule.filters.Add((Broadcast broadcast) => Filters.IsAuthorIdMatch(broadcast, author));
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
				discover.Replace(discover.OrderByDescending((Video video) => video.uploadDate).ToArray());
				community.Replace(community.OrderByDescending((Video video) => video.uploadDate).ToArray());
				anime.Replace(anime.OrderByDescending((Video video) => video.uploadDate).ToArray());
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