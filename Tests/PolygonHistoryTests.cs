namespace StockSharp.Connectors.Tests;

using System;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.PolygonIO;

[TestClass]
[DoNotParallelize]
public class PolygonHistoryTests : HistoryTestBase
{
	// The token comes from the environment. A machine without the variable reports the tests
	// inconclusive rather than failing them on a null token.
	private static IMessageAdapter CreatePolygon()
	{
		var token = Env("POLYGON_TOKEN");

		if (token.IsEmpty())
			Inconclusive("POLYGON_TOKEN is not set.");

		return CreateAdapter<PolygonIOMessageAdapter>(a =>
		{
			a.Token = token.Secure();
			a.IterationInterval = TimeSpan.FromSeconds(0);
			a.ConnectionType = PolygonIOConnectionTypes.History;
		});
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonLookup()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreatePolygon(), LookupAll).WithCancellation(CancellationToken))
			counter++;
		IsLess(counter, 60000);
		IsGreater(counter, 40000);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonLookupMulti()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreatePolygon(), new() { SecurityId = new() { SecurityCode = "AAPL" }, SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Currency] }).WithCancellation(CancellationToken))
			counter++;
		IsLess(counter, 10);
		IsGreater(counter, 0);

		counter = 0;
		await foreach (var _ in DownloadSecurities(CreatePolygon(), new() { SecurityId = new() { SecurityCode = "A" }, SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Currency], Count = 200 }).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 100);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonTicksAapl()
	{
		SkipUnlessLive();

		var secId = new Security
		{
			Id = $"AAPL@{BoardCodes.StockSharp}",
		}.ToSecurityId();

		async Task download(DateTime from, int count)
		{
			var counter = 0;
			var lastTime = DateTime.MinValue;
			await foreach (var msg in DownloadTicks(CreatePolygon(), secId, from.UtcKind(), DateTime.UtcNow, count).WithCancellation(CancellationToken))
			{
				Validate(msg, secId, from.UtcKind(), DateTime.UtcNow, ref lastTime);
				counter++;
			}
			AreEqual(count, counter);
		}

		await download(new DateTime(2015, 1, 1), 60000);
		await download(new DateTime(1975, 1, 1), 1);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonQuotesAapl()
	{
		SkipUnlessLive();

		var secId = new Security
		{
			Id = $"AAPL@{BoardCodes.StockSharp}",
		}.ToSecurityId();

		async Task download(DateTime from, int count)
		{
			var counter = 0;
			var lastTime = DateTime.MinValue;
			await foreach (var msg in DownloadLevel1(CreatePolygon(), secId, from.UtcKind(), DateTime.UtcNow, count).WithCancellation(CancellationToken))
			{
				Validate(msg, secId, from.UtcKind(), DateTime.UtcNow, ref lastTime);
				counter++;
			}
			AreEqual(count, counter);
		}

		await download(new DateTime(2015, 1, 1), 60000);
		await download(new DateTime(1975, 1, 1), 1);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task PolygonCandlesAapl()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreatePolygon, $"AAPL@{BoardCodes.StockSharp}".ToSecurityId(), default, new DateTime(2022, 11, 18).UtcKind(), new()
		{
			{ TimeSpan.FromMinutes(1), 760 },
			{ TimeSpan.FromMinutes(30), 34 },
			{ TimeSpan.FromHours(1), 17 },
			{ TimeSpan.FromDays(1), 4 },
			{ TimeSpan.FromDays(7), 4 },
			{ TimeSpan.FromDays(30), 2 },
		}, CancellationToken);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonDiv()
	{
		SkipUnlessLive();

		var secId = new Security { Id = "AAPL@STSH" }.ToSecurityId();
		var from = new DateTime(2010, 1, 30);
		var to = new DateTime(PrevYear, 5, 31);

		var lastTime = ResetTimeOrder();
		var counter = 0;
		await foreach (var msg in DownloadLevel1(CreatePolygon(), secId, from, to, fields: [Level1Fields.Dividend]).WithCancellation(CancellationToken))
		{
			Validate(msg, secId, from, to, ref lastTime);
			IsGreater((decimal)msg.Changes[Level1Fields.Dividend], 0m);
			counter++;
		}
		IsInRange(counter, 30, 50);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task PolygonNews()
	{
		SkipUnlessLive();

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.News,
			From = new DateTime(2010, 1, 1),
			To = DateTime.UtcNow,
			Count = 100,
		};

		var counter = 0;
		await foreach (var _ in Download<NewsMessage>(CreatePolygon(), mdMsg).WithCancellation(CancellationToken))
			counter++;
		AreEqual(100, counter);
	}
}
