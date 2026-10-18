namespace StockSharp.Connectors.Tests;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.IQFeed;

[TestClass]
public class IQFeedHistoryTests : HistoryTestBase
{
	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task IqFeedLookup()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in IQFeedSecurityParser.ParseFromUrlAsync().WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 10000);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task IqFeedLookupLimit()
	{
		SkipUnlessLive();

		const int count = 100;

		var counter = 0;
		await foreach (var _ in IQFeedSecurityParser.ParseFromUrlAsync(maxCount: count).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}
}
