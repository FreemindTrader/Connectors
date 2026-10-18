namespace StockSharp.Connectors.Tests;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Connectors.Common;

[TestClass]
public class S3BucketHelperTests : HistoryTestBase
{
	private readonly HttpClient _client = new();

	[TestCleanup]
	public void Cleanup()
	{
		_client.Dispose();
	}

	#region GetDateRangeFromFoldersAsync

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFolders_GateIO_Spot_Deals()
	{
		SkipUnlessLive();

		var range = await S3BucketHelper.GetDateRangeFromFoldersAsync(
			_client,
			"gateio-public-data",
			"ap-northeast-1",
			"spot/deals/",
			"yyyyMM",
			CancellationToken);

		IsTrue(range.HasValue, "range should not be null");
		IsTrue(range.Value.min < new DateTime(2019, 1, 1), $"min {range.Value.min} should be before 2019");
		IsTrue(range.Value.max > new DateTime(2024, 1, 1), $"max {range.Value.max} should be after 2024");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFolders_GateIO_Spot_Candles()
	{
		SkipUnlessLive();

		var range = await S3BucketHelper.GetDateRangeFromFoldersAsync(
			_client,
			"gateio-public-data",
			"ap-northeast-1",
			"spot/candlesticks_1h/",
			"yyyyMM",
			CancellationToken);

		IsTrue(range.HasValue, "range should not be null");
		IsTrue(range.Value.min < new DateTime(2018, 1, 1), $"min {range.Value.min} should be before 2018");
		IsTrue(range.Value.max > new DateTime(2024, 1, 1), $"max {range.Value.max} should be after 2024");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFolders_NonExistentPrefix_ReturnsNull()
	{
		SkipUnlessLive();

		var range = await S3BucketHelper.GetDateRangeFromFoldersAsync(
			_client,
			"gateio-public-data",
			"ap-northeast-1",
			"nonexistent/prefix/that/does/not/exist/",
			"yyyyMM",
			CancellationToken);

		IsFalse(range.HasValue, "range should be null for non-existent prefix");
	}

	#endregion

	#region GetDateRangeFromFilesAsync

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFiles_Kucoin_Spot_Trades()
	{
		SkipUnlessLive();

		// Kucoin prefix format: data/{market}/daily/trades/{archiveSymbol}/{archiveSymbol}-trades-
		// Kucoin uses a custom domain for S3 access
		// Note: Archive symbols don't have dash (BTCUSDT instead of BTC-USDT)
		var range = await S3BucketHelper.GetDateRangeFromFilesAsync(
			_client,
			"k-line-history-data",
			"ap-northeast-1",
			"data/spot/daily/trades/BTCUSDT/BTCUSDT-trades-",
			"yyyy-MM-dd",
			CancellationToken,
			"https://historical-data.kucoin.com/");

		IsTrue(range.HasValue, "range should not be null");
		IsTrue(range.Value.min < new DateTime(2023, 1, 1), $"min {range.Value.min} should be before 2023");
		IsTrue(range.Value.max > new DateTime(2024, 1, 1), $"max {range.Value.max} should be after 2024");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFiles_Binance_Spot_Candles()
	{
		SkipUnlessLive();

		// Binance prefix format: data/spot/daily/klines/{symbol}/{tf}/{symbol}-{tf}-
		// Binance uses path-style S3 URL
		var range = await S3BucketHelper.GetDateRangeFromFilesAsync(
			_client,
			"data.binance.vision",
			"ap-northeast-1",
			"data/spot/daily/klines/BTCUSDT/1m/BTCUSDT-1m-",
			"yyyy-MM-dd",
			CancellationToken,
			"https://s3.ap-northeast-1.amazonaws.com/data.binance.vision/");

		IsTrue(range.HasValue, "range should not be null");
		IsTrue(range.Value.min < new DateTime(2020, 1, 1), $"min {range.Value.min} should be before 2020");
		IsTrue(range.Value.max > new DateTime(2024, 1, 1), $"max {range.Value.max} should be after 2024");
	}

	[TestMethod]
	[TestCategory("Integration")]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFiles_NonExistentPrefix_ReturnsNull()
	{
		SkipUnlessLive();

		var range = await S3BucketHelper.GetDateRangeFromFilesAsync(
			_client,
			"k-line-history-data",
			"ap-northeast-1",
			"nonexistent/prefix/that/does/not/exist/",
			"yyyy-MM-dd",
			CancellationToken);

		IsFalse(range.HasValue, "range should be null for non-existent prefix");
	}

	#endregion

	#region Cancellation

	[TestMethod]
	[Timeout(5000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFolders_Cancellation_ThrowsOperationCanceled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<TaskCanceledException>(async () =>
		{
			await S3BucketHelper.GetDateRangeFromFoldersAsync(
				_client,
				"gateio-public-data",
				"ap-northeast-1",
				"spot/deals/",
				"yyyyMM",
				cts.Token);
		});
	}

	[TestMethod]
	[Timeout(5000, CooperativeCancellation = true)]
	public async Task GetDateRangeFromFiles_Cancellation_ThrowsOperationCanceled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await ThrowsAsync<TaskCanceledException>(async () =>
		{
			await S3BucketHelper.GetDateRangeFromFilesAsync(
				_client,
				"k-line-history-data",
				"ap-northeast-1",
				"data/spot/daily/trades/BTC-USDT/BTC-USDT-trades-",
				"yyyy-MM-dd",
				cts.Token);
		});
	}

	#endregion
}
