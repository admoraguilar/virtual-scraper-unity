using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Midnight;
using Midnight.Web;
using TMPro;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Videos;

namespace VirtualHole.Sandbox
{
	public class VideoLoadTest : MonoBehaviour
	{
		[Header("Data")]
		public bool isLoadOnStart = false;
		public string thumbnailUrl = string.Empty;
		public string channelName = string.Empty;
		public string viewsCount = string.Empty;

		[Header("UI")]
		public Image thumbnailUi = null;
		public TMP_Text channelUi = null;
		public TMP_Text viewsUi = null;

		private async Task LoadVideo()
		{
			thumbnailUi.sprite = await ImageGetWebRequest.GetAsync(thumbnailUrl);
			channelUi.text = channelName;
			viewsUi.text = viewsCount;
		}

		private async Task LoadExplodeVideo()
		{
			Debug.Log("Load Explode Video");

			YoutubeClient youtubeClient = new YoutubeClient();

			IReadOnlyList<Video> videos = await youtubeClient.Channels.GetUploadsAsync(new ChannelId("UC1CfXB_kRs3C-zaeTG3oGyg"));
			Debug.Log($"video count: {videos.Count}");
			//foreach(Video video in videos) {
			//	Debug.Log($"{video.Title} | {video.Duration} | {video.UploadDate} | {video.Author}");
			//}
			Video video = videos[0];
			thumbnailUi.sprite = await ImageGetWebRequest.GetAsync(video.Thumbnails.MediumResUrl);
			channelUi.text = video.Author;
			viewsUi.text = $"{video.Engagement.ViewCount}";
		}

		private void Start()
		{
			if(isLoadOnStart) {
				_ = LoadExplodeVideo();
			}
		}

#if UNITY_EDITOR

		[ContextMenu("Load Video")]
		private void Editor_LoadVideo()
		{
			_ = LoadVideo();
		}

		[ContextMenu("Load Explode Video")]
		private void Editor_LoadExplodeVideo()
		{
			_ = LoadExplodeVideo();
		}

#endif
	}
}
