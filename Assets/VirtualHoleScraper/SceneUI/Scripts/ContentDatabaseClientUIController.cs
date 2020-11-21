﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Midnight;
using Midnight.Tasks;

namespace VirtualHole.Scraper.UI
{
	public class ContentDatabaseClientUIController : MonoBehaviour
	{
		private enum WriteMode
		{
			Full,
			Incremental
		}

		[SerializeField]
		private ContentClientObject _clientObject = null;

		[Header("UI")]
		[SerializeField]
		private TMP_Text _incrementalScanCountText = null;

		[SerializeField]
		private TMP_Text _fullScanCountText = null;

		[SerializeField]
		private TMP_Text _isRunningText = null;

		[SerializeField]
		private TMP_Text _lastRunText = null;

		[SerializeField]
		private TMP_InputField _iterationGapAmountInputField = null;

		[SerializeField]
		private Toggle _useProxiesToggle = null;

		[SerializeField]
		private Button _runButton = null;

		[SerializeField]
		private Button _runStartIncrementalButton = null;

		[SerializeField]
		private Button _cancelButton = null;

		[SerializeField]
		private Button _showDebugButton = null;

		private float iterationGapAmount
		{
			get => _iterationGapAmount;
			set {
				_iterationGapAmount = value;
				if(_iterationGapAmountInputField != null) {
					_iterationGapAmountInputField.text = _iterationGapAmount.ToString();
				}
			}
		}
		private float _iterationGapAmount = 5f;
		
		private int incrementalScanCount
		{
			get => _incrementalScanCount;
			set {
				_incrementalScanCount = value;
				if(_incrementalScanCountText != null) {
					_incrementalScanCountText.text = $"Incremental Scan Count: {_incrementalScanCount}";
				}
			}
		}
		private int _incrementalScanCount = 0;

		private int fullScanCount
		{
			get => _fullScanCount;
			set {
				_fullScanCount = value;
				if(_fullScanCountText != null) {
					_fullScanCountText.text = $"Full Scan Count: {_fullScanCount}";
				}
			}
		}
		private int _fullScanCount = 0;

		private bool isRunning
		{
			get => _isRunning;
			set {
				_isRunning = value;
				if(_isRunningText != null) {
					_isRunningText.text = $"Is Running: {_isRunning}";
				}
			}
 		}
		private bool _isRunning = false;

		public string lastRunDetails
		{
			get => _lastRunDetails;
			set {
				_lastRunDetails = value;
				if(_lastRunText != null) {
					_lastRunText.text = $"Last Run: {_lastRunDetails}";
				}
			}
		}
		private string _lastRunDetails = string.Empty;

		private Stack<WriteMode> _writeMode = new Stack<WriteMode>();
		private DateTime _lastFullRun = DateTime.MinValue;
		private CancellationTokenSource _cts = null;

		private void Run(bool isStartIncremental = false)
		{
			if(isRunning) { return; }
			isRunning = true;

			_clientObject.isUseProxy = _useProxiesToggle.isOn;

			incrementalScanCount = 0;
			fullScanCount = 0;

			if(isStartIncremental) { _writeMode.Push(WriteMode.Incremental); } 
			else { _writeMode.Push(WriteMode.Full); }
			
			_lastFullRun = DateTime.Now;
			RunTask(Execute);

			async Task Execute(CancellationToken cancellationToken = default)
			{
				while(isRunning) {
					WriteMode curWriteMode = _writeMode.Pop();
					cancellationToken.ThrowIfCancellationRequested();

					using(StopwatchScope stopwatch = new StopwatchScope(
						nameof(ContentDatabaseClientUIController),
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

						lastRunDetails = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}";
					}

					if(curWriteMode == WriteMode.Full) { fullScanCount++; }
					else { incrementalScanCount++; }

					if(DateTime.Now.Subtract(_lastFullRun).Days > 0) {
						_writeMode.Push(WriteMode.Full);
						_lastFullRun = DateTime.Now;
					} else {
						_writeMode.Push(WriteMode.Incremental);
					}

					await Task.Delay(TimeSpan.FromSeconds(iterationGapAmount), cancellationToken);
				}
			}
		}

		private void RunTask(Func<CancellationToken, Task> taskFactory)
		{
			CancellationTokenSourceExt.CancelAndCreate(ref _cts);

			isRunning = true;
			TaskExt.FireForget(Execute(), (Exception e) => { isRunning = false; });

			async Task Execute()
			{
				await taskFactory(_cts.Token);
				isRunning = false;
			}
		}

		private void Cancel()
		{
			CancellationTokenSourceExt.Cancel(ref _cts);
		}

		private void OnIterationGapInputFieldValueChanged(string value)
		{
			_iterationGapAmount = float.Parse(value);
		}

		private void OnTriggerRunButton()
		{
			Run();
		}

		private void OnTriggerRunStartIncrementalButton()
		{
			Run(true);
			MLog.Log(nameof(ContentDatabaseClientUIController), "Starting incremental...");
		}

		private void OnTriggerCancelButton()
		{
			MLog.LogWarning(nameof(ContentDatabaseClientUIController), $"Cancelling on-going tasks...");
			Cancel();
		}

		private void OnShowDebugButtonClicked()
		{
			SRDebug.Init();
			SRDebug.Instance.ShowDebugPanel();
		}

		private void OnUseProxiesToggleValueChanged(bool value)
		{
			_clientObject.isUseProxy = value;
		}

		private void OnEnable()
		{
			_iterationGapAmountInputField.onValueChanged.AddListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.AddListener(OnTriggerRunButton);
			_runStartIncrementalButton.onClick.AddListener(OnTriggerRunStartIncrementalButton);
			_cancelButton.onClick.AddListener(OnTriggerCancelButton);
			_showDebugButton.onClick.AddListener(OnShowDebugButtonClicked);

			_useProxiesToggle.onValueChanged.AddListener(OnUseProxiesToggleValueChanged);
		}

		private void OnDisable()
		{
			_iterationGapAmountInputField.onValueChanged.RemoveListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.RemoveListener(OnTriggerRunButton);
			_runStartIncrementalButton.onClick.RemoveListener(OnTriggerRunStartIncrementalButton);
			_cancelButton.onClick.RemoveListener(OnTriggerCancelButton);
			_showDebugButton.onClick.RemoveListener(OnShowDebugButtonClicked);

			_useProxiesToggle.onValueChanged.RemoveListener(OnUseProxiesToggleValueChanged);
		}

		private void Start()
		{
			iterationGapAmount = float.Parse(_iterationGapAmountInputField.text);
			incrementalScanCount = 0;
			fullScanCount = 0;
			isRunning = false;
			lastRunDetails = "--";

			_useProxiesToggle.isOn = _clientObject.isUseProxy;
		}
	}
}