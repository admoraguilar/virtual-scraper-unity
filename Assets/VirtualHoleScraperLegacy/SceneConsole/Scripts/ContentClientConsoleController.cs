using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using Midnight.Tasks;

namespace VirtualHole.Scraper
{
	public class ContentClientConsoleController : MonoBehaviour
	{
		private enum WriteMode
		{
			Full,
			Incremental
		}

		public string settingsPath
		{
			get => PathUtilities.CreateDataPath("VirtualHoleScraper/config", "startup.json", PathType.Data);
		}

		[SerializeField]
		private ContentClientObject _clientObject = null;
		private ContentClientConsoleSettings _settings = new ContentClientConsoleSettings();
		
		private Queue<Action> _actionQueue = new Queue<Action>();
		private DateTime _lastFullRun = DateTime.MinValue;
		private DateTime _lastRun = DateTime.MinValue;
		
		private bool _isRunning = false;
		private CancellationTokenSource _cts = null;

		private void LoadSettings()
		{
			_settings = JsonUtilities.LoadFromDisk<ContentClientConsoleSettings>(new JsonUtilities.LoadFromDiskParameters() {
				filePath = settingsPath
			});
			MLog.Log($"Loaded settings: {Environment.NewLine} {_settings}");
		}

		private async Task RunAsync(bool isIncremental = false, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if(_isRunning) { return; }
			_isRunning = true;

			_clientObject.isUseProxy = _settings.useProxies;
			_lastFullRun = DateTime.Now;

			using(StopwatchScope stopwatch = new StopwatchScope(
				nameof(ContentClientConsoleController),
				"Start run..",
				"Success! Taking a break before next iteration.")) {
				await TaskExt.RetryAsync(
					() => _clientObject.WriteToVideosDBUsingCreatorsDBAsync(
						isIncremental, cancellationToken),
					TimeSpan.FromSeconds(3), 3, cancellationToken
				);
			}

			if(DateTime.Now.Subtract(_lastFullRun).Days > 0) {
				_lastFullRun = DateTime.Now;

				_actionQueue.Enqueue(() => {
					Task.Run(() => RunAsync(false));
				});
			} else {
				_actionQueue.Enqueue(() => {
					Task.Run(() => RunAsync(true));
				});
			}

			_lastRun = DateTime.Now;
			_isRunning = false;
		}

		private void Start()
		{
			// Setting idle frame rate because this is headless and 
			// we don't really need to run at 60 FPS.
			// https://forum.unity.com/threads/headless-server-high-cpu-usage.441727/#post-2858772
			Application.targetFrameRate = 1;

			LoadSettings();
			Task.Run(() => RunAsync(_settings.isStartIncremental));
		}

		private void FixedUpdate()
		{
			if(!_isRunning) {
				TimeSpan diff = DateTime.Now - _lastRun;
				if(diff.Seconds >= _settings.iterationGapAmount) {
					Action action = _actionQueue.Dequeue();
					action();
				}
			}
		}
	}
}
