using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Midnight;
using Midnight.Concurrency;

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
		private Stack<WriteMode> _writeMode = new Stack<WriteMode>();
		private DateTime _lastFullRun = DateTime.MinValue;
		private bool _isRunning = false;
		private CancellationTokenSource _cts = null;

		private void LoadSettings()
		{
			_settings = JsonUtilities.LoadFromDisk<ContentClientConsoleSettings>(new JsonUtilities.LoadFromDiskParameters() {
				filePath = settingsPath
			});
			MLog.Log($"Loaded settings: {Environment.NewLine} {_settings}");
		}

		private void Run(bool isStartIncremental = false)
		{
			if(_isRunning) { return; }
			_isRunning = true;

			_clientObject.isUseProxy = _settings.useProxies;

			if(isStartIncremental) { _writeMode.Push(WriteMode.Incremental); } 
			else { _writeMode.Push(WriteMode.Full); }

			_lastFullRun = DateTime.Now;
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				while(_isRunning) {
					WriteMode curWriteMode = _writeMode.Pop();
					cancellationToken.ThrowIfCancellationRequested();

					using(StopwatchScope stopwatch = new StopwatchScope(
						nameof(ContentClientConsoleController),
						"Start run..",
						"Success! Taking a break before next iteration.")) {
						bool isIncremental = false;
						if(curWriteMode == WriteMode.Full) { isIncremental = false; } 
						else { isIncremental = true; }

						await TaskExt.RetryAsync(
							() => _clientObject.WriteToVideosDBUsingCreatorsDBAsync(
								isIncremental, cancellationToken),
							TimeSpan.FromSeconds(3), 3, cancellationToken
						);
					}

					if(DateTime.Now.Subtract(_lastFullRun).Days > 0) {
						_writeMode.Push(WriteMode.Full);
						_lastFullRun = DateTime.Now;
					} else {
						_writeMode.Push(WriteMode.Incremental);
					}

					await Task.Delay(TimeSpan.FromSeconds(_settings.iterationGapAmount), cancellationToken);
				}
			}
		}

		private void RunTask(Func<CancellationToken, Task> taskFactory)
		{
			CancellationTokenSourceFactory.CancelAndCreateCancellationTokenSource(ref _cts);

			_isRunning = true;
			TaskExt.FireForget(Execute(), (Exception e) => { _isRunning = false; });

			async Task Execute()
			{
				await taskFactory(_cts.Token);
				_isRunning = false;
			}
		}

		private void Start()
		{
			// Setting idle frame rate because this is headless and 
			// we don't really need to run at 60 FPS.
			// https://forum.unity.com/threads/headless-server-high-cpu-usage.441727/#post-2858772
			Application.targetFrameRate = 1;

			LoadSettings();
			Run(_settings.isStartIncremental);
		}
	}
}
