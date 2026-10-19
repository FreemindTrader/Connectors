namespace StockSharp.Connectors.Tests;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.AlphaVantage.Native;

[TestClass]
public class AlphaVantageRegressionTests : BaseTestClass
{
	[TestMethod]
	[DataRow("Information")]
	[DataRow("Note")]
	[DataRow("Error Message")]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupReportsApiErrors(string field)
	{
		await using var server = new HistoryHttpServer(() => $"{{\"{field}\":\"Request limit reached.\"}}", CancellationToken);
		using var client = new AlphaClient(server.Address.ToString(), TimeSpan.FromSeconds(1));
		var error = await ThrowsAsync<InvalidOperationException>(async () =>
			await client.Lookup("AAPL", "test-key".Secure(), CancellationToken));
		IsTrue(error.Message.Contains("Request limit reached."));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task RequestsFromDifferentClientsAreSpaced()
	{
		var timer = Stopwatch.StartNew();
		TimeSpan? lastRequest = null;
		var interval = TimeSpan.MaxValue;
		await using var server = new HistoryHttpServer(() =>
		{
			if (lastRequest is { } last)
				interval = timer.Elapsed - last;
			lastRequest = timer.Elapsed;
			return "{\"bestMatches\":[]}";
		}, CancellationToken);
		using var first = new AlphaClient(server.Address.ToString(), TimeSpan.FromSeconds(1));
		using var second = new AlphaClient(server.Address.ToString(), TimeSpan.FromSeconds(1));
		await first.Lookup("AAPL", "test-key".Secure(), CancellationToken);
		await second.Lookup("AAPL", "test-key".Secure(), CancellationToken);
		IsTrue(interval >= TimeSpan.FromSeconds(1), $"Requests were only {interval} apart.");
	}
}
