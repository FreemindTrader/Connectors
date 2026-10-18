namespace StockSharp.Connectors.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;
using StockSharp.OkexHistory;

[TestClass]
public class OkexHistoryTests : HistoryTestBase
{
	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task OkxHistLookupAll()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAdapter<OkexHistoryMessageAdapter>(), LookupAll).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 300);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task OkxHistLookupLimit()
	{
		SkipUnlessLive();

		const int count = 300;

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAdapter<OkexHistoryMessageAdapter>(), new() { Count = count }).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistTicksSpot()
	{
		SkipUnlessLive();

		var secId = $"BTC-USD@{BoardCodes.Okex}".ToSecurityId();
		// Use a recent date that should have data available
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.EndOfDay();

		const int maxCount = 1000;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is ExecutionMessage exec && exec.DataTypeEx == DataType.Ticks)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 tick but got 0. All messages: {msgTypes.JoinCommaSpace()}");
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistTicksSwap()
	{
		SkipUnlessLive();

		var secId = $"BTC-USD@{BoardCodes.Okex}".ToSecurityId();
		// Use a recent date that should have data available
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.EndOfDay();

		const int maxCount = 1000;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityType = SecurityTypes.Swap,
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is ExecutionMessage exec && exec.DataTypeEx == DataType.Ticks)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 tick but got 0. All messages: {msgTypes.JoinCommaSpace()}");
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistTicksFutures()
	{
		SkipUnlessLive();

		var secId = $"BTC-USD@{BoardCodes.Okex}".ToSecurityId();
		// Use a recent date that should have data available
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.EndOfDay();
		// Use an expiry date that should have been available (quarterly futures)
		//var expiryDate = new DateTime(2025, 12, 27).UtcKind();

		const int maxCount = 1000;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			SecurityType = SecurityTypes.Future,
			//ExpiryDate = expiryDate,
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is ExecutionMessage exec && exec.DataTypeEx == DataType.Ticks)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 tick but got 0. All messages: {msgTypes.JoinCommaSpace()}");
		AreEqual(maxCount, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistCandlesSpot()
	{
		SkipUnlessLive();

		var secId = $"BTC-USDT@{BoardCodes.Okex}".ToSecurityId();
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.AddDays(1);

		const int maxCount = 100;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is TimeFrameCandleMessage)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 candle but got 0. All messages: {msgTypes.JoinCommaSpace()}");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistCandlesSwap()
	{
		SkipUnlessLive();

		var secId = $"BTC-USDT@{BoardCodes.Okex}".ToSecurityId();
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.AddDays(1);

		const int maxCount = 100;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityType = SecurityTypes.Swap,
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is TimeFrameCandleMessage)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 candle but got 0. All messages: {msgTypes.JoinCommaSpace()}");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistCandlesFutures()
	{
		SkipUnlessLive();

		var secId = $"BTC-USDT@{BoardCodes.Okex}".ToSecurityId();
		var from = new DateTime(2025, 12, 01).UtcKind();
		var to = from.AddDays(1);
		//var expiryDate = new DateTime(2025, 12, 27).UtcKind();

		const int maxCount = 100;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityType = SecurityTypes.Future,
			//ExpiryDate = expiryDate,
			From = from,
			To = to,
			Count = maxCount,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is TimeFrameCandleMessage)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 candle but got 0. All messages: {msgTypes.JoinCommaSpace()}");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistOrderBookSpot()
	{
		SkipUnlessLive();

		var secId = $"BTC-USDT@{BoardCodes.Okex}".ToSecurityId();
		var from = new DateTime(2026, 01, 18).UtcKind();
		var to = from.AddHours(1); // Only 1 hour of data to limit download size

		const int maxCount = 100;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			From = from,
			To = to,
			Count = maxCount,
			MaxDepth = 20,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is QuoteChangeMessage)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 order book snapshot but got 0. All messages: {msgTypes.JoinCommaSpace()}");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(180000, CooperativeCancellation = true)]
	public async Task OkxHistOrderBookSwap()
	{
		SkipUnlessLive();

		var secId = $"BTC-USDT@{BoardCodes.Okex}".ToSecurityId();
		var from = new DateTime(2026, 01, 18).UtcKind();
		var to = from.AddHours(1);

		const int maxCount = 100;
		var allMessages = new List<Message>();
		var adapter = CreateAdapter<OkexHistoryMessageAdapter>();

		var counter = 0;

		await foreach (var msg in adapter.ConnectAndDownloadAsync<Message>(new MarketDataMessage
		{
			SecurityId = secId,
			IsSubscribe = true,
			DataType2 = DataType.MarketDepth,
			SecurityType = SecurityTypes.Swap,
			From = from,
			To = to,
			Count = maxCount,
			MaxDepth = 20,
		}).WithCancellation(CancellationToken))
		{
			allMessages.Add(msg);
			if (msg is QuoteChangeMessage)
				counter++;
		}

		var msgTypes = allMessages.GroupBy(m => m.GetType().Name).Select(g => $"{g.Key}:{g.Count()}");
		IsGreater(counter, 0, $"Expected at least 1 order book snapshot but got 0. All messages: {msgTypes.JoinCommaSpace()}");
	}
}
