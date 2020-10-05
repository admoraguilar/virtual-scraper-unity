using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Midnight;
using Midnight.Concurrency;
using System;

namespace Holoverse.Scraper.UI
{
	public class ContentDatabaseClientUIController : MonoBehaviour
	{
		[SerializeField]
		private ContentDatabaseClientObject _contentDatabase = null;

		[Header("UI")]
		[SerializeField]
		private TMP_Text _successText = null;

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
		
		private int successCount
		{
			get => _successCount;
			set {
				_successCount = value;
				if(_successText != null) {
					_successText.text = $"Success Count: {_successCount}";
				}
			}
		}
		private int _successCount = 0;

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

		public void Run()
		{
			if(isRunning) { return; }
			isRunning = true;

			TaskExt.FireForget(Execute());

			async Task Execute()
			{
				while(isRunning) {
					MLog.Log(nameof(ContentDatabaseClientUIController), "Start running content database client from UI!");

					using(StopwatchScope stopwatch = new StopwatchScope()) {
						await TaskExt.Retry(
							() => _contentDatabase.client.GetAndWriteToVideosCollectionFromCreatorsCollection(),
							TimeSpan.FromSeconds(3),
							100
						);
						lastRunDetails = $"{stopwatch.elapsed.Duration()} - {DateTime.Now}";
					}
					
					successCount++;

					MLog.Log(nameof(ContentDatabaseClientUIController), "Sucess! Taking a break before next iteration.");
					await Task.Delay(TimeSpan.FromSeconds(iterationGapAmount));
				}

				successCount = 0;
			}
		}

		public void Cancel()
		{
			isRunning = false;
			
		}

		private void OnIterationGapInputFieldValueChanged(string value)
		{
			_iterationGapAmount = float.Parse(value);
		}

		private void OnEnable()
		{
			_iterationGapAmountInputField.onValueChanged.AddListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.AddListener(Run);
			_cancelButton.onClick.AddListener(Cancel);
		}

		private void OnDisable()
		{
			_iterationGapAmountInputField.onValueChanged.RemoveListener(OnIterationGapInputFieldValueChanged);
			_runButton.onClick.RemoveListener(Run);
			_cancelButton.onClick.RemoveListener(Cancel);
		}

		private void Start()
		{
			iterationGapAmount = float.Parse(_iterationGapAmountInputField.text);
			successCount = 0;
			isRunning = false;
			lastRunDetails = "--";
		}
	}
}