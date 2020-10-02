using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Holoverse.Api.Data;

namespace Holoverse.Scraper
{
	public partial class AggregateMap
	{
		public class RootMap : DataMap
		{
			public DataContainer<Video> discover { get; private set; } = new DataContainer<Video>();
			public DataContainer<Video> community { get; private set; } = new DataContainer<Video>();
			public DataContainer<Video> anime { get; private set; } = new DataContainer<Video>();
			public DataContainer<Broadcast> live { get; private set; } = new DataContainer<Broadcast>();
			public DataContainer<Broadcast> schedule { get; private set; } = new DataContainer<Broadcast>();


			public RootMap(string saveDirectoryPath) : base(saveDirectoryPath) { }
		}
	}
}
