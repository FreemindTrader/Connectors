namespace StockSharp.Connectors.Tests;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.PolygonIO.Native;

[TestClass]
public class PolygonHistoryRegressionTests : BaseTestClass
{
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupReadsNamedFieldsAndFollowsPagination()
	{
		var requests = 0;
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(HttpStatusCode.OK)
		{
			Content = new StringContent(++requests == 1
				? "{\"status\":\"OK\",\"results\":[{\"ticker\":\"AAPL\",\"primary_exchange\":\"XNAS\"}],\"next_url\":\"https://api.massive.com/v3/reference/tickers?cursor=next\"}"
				: "{\"status\":\"OK\",\"results\":[{\"ticker\":\"MSFT\",\"primary_exchange\":\"XNAS\"}]}"),
		}));
		var client = new RestClient(http, "massive.com");
		var tickers = await client.GetTickers(TimeSpan.Zero, null, null, true, 1, "test-key", CancellationToken).ToArrayAsync(CancellationToken);

		AreEqual(2, requests);
		AreEqual(2, tickers.Length);
		AreEqual("AAPL", tickers[0].Code);
		AreEqual("MSFT", tickers[1].Code);
		AreEqual("XNAS", tickers[0].PrimaryExchange);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CandlesUseExchangeFieldNamesAndMillisecondTimestamps()
	{
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(HttpStatusCode.OK)
		{
			Content = new StringContent("{\"status\":\"OK\",\"results\":[{\"t\":1704067200000,\"o\":100,\"h\":120,\"l\":90,\"c\":110,\"v\":25}]}"),
		}));
		var client = new RestClient(http, "massive.com");
		var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var bars = await client.GetBars(TimeSpan.Zero, "AAPL", "minute", 1, from, from.AddMinutes(1), true, "asc", 1, "test-key", CancellationToken)
			.ToArrayAsync(CancellationToken);

		AreEqual(1, bars.Length);
		AreEqual(from, bars[0].Time);
		AreEqual(100d, bars[0].Open);
		AreEqual(110d, bars[0].Close);
		AreEqual(25d, bars[0].Volume);
	}

	[TestMethod]
	[DataRow(HttpStatusCode.Forbidden)]
	[DataRow(HttpStatusCode.OK)]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ApiErrorsExplainMissingEntitlementWithoutLeakingKey(HttpStatusCode statusCode)
	{
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(statusCode)
		{
			Content = new StringContent("{\"status\":\"NOT_AUTHORIZED\",\"message\":\"Your plan does not include this data.\"}"),
		}));
		var client = new RestClient(http, "massive.com");
		var error = await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await foreach (var _ in client.GetTickers(TimeSpan.Zero, null, "AAPL", true, 1, "private-api-key", CancellationToken))
			{
			}
		});
		IsTrue(error.ToString().Contains("Your plan does not include this data."));
		IsFalse(error.ToString().Contains("private-api-key"));
	}
}
