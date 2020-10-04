using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "Holoverse/Content Database/Client Object")]
	public class ContentDatabaseClientObject : ScriptableObject
	{
		public ContentDatabaseClient client =>
			_contentDBClient == null ? _contentDBClient = new ContentDatabaseClient(_dataClientSettings) :
			_contentDBClient;
		private ContentDatabaseClient _contentDBClient = null;

		[SerializeField]
		private HoloverseDataClientSettings _dataClientSettings = new HoloverseDataClientSettings();

#if UNITY_EDITOR
		public UObject editor_creatorObjectsFolderPath => _editor_creatorObjectsFolderPath;
		[Header("Editor")]
		[SerializeField]
		private UObject _editor_creatorObjectsFolderPath = null;

		public CreatorObject[] editor_toExcludeCreators => _editor_toExcludeCreators;
		[SerializeField]
		private CreatorObject[] _editor_toExcludeCreators = new CreatorObject[0];
#endif
	}
}
