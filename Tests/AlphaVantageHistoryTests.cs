namespace StockSharp.Connectors.Tests;

using System;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.AlphaVantage;
using StockSharp.Messages;

[TestClass]
[DoNotParallelize]
public class AlphaVantageHistoryTests : HistoryTestBase
{
	// The token comes from the environment. A machine without the variable reports the tests
	// inconclusive rather than failing them on a null token.
	private static IMessageAdapter CreateAlphaVantage()
	{
		var token = Env("ALPHAVANTAGE_TOKEN");

		if (token.IsEmpty())
			Inconclusive("ALPHAVANTAGE_TOKEN is not set.");

		return CreateAdapter<AlphaVantageMessageAdapter>(a => a.Token = token.Secure());
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupMulti()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityId = new() { SecurityCode = "A" }, SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Currency] }).WithCancellation(CancellationToken))
			counter++;
		IsLess(counter, 40);
		IsGreater(counter, 20);

		counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Currency] }).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 100);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupAapl()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityId = new() { SecurityCode = "AAPL" } }).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 3);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupApplLimit()
	{
		SkipUnlessLive();

		const int count = 1;

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityId = new() { SecurityCode = "AAPL" }, Count = count }).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupCrypto()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityType = SecurityTypes.CryptoCurrency }).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 500);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupCryptoLimit()
	{
		SkipUnlessLive();

		const int count = 10;

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityType = SecurityTypes.CryptoCurrency, Count = count }).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupFX()
	{
		SkipUnlessLive();

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityType = SecurityTypes.Currency }).WithCancellation(CancellationToken))
			counter++;
		IsGreater(counter, 100);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public async Task AlphaVantageLookupFXLimit()
	{
		SkipUnlessLive();

		const int count = 10;

		var counter = 0;
		await foreach (var _ in DownloadSecurities(CreateAlphaVantage(), new() { SecurityType = SecurityTypes.Currency, Count = count }).WithCancellation(CancellationToken))
			counter++;
		AreEqual(count, counter);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task AlphaVantageCandlesAapl()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreateAlphaVantage, $"AAPL@{BoardCodes.AlphaVantage}".ToSecurityId(), default, new DateTime(2023, 08, 01).UtcKind(), new()
		{
			{ TimeSpan.FromMinutes(1), 956 },
			{ TimeSpan.FromMinutes(5), 576 },
			{ TimeSpan.FromMinutes(15), 192 },
			{ TimeSpan.FromMinutes(30), 96 },
			{ TimeSpan.FromHours(1), 48 },
			{ TimeSpan.FromDays(7), 4 },
			{ TimeSpan.FromDays(30), 2 },
		}, CancellationToken);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task AlphaVantageCandlesCrypto()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreateAlphaVantage, $"BTC/USD@{BoardCodes.AlphaVantage}".ToSecurityId(), SecurityTypes.CryptoCurrency, new DateTime(PrevYear, 08, 01).UtcKind(), new()
		{
			{ TimeSpan.FromDays(1), 8 },
			{ TimeSpan.FromDays(7), 4 },
			{ TimeSpan.FromDays(30), 2 },
		}, CancellationToken);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(120000, CooperativeCancellation = true)]
	public Task AlphaVantageCandlesFX()
	{
		SkipUnlessLive();

		return DownloadCandlesMultiTf(CreateAlphaVantage, $"EUR/USD@{BoardCodes.AlphaVantage}".ToSecurityId(), SecurityTypes.Currency, DateTime.UtcNow.AddDays(-30), new()
		{
			{ TimeSpan.FromDays(1), 5 },
			{ TimeSpan.FromDays(7), 5 },
			{ TimeSpan.FromDays(30), 2 },
		}, CancellationToken);
	}
}
