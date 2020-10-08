using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Midnight;
using Midnight.Concurrency;

namespace Holoverse.Scraper.UI
{
	public class ContentDatabaseClientUIController : MonoBehaviour
	{
		private enum WriteMode
		{
			Full,
			Incremental
		}

		[SerializeField]
		private ContentDatabaseClientObject _contentDatabase = null;

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
		private Button _runButton = null;

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

		private void Run()
		{
			if(isRunning) { return; }
			isRunning = true;

			incrementalScanCount = 0;
			fullScanCount = 0;

			_writeMode.Push(WriteMode.Full);
			_lastFullRun = DateTime.Now;

			CancellableFireForget(
				Execute,
				(Exception e) => {
					if(e is OperationCanceledException) {
						isRunning = false;
					}
				});

			async Task Execute(CancellationToken cancellationToken = default)
			{
				while(isRunning) {
					WriteMode curWriteMode = _writeMode.Pop();

					cancellationToken.ThrowIfCancellationRequested();
					MLog.Log(
						nameof(ContentDatabaseClientUIController), 
						$"[WriteMode: {curWriteMode}] Start running content database client from UI!"
					);

					using(StopwatchScope stopwatch = new StopwatchScope()) {
						bool isIncremental = false;
						if(curWriteMode == WriteMode.Full) { isIncremental = false; }
						else { isIncremental = true; }

						await TaskExt.RetryAsync(
							() => _contentDatabase.client.GetAndWriteToVideosCollectionFromCreatorsCollection(
								isIncremental, cancellationToken),
							TimeSpan.FromSeconds(3),
							100, cancellationToken
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

					MLog.Log(nameof(ContentDatabaseClientUIController), "Sucess! Taking a break before next iteration.");
					await Task.Delay(TimeSpan.FromSeconds(iterationGapAmount), cancellationToken);
				}
			}
		}

		private void CancellableFireForget(
			Func<CancellationToken, Task> task, Action<Exception> onException = null)
		{
			Cancel();

			_cts = new CancellationTokenSource();
			TaskExt.FireForget(task(_cts.Token), onException);
		}

		private void Cancel()
		{
			if(_cts != null) {
				_cts.Cancel();
				_cts.Dispose();

				MLog.LogWarning(nameof(ContentDatabaseClientUIController), $"Cancelled on-going tasks.");
				_cts = null;
			}
		}

		private void OnIterationGapInputFieldValueChanged(string value)
		{
			_iterationGapAmount = float.Parse(value);
		}

		private void OnShowDebugButton()
		{
			FindObjectOfType<Reporter>().doShow();
		}

		private void OnEnable()
		{
			_iterationGapAmountInputField.onValueChanged.AddListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.AddListener(Run);
			_cancelButton.onClick.AddListener(Cancel);
			_showDebugButton.onClick.AddListener(OnShowDebugButton);
		}

		private void OnDisable()
		{
			_iterationGapAmountInputField.onValueChanged.RemoveListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.RemoveListener(Run);
			_cancelButton.onClick.RemoveListener(Cancel);
			_showDebugButton.onClick.RemoveListener(OnShowDebugButton);
		}

		private void Start()
		{
			iterationGapAmount = float.Parse(_iterationGapAmountInputField.text);
			incrementalScanCount = 0;
			fullScanCount = 0;
			isRunning = false;
			lastRunDetails = "--";
		}
	}
}