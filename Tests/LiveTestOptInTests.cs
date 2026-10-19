namespace StockSharp.Connectors.Tests;

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using Moq.Protected;

using StockSharp.Messages;

[TestClass]
[DoNotParallelize]
[CLSCompliant(false)]
public class LiveTestOptInTests : BaseTestClass
{
	private sealed class CandleAdapter : HistoricalMessageAdapter
	{
		public CandleAdapter() : base(new IncrementalIdGenerator())
		{
			this.AddMarketDataSupport();
			this.AddSupportedCandleTimeFrames([TimeSpan.FromMinutes(1)]);
		}

		protected override async ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage message, CancellationToken cancellationToken)
		{
			await SendSubscriptionReplyAsync(message.TransactionId, cancellationToken);
			await TimeSpan.FromMilliseconds(1200).Delay(cancellationToken);
			await SendOutMessageAsync(new TimeFrameCandleMessage
			{
				OriginalTransactionId = message.TransactionId,
				SecurityId = message.SecurityId,
				DataType = message.DataType2,
				OpenTime = message.From ?? DateTime.UtcNow,
				OpenPrice = 1,
				HighPrice = 1,
				LowPrice = 1,
				ClosePrice = 1,
				State = CandleStates.Finished,
			}, cancellationToken);
			await SendSubscriptionFinishedAsync(message.TransactionId, cancellationToken);
		}
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CandleCheckRunsWhenHeartbeatIsDisabled()
	{
		var adapter = new CandleAdapter();
		var check = new Mock<LiveMarketDataTestBase> { CallBase = true };
		check.Protected().Setup<MessageAdapter>("CreateAdapter").Returns(adapter);
		check.Protected().SetupGet<SecurityId>("TestSecurityId").Returns(new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" });
		check.Object.TestContext = TestContext;

		try
		{
			await check.Object.Candles();
		}
		catch (AssertInconclusiveException ex)
		{
			Fail(ex.Message);
		}
	}

	[TestMethod]
	[DataRow("", false)]
	[DataRow("0", false)]
	[DataRow("1", true)]
	[Timeout(10000)]
	public void LiveTestsHonorExplicitOptIn(string setting, bool expectedToRun)
	{
		var previous = Environment.GetEnvironmentVariable("STOCKSHARP_LIVE_TESTS");
		try
		{
			Environment.SetEnvironmentVariable("STOCKSHARP_LIVE_TESTS", setting);
			var probe = new AlorHistoryMarketDataTests();
			var skipped = false;
			foreach (var method in probe.GetType().GetMethods().Where(m => m.IsDefined(typeof(TestInitializeAttribute))))
			{
				try
				{
					method.Invoke(probe, null);
				}
				catch (TargetInvocationException ex) when (ex.InnerException is AssertInconclusiveException)
				{
					skipped = true;
				}
			}

			AreEqual(expectedToRun, !skipped);
		}
		finally
		{
			Environment.SetEnvironmentVariable("STOCKSHARP_LIVE_TESTS", previous);
		}
	}
}
