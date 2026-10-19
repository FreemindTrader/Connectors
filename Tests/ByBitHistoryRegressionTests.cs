namespace StockSharp.Connectors.Tests;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.ByBitHistory;
using StockSharp.Messages;

[TestClass]
public class ByBitHistoryRegressionTests : HistoryTestBase
{
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupPropagatesHttpFailure()
	{
		var adapter = CreateAdapter<ByBitHistoryMessageAdapter>();
		using var http = new HttpClient(new HistoryHttpHandler(_ => new(HttpStatusCode.Forbidden)
		{
			Content = new StringContent("Access denied."),
		}));
		HistoryHttpHandler.ReplaceClient(adapter.FindAdapter<ByBitHistoryMessageAdapter>(), http);

		await ThrowsAsync<HttpRequestException>(async () =>
		{
			await foreach (var _ in DownloadSecurities(adapter, LookupAll).WithCancellation(CancellationToken))
			{
			}
		});
	}
}
