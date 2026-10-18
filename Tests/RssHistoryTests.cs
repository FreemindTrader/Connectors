namespace StockSharp.Connectors.Tests;

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;
using StockSharp.Rss;

[TestClass]
public class RssHistoryTests : HistoryTestBase
{
	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task Rss()
	{
		SkipUnlessLive();

		foreach (var addr in RssAddresses.All)
		{
			var adapter = CreateAdapter<RssMessageAdapter>();
			adapter.FindAdapter<RssMessageAdapter>().Address = addr;

			var now = DateTime.UtcNow;

			var mdMsg = new MarketDataMessage
			{
				IsSubscribe = true,
				DataType2 = DataType.News,
				From = now.AddDays(-90),
				To = now,
			};

			var counter = 0;
			await foreach (var _ in Download<NewsMessage>(adapter, mdMsg).WithCancellation(CancellationToken))
				counter++;
			IsGreater(counter, 0, $"No news from {addr}.");
		}
	}
}
