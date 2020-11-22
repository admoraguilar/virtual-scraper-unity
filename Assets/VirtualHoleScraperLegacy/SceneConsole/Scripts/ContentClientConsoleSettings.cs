using System;

namespace VirtualHole.LegacyScraper
{
	public class ContentClientConsoleSettings
	{
		public float iterationGapAmount = 0f;
		public bool useProxies = true;
		public bool isStartIncremental = false;

		public override string ToString()
		{
			return $"{nameof(iterationGapAmount)}: {iterationGapAmount} {Environment.NewLine}" +
				$"{nameof(useProxies)}: {useProxies} {Environment.NewLine}" +
				$"{nameof(isStartIncremental)}: {isStartIncremental} {Environment.NewLine}";
		}
	}
}
