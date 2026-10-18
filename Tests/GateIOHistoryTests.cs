namespace StockSharp.Connectors.Tests;

using System;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.GateIOHistory;
using StockSharp.Messages;

[TestClass]
public class GateIOHistoryTests : HistoryTestBase
{
	private static IMessageAdapter CreateGateIOHistory()
		=> CreateAdapter<GateIOHistoryMessageAdapter>(a => a.CheckDates = false);

	private static IMessageAdapter CreateGateIOHistoryCheckDates()
		=> CreateAdapter<GateIOHistoryMessageAdapter>(a => a.CheckDates = true);

	#region Lookup

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistLookupAll()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateGateIOHistory(), LookupAll).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 1000);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistLookupLimit()
	{
		SkipUnlessLive();

		const int count = 200;
		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateGateIOHistory(), new() { Count = count }).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}

	#endregion

	#region Spot

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistTicksSpot()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIO}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 5000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadTicks(CreateGateIOHistory(), secId, from, to, maxCount).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task GateIOHistCandlesSpot()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreateGateIOHistory, $"BTC_USDT@{BoardCodes.GateIO}".ToSecurityId(), default, new DateTime(2024, 06, 01).UtcKind(), new()
		{
			{ TimeSpan.FromMinutes(1), 1439 },
			{ TimeSpan.FromMinutes(5), 864 },
			{ TimeSpan.FromHours(1), 72 },
			{ TimeSpan.FromDays(1), 7 },
		}, CancellationToken);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistOrderBookSpot()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIO}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 1000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadOrderBook(CreateGateIOHistory(), secId, from, to, maxCount).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	#endregion

	#region Perpetual Futures

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistTicksPerpetual()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIOPerpetual}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 5000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadTicks(CreateGateIOHistory(), secId, from, to, maxCount, SecurityTypes.Future).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task GateIOHistCandlesPerpetual()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreateGateIOHistory, $"BTC_USDT@{BoardCodes.GateIOPerpetual}".ToSecurityId(), SecurityTypes.Future, new DateTime(2024, 06, 01).UtcKind(), new()
		{
			{ TimeSpan.FromMinutes(1), 1419 },
			{ TimeSpan.FromMinutes(5), 864 },
			{ TimeSpan.FromHours(1), 72 },
			{ TimeSpan.FromDays(1), 7 },
		}, CancellationToken);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task GateIOHistOrderBookPerpetual()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIOPerpetual}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 1000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadOrderBook(CreateGateIOHistory(), secId, from, to, maxCount, SecurityTypes.Future).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	#endregion

	// Delivery and options are not covered: the GateIO archive holds no trades or candles for
	// those markets (only order books for delivery_usdt).

	#region CheckDates

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(300000, CooperativeCancellation = true)]
	public async Task GateIOHistTicksSpotCheckDates()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIO}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 1000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadTicks(CreateGateIOHistoryCheckDates(), secId, from, to, maxCount).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(300000, CooperativeCancellation = true)]
	public async Task GateIOHistCandlesSpotCheckDates()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIO}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 100;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadCandles(CreateGateIOHistoryCheckDates(), secId, TimeSpan.FromHours(1), from, to, maxCount).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		IsGreater(counter, 0, "No candles found");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(300000, CooperativeCancellation = true)]
	public async Task GateIOHistTicksPerpetualCheckDates()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIOPerpetual}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 1000;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadTicks(CreateGateIOHistoryCheckDates(), secId, from, to, maxCount, SecurityTypes.Future).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(300000, CooperativeCancellation = true)]
	public async Task GateIOHistCandlesPerpetualCheckDates()
	{
		SkipUnlessLive();

		var secId = $"BTC_USDT@{BoardCodes.GateIOPerpetual}".ToSecurityId();
		var from = new DateTime(2024, 06, 01).UtcKind();
		var to = from.AddDays(1);
		const int maxCount = 100;
		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadCandles(CreateGateIOHistoryCheckDates(), secId, TimeSpan.FromHours(1), from, to, maxCount, secType: SecurityTypes.Future).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			counter++;
		}
		IsGreater(counter, 0, "No candles found");
	}

	#endregion
}
