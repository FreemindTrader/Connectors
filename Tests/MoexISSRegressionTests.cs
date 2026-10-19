namespace StockSharp.Connectors.Tests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using StockSharp.MoexISS.Native.Requests;

[TestClass]
public class MoexISSRegressionTests : BaseTestClass
{
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EmptyDividendTableIsAValidResponse()
	{
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(HttpStatusCode.OK)
		{
			Content = new StringContent("{\"dividends\":{\"metadata\":{},\"columns\":[\"registryclosedate\",\"value\",\"currencyid\"],\"data\":[]}}"),
		})) { BaseAddress = new("https://iss.moex.com/iss/") };
		var response = await new SecurityDividendsRequest(http, "SBER").Get(CancellationToken);
		AreEqual(0, response.Dividends.Data.Count);
		AreEqual(3, response.Dividends.Columns.Count);
	}

	[TestMethod]
	[DataRow("{\"description\":{},\"boards\":{}}")]
	[DataRow("{\"dividends\":null}")]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task MissingDividendTableReportsUnavailableData(string response)
	{
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(HttpStatusCode.OK)
		{
			Content = new StringContent(response),
		})) { BaseAddress = new("https://iss.moex.com/iss/") };
		var error = await ThrowsAsync<JsonSerializationException>(async () =>
			await new SecurityDividendsRequest(http, "SBER").Get(CancellationToken));
		IsTrue(error.Message.Contains("dividends", StringComparison.OrdinalIgnoreCase));
	}
}
