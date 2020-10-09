using UnityEngine;

namespace Holoverse.Scraper
{
	using Api.Data;

	using UObject = UnityEngine.Object;

	[CreateAssetMenu(menuName = "Holoverse/Content Database/Client Object")]
	public class ContentDatabaseClientObject : ScriptableObject
	{
		public ContentDatabaseClient client =>
			_contentDBClient == null ? _contentDBClient = new ContentDatabaseClient(_settings) :
			_contentDBClient;
		private ContentDatabaseClient _contentDBClient = null;

		[SerializeField]
		private ContentDatabaseClientSettings _settings = new ContentDatabaseClientSettings();

#if UNITY_EDITOR
		public enum Editor_CreatorObjectsListMode
		{
			Include,
			Exclude
		};

		public UObject editor_creatorObjectsFolderPath => _editor_creatorObjectsFolderPath;
		[Header("Editor")]
		[SerializeField]
		private UObject _editor_creatorObjectsFolderPath = null;

		public CreatorObject[] editor_creatorsList => _editor_creatorsList;
		[SerializeField]
		private CreatorObject[] _editor_creatorsList = new CreatorObject[0];

		public Editor_CreatorObjectsListMode editor_creatorListMode => _editor_creatorListMode;
		[SerializeField]
		private Editor_CreatorObjectsListMode _editor_creatorListMode = Editor_CreatorObjectsListMode.Include;
#endif
	}
}
