using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using YFSharp.Models;

namespace YFSharp.Tests;

public sealed class YahooFinanceClientTests
{
    [Fact]
    public async Task GetHistoryAsync_ParsesAdjustedCandlesAndEvents()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "chart": {
                "result": [{
                  "meta": {
                    "currency": "USD",
                    "exchangeName": "NMS",
                    "exchangeTimezoneName": "America/New_York",
                    "regularMarketPrice": 191.2
                  },
                  "timestamp": [1704067200, 1704153600],
                  "indicators": {
                    "quote": [{
                      "open": [90, 100],
                      "high": [110, 120],
                      "low": [80, 90],
                      "close": [100, 110],
                      "volume": [1000, 2000]
                    }],
                    "adjclose": [{
                      "adjclose": [50, 55]
                    }]
                  },
                  "events": {
                    "dividends": {
                      "1704153600": { "amount": 0.25, "date": 1704153600 }
                    },
                    "splits": {
                      "1704067200": { "numerator": 4, "denominator": 1, "date": 1704067200 }
                    }
                  }
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var data = await client.GetHistoryAsync("aapl", HistoryRequest.ForPeriod("5d") with { Round = true });

        Assert.Equal("AAPL", data.Symbol);
        Assert.Equal("USD", data.Currency);
        Assert.Equal("America/New_York", data.ExchangeTimezoneName);
        Assert.Equal(2, data.Bars.Count);
        Assert.Equal(45m, data.Bars[0].Open);
        Assert.Equal(50m, data.Bars[0].Close);
        Assert.Equal(4m, data.Bars[0].StockSplit);
        Assert.Equal(0.25m, data.Bars[1].Dividend);
        Assert.Equal("/v8/finance/chart/AAPL", handler.Requests.Single().Uri.AbsolutePath);
        Assert.Contains("range=5d", handler.Requests.Single().Uri.Query);
        Assert.Contains("events=div%2Csplits%2CcapitalGains", handler.Requests.Single().Uri.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_AppliesBackAdjustRoundingAndTimezoneOptions()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "chart": {
                "result": [{
                  "meta": {
                    "currency": "USD",
                    "exchangeName": "NMS",
                    "exchangeTimezoneName": "America/New_York",
                    "regularMarketPrice": 191.2,
                    "priceHint": 3
                  },
                  "timestamp": [1704205800],
                  "indicators": {
                    "quote": [{
                      "open": [90.1234],
                      "high": [110.1234],
                      "low": [80.1234],
                      "close": [100.1234],
                      "volume": [1000]
                    }],
                    "adjclose": [{
                      "adjclose": [50.0617]
                    }]
                  }
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var exchangeTimeData = await client.GetHistoryAsync(
            "aapl",
            HistoryRequest.ForPeriod("5D", "1D") with
            {
                AutoAdjust = false,
                BackAdjust = true,
                Rounding = true
            });
        var utcData = await client.GetHistoryAsync(
            "aapl",
            HistoryRequest.ForPeriod("5D", "1D") with
            {
                AutoAdjust = false,
                BackAdjust = true,
                IgnoreTimezone = true,
                Rounding = true
            });

        var bar = exchangeTimeData.Bars.Single();
        Assert.Equal(TimeSpan.FromHours(-5), bar.Time.Offset);
        Assert.Equal(9, bar.Time.Hour);
        Assert.Equal(30, bar.Time.Minute);
        Assert.Equal(45.062m, bar.Open);
        Assert.Equal(100.123m, bar.Close);
        Assert.Equal(50.062m, bar.AdjustedClose);
        Assert.Equal(TimeSpan.Zero, utcData.Bars.Single().Time.Offset);
        Assert.Contains("interval=1d", handler.Requests.First().Uri.Query);
        Assert.Contains("range=5d", handler.Requests.First().Uri.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_ValidatesHistoryOptionsBeforeRequest()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("No request expected."));
        var options = new YahooFinanceClientOptions
        {
            TimeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 6, 27, 0, 0, 0, TimeSpan.Zero))
        };

        using var client = new YahooFinanceClient(new HttpClient(handler), options);

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetHistoryAsync(
            "AAPL",
            HistoryRequest.ForPeriod("forever")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetHistoryAsync(
            "AAPL",
            HistoryRequest.ForPeriod("1mo", "7m")));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetHistoryAsync(
            "AAPL",
            HistoryRequest.ForPeriod("3mo", "1m")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetHistoryAsync(
            "AAPL",
            new HistoryRequest { End = new DateTimeOffset(2026, 6, 26, 0, 0, 0, TimeSpan.Zero) }));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetHistoryAsync_RepairsCurrencyUnitMixupsWhenRequested()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "chart": {
                "result": [{
                  "meta": { "currency": "USD", "exchangeTimezoneName": "America/New_York" },
                  "timestamp": [1704119400, 1704205800, 1704292200],
                  "indicators": {
                    "quote": [{
                      "open": [99, 9900, 100],
                      "high": [101, 10100, 102],
                      "low": [98, 9800, 99],
                      "close": [100, 10000, 101],
                      "volume": [1000, 1100, 1200]
                    }],
                    "adjclose": [{
                      "adjclose": [100, 10000, 101]
                    }]
                  }
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var raw = await client.GetHistoryAsync(
            "AAPL",
            HistoryRequest.ForPeriod("5d") with { AutoAdjust = false });
        var repaired = await client.GetHistoryAsync(
            "AAPL",
            HistoryRequest.ForPeriod("5d") with { AutoAdjust = false, Repair = true });

        Assert.Equal(10000m, raw.Bars[1].Close);
        Assert.False(raw.Bars[1].Repaired);
        Assert.Equal(100m, repaired.Bars[1].Close);
        Assert.Equal(99m, repaired.Bars[1].Open);
        Assert.Equal(100m, repaired.Bars[1].AdjustedClose);
        Assert.True(repaired.Bars[1].Repaired);
    }

    [Fact]
    public async Task GetHistoryAsync_RepairsDividendAndSplitAnomaliesWhenRequested()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.AbsolutePath.Contains("/DIV", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "chart": {
                        "result": [{
                          "meta": { "currency": "USD", "exchangeTimezoneName": "America/New_York" },
                          "timestamp": [1704119400, 1704205800, 1704292200],
                          "indicators": {
                            "quote": [{
                              "open": [10, 9.5, 9.6],
                              "high": [10, 9.5, 9.6],
                              "low": [10, 9.5, 9.6],
                              "close": [10, 9.5, 9.6],
                              "volume": [1000, 1100, 1200]
                            }],
                            "adjclose": [{
                              "adjclose": [10, 9.5, 9.6]
                            }]
                          },
                          "events": {
                            "dividends": {
                              "1704205800": { "amount": 50, "date": 1704205800 },
                              "1704292200": { "amount": 0.5, "date": 1704292200 }
                            }
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            return JsonResponse("""
                {
                  "chart": {
                    "result": [{
                      "meta": { "currency": "USD", "exchangeTimezoneName": "America/New_York" },
                      "timestamp": [1704119400, 1704205800],
                      "indicators": {
                        "quote": [{
                          "open": [400, 100],
                          "high": [404, 101],
                          "low": [396, 99],
                          "close": [400, 100],
                          "volume": [1000, 4000]
                        }],
                        "adjclose": [{
                          "adjclose": [400, 100]
                        }]
                      },
                      "events": {
                        "splits": {
                          "1704205800": { "numerator": 4, "denominator": 1, "date": 1704205800 }
                        }
                      }
                    }],
                    "error": null
                  }
                }
                """);
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var dividends = await client.GetHistoryAsync(
            "DIV",
            HistoryRequest.ForPeriod("5d") with
            {
                AutoAdjust = false,
                Repair = true
            });
        var splits = await client.GetHistoryAsync(
            "SPLT",
            HistoryRequest.ForPeriod("5d") with
            {
                AutoAdjust = false,
                Repair = true
            });

        Assert.Equal(9.5m, dividends.Bars[0].AdjustedClose);
        Assert.True(dividends.Bars[0].Repaired);
        Assert.Equal(0.5m, dividends.Bars[1].Dividend);
        Assert.Null(dividends.Bars[2].Dividend);
        Assert.True(dividends.Bars[2].Repaired);

        Assert.Equal(100m, splits.Bars[0].Close);
        Assert.Equal(101m, splits.Bars[0].High);
        Assert.Equal(4m, splits.Bars[1].StockSplit);
        Assert.True(splits.Bars[0].Repaired);
    }

    [Fact]
    public void HistoricalData_ToCsv_ExportsRowsAndGroupedMultiTickerColumns()
    {
        var time = new DateTimeOffset(2024, 1, 2, 9, 30, 0, TimeSpan.FromHours(-5));
        var aapl = new HistoricalData
        {
            Symbol = "AAPL",
            Bars =
            [
                new PriceBar
                {
                    Time = time,
                    Open = 1.1m,
                    High = 1.2m,
                    Low = 1.0m,
                    Close = 1.15m,
                    AdjustedClose = 1.14m,
                    Volume = 100,
                    Dividend = 0.01m,
                    StockSplit = 4m,
                    Repaired = true
                }
            ]
        };
        var msft = new HistoricalData
        {
            Symbol = "MSFT",
            Bars =
            [
                new PriceBar
                {
                    Time = time,
                    Open = 2.1m,
                    High = 2.2m,
                    Low = 2.0m,
                    Close = 2.15m,
                    AdjustedClose = 2.14m,
                    Volume = 200
                }
            ]
        };

        var single = aapl.ToCsv();
        var tickerGrouped = HistoricalData.ToCsv([msft, aapl], HistoryGroupBy.Ticker);
        var columnGrouped = HistoricalData.ToCsv([aapl, msft], HistoryGroupBy.Column);

        Assert.StartsWith("Symbol,Time,Open,High,Low,Close,AdjustedClose,Volume,Dividend,StockSplit,CapitalGain,Repaired", single);
        Assert.Contains("AAPL,", single);
        Assert.Contains(",true", single);
        Assert.Contains("Time,AAPL.Open,AAPL.High,AAPL.Low,AAPL.Close", tickerGrouped);
        Assert.Contains("MSFT.Open", tickerGrouped);
        Assert.Contains("Time,Open.AAPL,Open.MSFT,High.AAPL,High.MSFT", columnGrouped);
    }

    [Fact]
    public async Task GetOptionsAsync_ParsesExpirationsAndContracts()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "optionChain": {
                "result": [{
                  "expirationDates": [1709856000],
                  "quote": { "symbol": "AAPL", "regularMarketPrice": 190.5 },
                  "options": [{
                    "calls": [{
                      "contractSymbol": "AAPL240308C00190000",
                      "lastTradeDate": 1709251200,
                      "strike": 190,
                      "lastPrice": 4.2,
                      "bid": 4.1,
                      "ask": 4.3,
                      "volume": 123,
                      "openInterest": 456,
                      "impliedVolatility": 0.21,
                      "inTheMoney": true,
                      "contractSize": "REGULAR",
                      "currency": "USD"
                    }],
                    "puts": []
                  }]
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var chain = await client.GetOptionsAsync("AAPL");

        Assert.Equal("AAPL", chain.Symbol);
        Assert.Equal(new DateOnly(2024, 3, 8), chain.ExpirationDates.Single());
        Assert.Equal("AAPL240308C00190000", chain.Calls.Single().ContractSymbol);
        Assert.Equal(190m, chain.Calls.Single().Strike);
        Assert.True(chain.Calls.Single().InTheMoney);
        Assert.Equal("/v7/finance/options/AAPL", handler.Requests.Single().Uri.AbsolutePath);
    }

    [Fact]
    public async Task SearchAsync_ParsesQuotesNewsAndUsesOptions()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "quotes": [
                {
                  "symbol": "MSFT",
                  "shortname": "Microsoft Corporation",
                  "quoteType": "EQUITY",
                  "score": 12345,
                  "exchDisp": "NASDAQ",
                  "isYahooFinance": true
                },
                { "shortname": "No symbol" }
              ],
              "news": [
                {
                  "uuid": "news-1",
                  "title": "Market note",
                  "publisher": "Example",
                  "providerPublishTime": 1704067200,
                  "type": "STORY",
                  "thumbnail": {
                    "resolutions": [
                      { "url": "https://example.test/thumb.jpg", "width": 128, "height": 72, "tag": "original" }
                    ]
                  },
                  "relatedTickers": ["MSFT", "AAPL"]
                }
              ],
              "lists": [
                { "slug": "tech-stocks", "name": "Technology Stocks", "symbolCount": 42, "dailyPercentGain": 1.25 }
              ],
              "researchReports": [
                {
                  "id": "report-1",
                  "reportId": "R1",
                  "title": "Microsoft Initiation",
                  "provider": "Example Research",
                  "symbol": "MSFT",
                  "targetPrice": 500,
                  "providerPublishTime": 1704067200,
                  "link": "https://example.test/report"
                }
              ],
              "nav": [
                { "navTitle": "Microsoft profile", "navUrl": "https://finance.yahoo.com/quote/MSFT", "type": "quote" }
              ],
              "recommendedSymbols": [
                { "symbol": "MSFT", "shortname": "Microsoft Corporation", "quoteType": "EQUITY" }
              ],
              "culturalAssets": [{ "id": "asset-1" }]
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var result = await client.SearchAsync("microsoft", new SearchOptions { QuotesCount = 3, NewsCount = 1 });

        Assert.Equal("MSFT", result.Quotes.Single().Symbol);
        Assert.Equal(12345m, result.Quotes.Single().Score);
        Assert.True(result.Quotes.Single().IsYahooFinance);
        Assert.Equal("Microsoft Corporation", result.Quotes.Single().Raw.GetProperty("shortname").GetString());
        Assert.Equal("Market note", result.News.Single().Title);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), result.News.Single().ProviderPublishedAt);
        Assert.Equal("https://example.test/thumb.jpg", result.News.Single().Thumbnail!.Resolutions.Single().Url);
        Assert.Equal(new[] { "MSFT", "AAPL" }, result.News.Single().RelatedTickers);
        Assert.Equal("Technology Stocks", result.ListResults.Single().Name);
        Assert.Equal(1.25m, result.ListResults.Single().DailyPercentGain);
        Assert.Equal("Technology Stocks", result.Lists.Single().GetProperty("name").GetString());
        Assert.Equal("Example Research", result.ResearchReports.Single().Provider);
        Assert.Equal(500m, result.ResearchReports.Single().TargetPrice);
        Assert.Equal("report-1", result.Research.Single().GetProperty("id").GetString());
        Assert.Equal("Microsoft profile", result.NavigationLinks.Single().DisplayTitle);
        Assert.Equal("https://finance.yahoo.com/quote/MSFT", result.NavigationLinks.Single().DisplayUrl);
        Assert.Equal("Microsoft profile", result.Navigation.Single().GetProperty("navTitle").GetString());
        Assert.Equal("MSFT", result.RecommendedSymbols.Single().Symbol);
        Assert.Equal("asset-1", result.CulturalAssets.Single().GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Object, result.Raw.ValueKind);
        Assert.Equal("/v1/finance/search", handler.Requests.Single().Uri.AbsolutePath);
        Assert.Contains("quotesCount=3", handler.Requests.Single().Uri.Query);
        Assert.Contains("newsCount=1", handler.Requests.Single().Uri.Query);
    }

    [Fact]
    public async Task LookupConvenienceMethods_UseExpectedLookupTypesAndPreserveRaw()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "finance": {
                "result": [{
                  "documents": [{
                    "symbol": "MSFT",
                    "shortName": "Microsoft Corporation",
                    "quoteType": "EQUITY",
                    "exchange": "NMS",
                    "regularMarketPrice": 400
                  }]
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var stocks = await client.GetStocksAsync("microsoft", count: 2);
        var etfs = await client.GetEtfsAsync("microsoft", count: 2);
        var mutualFunds = await client.GetMutualFundsAsync("microsoft", count: 2);
        var indexes = await client.GetIndexesAsync("microsoft", count: 2);
        var futures = await client.GetFuturesAsync("microsoft", count: 2);
        var currencies = await client.GetCurrenciesAsync("microsoft", count: 2);
        var cryptocurrencies = await client.GetCryptocurrenciesAsync("microsoft", count: 2);

        Assert.Equal(LookupType.Equity, stocks.Type);
        Assert.Equal("MSFT", stocks.Documents.Single().Symbol);
        Assert.Equal("Microsoft Corporation", stocks.Documents.Single().Name);
        Assert.Equal(400m, stocks.Documents.Single().Raw.GetProperty("regularMarketPrice").GetDecimal());
        Assert.Equal(JsonValueKind.Object, stocks.Raw.GetProperty("finance").ValueKind);
        Assert.Equal(LookupType.Etf, etfs.Type);
        Assert.Equal(LookupType.MutualFund, mutualFunds.Type);
        Assert.Equal(LookupType.Index, indexes.Type);
        Assert.Equal(LookupType.Future, futures.Type);
        Assert.Equal(LookupType.Currency, currencies.Type);
        Assert.Equal(LookupType.Cryptocurrency, cryptocurrencies.Type);

        Assert.Collection(
            handler.Requests,
            request => Assert.Contains("type=equity", request.Uri.Query),
            request => Assert.Contains("type=etf", request.Uri.Query),
            request => Assert.Contains("type=mutualfund", request.Uri.Query),
            request => Assert.Contains("type=index", request.Uri.Query),
            request => Assert.Contains("type=future", request.Uri.Query),
            request => Assert.Contains("type=currency", request.Uri.Query),
            request => Assert.Contains("type=cryptocurrency", request.Uri.Query));
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("/v1/finance/lookup", request.Uri.AbsolutePath);
            Assert.Contains("count=2", request.Uri.Query);
        });
    }

    [Fact]
    public async Task ScreenAsync_SerializesCustomQueryInYahooShape()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "finance": {
                "result": [{
                  "count": 1,
                  "total": 1,
                  "quotes": [{ "symbol": "MSFT" }]
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));
        var request = new ScreenerRequest
        {
            Count = 10,
            SortField = "percentchange",
            Query = new EquityQuery("and",
            [
                new EquityQuery("gt", ["percentchange", 3]),
                new EquityQuery("eq", ["region", "us"])
            ])
        };

        var result = await client.ScreenAsync(request);

        Assert.Equal(1, result.Count);
        Assert.Equal("MSFT", result.Quotes.Single().GetProperty("symbol").GetString());
        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("/v1/finance/screener", handler.Requests.Single().Uri.AbsolutePath);
        Assert.Contains("\"operator\":\"AND\"", handler.Requests.Single().Body);
        Assert.Contains("\"operator\":\"GT\"", handler.Requests.Single().Body);
        Assert.Contains("\"quoteType\":\"EQUITY\"", handler.Requests.Single().Body);
    }

    [Fact]
    public void ScreenerQuery_ValidatesFieldsOperatorsAndRestrictedValues()
    {
        var validEquity = new EquityQuery("and",
        [
            new EquityQuery("gt", ["percentchange", 3]),
            new EquityQuery("is-in", ["exchange", "NMS", "NYQ"]),
            new EquityQuery("eq", ["region", "us"])
        ]);
        var validFund = new FundQuery("eq", ["categoryname", "Large Growth"]);
        var validEtf = new EtfQuery("is-in", ["performanceratingoverall", 4, 5]);

        Assert.Equal("AND", validEquity.Operator);
        Assert.Equal("EQ", validFund.Operator);
        Assert.Equal("IS-IN", validEtf.Operator);
        Assert.Contains("percentchange", ScreenerMetadata.EquityFields["price"]);
        Assert.Equal(ScreenerQuoteType.Etf, PredefinedScreeners.Metadata[PredefinedScreeners.TopEtfsUs].QuoteType);

        Assert.Throws<ArgumentException>(() => new EquityQuery("eq", ["not_a_field", "us"]));
        Assert.Throws<ArgumentException>(() => new EquityQuery("eq", ["region", "moon"]));
        Assert.Throws<ArgumentException>(() => new EquityQuery("gt", ["percentchange", "3"]));
        Assert.Throws<ArgumentException>(() => new EquityQuery("and", [new EquityQuery("eq", ["region", "us"])]));
        Assert.Throws<ArgumentException>(() => new EquityQuery("and",
        [
            new EquityQuery("eq", ["region", "us"]),
            new FundQuery("eq", ["categoryname", "Large Growth"])
        ]));
        Assert.Throws<ArgumentException>(() => new FundQuery("is-in", ["performanceratingoverall", 4, 6]));
    }

    [Fact]
    public async Task ScreenAsync_InfersQuoteTypeFromTypedQuery()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "finance": {
                "result": [{
                  "count": 0,
                  "total": 0,
                  "quotes": []
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));
        var request = new ScreenerRequest
        {
            Query = new FundQuery("and",
            [
                new FundQuery("eq", ["categoryname", "Large Growth"]),
                new FundQuery("is-in", ["performanceratingoverall", 4, 5])
            ])
        };

        await client.ScreenAsync(request);

        Assert.Contains("\"quoteType\":\"MUTUALFUND\"", handler.Requests.Single().Body);
    }

    [Fact]
    public async Task CalendarAsync_PostsVisualizationQueryAndParsesEarningsRows()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "finance": {
                "result": [{
                  "documents": [{
                    "columns": [
                      { "label": "Symbol", "field": "ticker", "type": "STRING" },
                      { "label": "Company Name", "field": "companyshortname", "type": "STRING" },
                      { "label": "Market Cap (Intraday)", "field": "intradaymarketcap", "type": "NUMBER" },
                      { "label": "Event Name", "field": "eventname", "type": "STRING" },
                      { "label": "Event Start Date", "field": "startdatetime", "type": "DATETIME" },
                      { "label": "Event Start Date", "field": "startdatetimetype", "type": "STRING" },
                      { "label": "EPS Estimate", "field": "epsestimate", "type": "NUMBER" },
                      { "label": "Reported EPS", "field": "epsactual", "type": "NUMBER" },
                      { "label": "Surprise (%)", "field": "epssurprisepct", "type": "NUMBER" }
                    ],
                    "rows": [[
                      "AAPL",
                      "Apple Inc.",
                      2830000000000,
                      "Q3 2026 Earnings",
                      "2026-07-30T20:00:00Z",
                      "AMC",
                      1.55,
                      1.62,
                      4.5
                    ]]
                  }]
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var result = await client.GetEarningsCalendarAsync(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            filterMostActive: false,
            limit: 5);

        var row = result.Rows.Single();
        Assert.Equal(CalendarType.Earnings, result.Type);
        Assert.Equal("AAPL", row.Symbol);
        Assert.Equal("Apple Inc.", row.CompanyName);
        Assert.Equal(2830000000000m, row.MarketCap);
        Assert.Equal("Q3 2026 Earnings", row.EventName);
        Assert.Equal(new DateTimeOffset(2026, 7, 30, 20, 0, 0, TimeSpan.Zero), row.EventStartDate);
        Assert.Equal("AMC", row.Timing);
        Assert.Equal(1.55m, row.EpsEstimate);
        Assert.Equal(1.62m, row.ReportedEps);
        Assert.Equal(4.5m, row.SurprisePercent);
        Assert.Equal("Timing", result.Columns[5].Key);
        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("/v1/finance/visualization", handler.Requests.Single().Uri.AbsolutePath);
        Assert.Contains("\"entityIdType\":\"sp_earnings\"", handler.Requests.Single().Body);
        Assert.Contains("\"size\":5", handler.Requests.Single().Body);
        Assert.Contains("\"startdatetime\",\"2026-07-01\"", handler.Requests.Single().Body);
        Assert.Contains("\"startdatetime\",\"2026-07-31\"", handler.Requests.Single().Body);
    }

    [Fact]
    public async Task CalendarAsync_ParsesIpoEconomicAndSplitRows()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Body.Contains("\"entityIdType\":\"ipo_info\"", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "finance": {
                        "result": [{
                          "documents": [{
                            "columns": [
                              { "label": "Symbol", "field": "ticker" },
                              { "label": "Company Name", "field": "companyshortname" },
                              { "label": "Exchange Short Name", "field": "exchange_short_name" },
                              { "label": "Filing Date", "field": "filingdate" },
                              { "label": "Date", "field": "startdatetime" },
                              { "label": "Amended Date", "field": "amendeddate" },
                              { "label": "Price From", "field": "pricefrom" },
                              { "label": "Price To", "field": "priceto" },
                              { "label": "Price", "field": "offerprice" },
                              { "label": "Currency", "field": "currencyname" },
                              { "label": "Shares", "field": "shares" },
                              { "label": "Deal Type", "field": "dealtype" }
                            ],
                            "rows": [["ACME", "Acme Corp", "NYSE", "2026-07-01", "2026-07-15", "2026-07-08", 10, 12, 11, "USD", 10000000, "IPO"]]
                          }]
                        }]
                      }
                    }
                    """);
            }

            if (request.Body.Contains("\"entityIdType\":\"economic_event\"", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "finance": {
                        "result": [{
                          "documents": [{
                            "columns": [
                              { "label": "Event", "field": "econ_release" },
                              { "label": "Country Code", "field": "country_code" },
                              { "label": "Event Time", "field": "startdatetime" },
                              { "label": "Period", "field": "period" },
                              { "label": "Actual", "field": "after_release_actual" },
                              { "label": "Market Expectation", "field": "consensus_estimate" },
                              { "label": "Prior to This", "field": "prior_release_actual" },
                              { "label": "Revised from", "field": "originally_reported_actual" }
                            ],
                            "rows": [["Jobs Report", "US", "2026-07-03T12:30:00Z", "Jun", 200000, 190000, 180000, 175000]]
                          }]
                        }]
                      }
                    }
                    """);
            }

            return JsonResponse("""
                {
                  "finance": {
                    "result": [{
                      "documents": [{
                        "columns": [
                          { "label": "Symbol", "field": "ticker" },
                          { "label": "Company Name", "field": "companyshortname" },
                          { "label": "Payable On", "field": "startdatetime" },
                          { "label": "Optionable?", "field": "optionable" },
                          { "label": "Old Share Worth", "field": "old_share_worth" },
                          { "label": "Share Worth", "field": "share_worth" }
                        ],
                        "rows": [["XYZ", "XYZ Inc.", "2026-07-10", true, 1, 4]]
                      }]
                    }]
                  }
                }
                """);
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var ipo = await client.GetIpoCalendarAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var economic = await client.GetEconomicEventsCalendarAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var splits = await client.GetSplitsCalendarAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        Assert.Equal("ACME", ipo.Rows.Single().Symbol);
        Assert.Equal("NYSE", ipo.Rows.Single().Exchange);
        Assert.Equal(11m, ipo.Rows.Single().OfferPrice);
        Assert.Equal(10000000m, ipo.Rows.Single().Shares);
        Assert.Equal("Jobs Report", economic.Rows.Single().Event);
        Assert.Equal("US", economic.Rows.Single().Region);
        Assert.Equal(190000m, economic.Rows.Single().Expected);
        Assert.Equal("XYZ", splits.Rows.Single().Symbol);
        Assert.True(splits.Rows.Single().Optionable);
        Assert.Equal(4m, splits.Rows.Single().ShareWorth);
        Assert.Contains("\"entityIdType\":\"ipo_info\"", handler.Requests[0].Body);
        Assert.Contains("\"entityIdType\":\"economic_event\"", handler.Requests[1].Body);
        Assert.Contains("\"entityIdType\":\"splits\"", handler.Requests[2].Body);
    }

    [Fact]
    public async Task CalendarsFacade_UsesConfiguredDateRange()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "finance": {
                "result": [{
                  "documents": [{
                    "columns": [
                      { "label": "Event", "field": "econ_release" },
                      { "label": "Country Code", "field": "country_code" },
                      { "label": "Event Time", "field": "startdatetime" }
                    ],
                    "rows": [["Fed Decision", "US", "2026-07-29T18:00:00Z"]]
                  }]
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var result = await client.Calendars(new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 31))
            .GetEconomicEventsCalendarAsync(limit: 3);

        Assert.Equal("Fed Decision", result.Rows.Single().Event);
        Assert.Contains("\"size\":3", handler.Requests.Single().Body);
        Assert.Contains("\"startdatetime\",\"2026-07-20\"", handler.Requests.Single().Body);
        Assert.Contains("\"startdatetime\",\"2026-07-31\"", handler.Requests.Single().Body);
    }

    [Fact]
    public async Task GetQuotesAsync_ParsesQuoteResponse()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "quoteResponse": {
                "result": [{
                  "symbol": "AAPL",
                  "shortName": "Apple Inc.",
                  "currency": "USD",
                  "regularMarketPrice": 199.5,
                  "regularMarketVolume": 100
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var quote = (await client.GetQuotesAsync(["aapl"])).Single();

        Assert.Equal("AAPL", quote.Symbol);
        Assert.Equal("Apple Inc.", quote.ShortName);
        Assert.Equal(199.5m, quote.RegularMarketPrice);
        Assert.Contains("symbols=AAPL", handler.Requests.Single().Uri.Query);
    }

    [Fact]
    public async Task GetQuoteAsync_DefaultsToChromeRequestProfileHeaders()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "quoteResponse": {
                "result": [{
                  "symbol": "AAPL",
                  "regularMarketPrice": 199.5
                }]
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        await client.GetQuoteAsync("AAPL");

        var request = handler.Requests.Single();
        Assert.Contains("Chrome/133.0.0.0", request.Headers["User-Agent"]);
        Assert.Equal("en-US, en; q=0.9", request.Headers["Accept-Language"]);
        Assert.Equal("https://finance.yahoo.com", request.Headers["Origin"]);
        Assert.Equal("https://finance.yahoo.com/", request.Headers["Referer"]);
        Assert.Equal("same-site", request.Headers["Sec-Fetch-Site"]);
        Assert.Equal("cors", request.Headers["Sec-Fetch-Mode"]);
        Assert.Equal("empty", request.Headers["Sec-Fetch-Dest"]);
        Assert.Contains("application/json", request.Headers["Accept"]);
    }

    [Fact]
    public async Task GetQuoteAsync_AllowsDefaultRequestProfileAndCustomUserAgent()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "quoteResponse": {
                "result": [{
                  "symbol": "AAPL",
                  "regularMarketPrice": 199.5
                }]
              }
            }
            """));
        var options = new YahooFinanceClientOptions
        {
            RequestProfile = YahooFinanceRequestProfile.Default,
            UserAgent = "YFSharp.Tests/1.0"
        };

        using var client = new YahooFinanceClient(new HttpClient(handler), options);

        await client.GetQuoteAsync("AAPL");

        var request = handler.Requests.Single();
        Assert.Equal("YFSharp.Tests/1.0", request.Headers["User-Agent"]);
        Assert.False(request.Headers.ContainsKey("Sec-Fetch-Site"));
        Assert.False(request.Headers.ContainsKey("Origin"));
        Assert.Contains("application/json", request.Headers["Accept"]);
    }

    [Fact]
    public async Task GetQuoteAsync_RetriesQuoteEndpointWithCookieAndCrumbWhenUnauthorized()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.Host == "fc.yahoo.com")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty)
                };
                response.Headers.Add("Set-Cookie", "A3=session-cookie; Domain=.yahoo.com; Path=/");
                return response;
            }

            if (request.Uri.AbsolutePath == "/v1/test/getcrumb")
            {
                Assert.Contains("A3=session-cookie", request.CookieHeader);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-token", Encoding.UTF8, "text/plain")
                };
            }

            if (request.Uri.AbsolutePath == "/v7/finance/quote"
                && !request.Uri.Query.Contains("crumb=crumb-token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("""{"finance":{"error":{"code":"Unauthorized"}}}""")
                };
            }

            return JsonResponse("""
                {
                  "quoteResponse": {
                    "result": [{
                      "symbol": "AAPL",
                      "shortName": "Apple Inc.",
                      "currency": "USD",
                      "regularMarketPrice": 283.78
                    }]
                  }
                }
                """);
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var quote = await client.GetQuoteAsync("aapl");

        Assert.NotNull(quote);
        Assert.Equal("AAPL", quote.Symbol);
        Assert.Equal(283.78m, quote.RegularMarketPrice);
        Assert.Equal("USD", quote.Currency);
        Assert.Collection(
            handler.Requests,
            request => Assert.Equal("/v7/finance/quote", request.Uri.AbsolutePath),
            request => Assert.Equal("fc.yahoo.com", request.Uri.Host),
            request => Assert.Equal("/v1/test/getcrumb", request.Uri.AbsolutePath),
            request =>
            {
                Assert.Equal("/v7/finance/quote", request.Uri.AbsolutePath);
                Assert.Contains("crumb=crumb-token", request.Uri.Query);
                Assert.Contains("A3=session-cookie", request.CookieHeader);
            });
    }

    [Fact]
    public async Task GetQuoteAsync_ThrowsRateLimitWithoutRetriesByDefault()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("Edge: Too Many Requests", Encoding.UTF8, "text/plain")
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        await Assert.ThrowsAsync<YahooFinanceRateLimitException>(() => client.GetQuoteAsync("AAPL"));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetQuoteAsync_RetriesRateLimitsWithConfiguredBackoff()
    {
        var retryAttempts = new List<int>();
        var responseCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            responseCount++;
            if (responseCount <= 2)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("Edge: Too Many Requests", Encoding.UTF8, "text/plain")
                };
            }

            return JsonResponse("""
                {
                  "quoteResponse": {
                    "result": [{
                      "symbol": "AAPL",
                      "regularMarketPrice": 199.5
                    }]
                  }
                }
                """);
        });

        var options = new YahooFinanceClientOptions
        {
            MaxRetries = 2,
            RateLimitBackoff = retryAttempt =>
            {
                retryAttempts.Add(retryAttempt);
                return TimeSpan.Zero;
            }
        };

        using var client = new YahooFinanceClient(new HttpClient(handler), options);

        var quote = await client.GetQuoteAsync("AAPL");

        Assert.NotNull(quote);
        Assert.Equal(199.5m, quote.RegularMarketPrice);
        Assert.Equal([1, 2], retryAttempts);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task GetQuoteAsync_PersistsAndReusesCookieAndCrumb()
    {
        var store = new InMemoryYahooFinanceAuthStore();
        var crumbRequests = 0;
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.Host == "fc.yahoo.com")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty)
                };
                response.Headers.Add("Set-Cookie", "A3=session-cookie; Domain=.yahoo.com; Path=/");
                return response;
            }

            if (request.Uri.AbsolutePath == "/v1/test/getcrumb")
            {
                crumbRequests++;
                Assert.Contains("A3=session-cookie", request.CookieHeader);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-token", Encoding.UTF8, "text/plain")
                };
            }

            if (request.Uri.AbsolutePath == "/v7/finance/quote"
                && !request.Uri.Query.Contains("crumb=crumb-token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("""{"finance":{"error":{"code":"Unauthorized"}}}""")
                };
            }

            Assert.Contains("A3=session-cookie", request.CookieHeader);
            return JsonResponse("""
                {
                  "quoteResponse": {
                    "result": [{
                      "symbol": "AAPL",
                      "regularMarketPrice": 283.78
                    }]
                  }
                }
                """);
        });

        var options = new YahooFinanceClientOptions { AuthStore = store };

        using (var firstClient = new YahooFinanceClient(new HttpClient(handler), options))
        {
            var quote = await firstClient.GetQuoteAsync("AAPL");
            Assert.NotNull(quote);
        }

        var persisted = await store.LoadAsync();
        Assert.NotNull(persisted);
        Assert.Equal("crumb-token", persisted.Crumb);
        Assert.Equal(YahooFinanceCookieStrategy.Basic, persisted.Strategy);
        Assert.Equal("session-cookie", persisted.Cookies["A3"]);

        using (var secondClient = new YahooFinanceClient(new HttpClient(handler), options))
        {
            var quote = await secondClient.GetQuoteAsync("AAPL");
            Assert.NotNull(quote);
            Assert.Equal(283.78m, quote.RegularMarketPrice);
        }

        Assert.Equal(1, crumbRequests);
        Assert.DoesNotContain(handler.Requests.Skip(4), request => request.Uri.Host == "fc.yahoo.com");
        Assert.DoesNotContain(handler.Requests.Skip(4), request => request.Uri.AbsolutePath == "/v1/test/getcrumb");
    }

    [Fact]
    public async Task GetQuoteAsync_IgnoresExpiredAuthStoreState()
    {
        var store = new InMemoryYahooFinanceAuthStore(new YahooFinanceAuthState
        {
            Cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["A3"] = "expired-cookie"
            },
            Crumb = "expired-crumb",
            Strategy = YahooFinanceCookieStrategy.Basic,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
        });

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.AbsolutePath == "/v7/finance/quote"
                && !request.Uri.Query.Contains("crumb=new-crumb", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("expired-cookie", request.CookieHeader);
                Assert.DoesNotContain("expired-crumb", request.Uri.Query);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("""{"finance":{"error":{"code":"Unauthorized"}}}""")
                };
            }

            if (request.Uri.Host == "fc.yahoo.com")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty)
                };
                response.Headers.Add("Set-Cookie", "A3=new-cookie; Domain=.yahoo.com; Path=/");
                return response;
            }

            if (request.Uri.AbsolutePath == "/v1/test/getcrumb")
            {
                Assert.Contains("A3=new-cookie", request.CookieHeader);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("new-crumb", Encoding.UTF8, "text/plain")
                };
            }

            Assert.Contains("A3=new-cookie", request.CookieHeader);
            return JsonResponse("""
                {
                  "quoteResponse": {
                    "result": [{
                      "symbol": "AAPL",
                      "regularMarketPrice": 283.78
                    }]
                  }
                }
                """);
        });

        var options = new YahooFinanceClientOptions
        {
            AuthStore = store,
            AuthStateTtl = TimeSpan.FromHours(1)
        };

        using var client = new YahooFinanceClient(new HttpClient(handler), options);

        var quote = await client.GetQuoteAsync("AAPL");
        var persisted = await store.LoadAsync();

        Assert.NotNull(quote);
        Assert.NotNull(persisted);
        Assert.Equal("new-crumb", persisted.Crumb);
        Assert.Equal("new-cookie", persisted.Cookies["A3"]);
    }

    [Fact]
    public async Task GetQuoteSummaryAsync_ExposesTypedQuoteSummaryModules()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
            {
              "quoteSummary": {
                "result": [{
                  "assetProfile": {
                    "address1": "One Apple Park Way",
                    "city": "Cupertino",
                    "state": "CA",
                    "country": "United States",
                    "website": "https://www.apple.com",
                    "industry": "Consumer Electronics",
                    "sector": "Technology",
                    "fullTimeEmployees": { "raw": 164000, "fmt": "164k" },
                    "companyOfficers": [{
                      "name": "Jane Appleseed",
                      "title": "Chief Test Officer",
                      "totalPay": { "raw": 1234567, "fmt": "1.23M" }
                    }]
                  },
                  "summaryDetail": {
                    "previousClose": { "raw": 189.75, "fmt": "189.75" },
                    "open": { "raw": 190.10, "fmt": "190.10" },
                    "regularMarketVolume": { "raw": 50123000, "fmt": "50.12M" },
                    "fiftyTwoWeekHigh": { "raw": 237.49, "fmt": "237.49" },
                    "exDividendDate": { "raw": 1706745600, "fmt": "2024-02-01" },
                    "currency": "USD"
                  },
                  "defaultKeyStatistics": {
                    "forwardPE": { "raw": 31.25, "fmt": "31.25" },
                    "sharesOutstanding": { "raw": 15123456789, "fmt": "15.12B" },
                    "52WeekChange": { "raw": 0.12, "fmt": "12.00%" },
                    "lastSplitDate": { "raw": 1598832000, "fmt": "2020-08-31" }
                  },
                  "financialData": {
                    "currentPrice": { "raw": 190.50, "fmt": "190.50" },
                    "targetMeanPrice": { "raw": 210.25, "fmt": "210.25" },
                    "recommendationMean": { "raw": 1.8, "fmt": "1.80" },
                    "recommendationKey": "buy",
                    "numberOfAnalystOpinions": { "raw": 38, "fmt": "38" },
                    "financialCurrency": "USD"
                  },
                  "calendarEvents": {
                    "earnings": {
                      "earningsDate": [{ "raw": 1714608000, "fmt": "2024-05-02" }],
                      "earningsAverage": { "raw": 1.55, "fmt": "1.55" },
                      "revenueAverage": { "raw": 90000000000, "fmt": "90B" }
                    },
                    "dividendDate": { "raw": 1709164800, "fmt": "2024-02-29" }
                  },
                  "secFilings": {
                    "filings": [{
                      "epochDate": { "raw": 1711929600, "fmt": "2024-04-01" },
                      "date": "2024-04-01",
                      "type": "10-Q",
                      "title": "Quarterly report",
                      "edgarUrl": "https://www.sec.gov/example"
                    }]
                  },
                  "recommendationTrend": {
                    "trend": [{
                      "period": "0m",
                      "strongBuy": 12,
                      "buy": 20,
                      "hold": 8,
                      "sell": 1,
                      "strongSell": 0
                    }]
                  },
                  "upgradeDowngradeHistory": {
                    "history": [{
                      "epochGradeDate": 1717200000,
                      "firm": "Example Securities",
                      "toGrade": "Buy",
                      "fromGrade": "Neutral",
                      "action": "up"
                    }]
                  },
                  "esgScores": {
                    "totalEsg": { "raw": 18.4, "fmt": "18.4" },
                    "environmentScore": { "raw": 1.2, "fmt": "1.2" },
                    "socialScore": { "raw": 7.3, "fmt": "7.3" },
                    "governanceScore": { "raw": 9.9, "fmt": "9.9" },
                    "ratingYear": 2024,
                    "ratingMonth": 9,
                    "peerGroup": "Technology Hardware",
                    "relatedControversy": ["Customer Incidents"]
                  }
                }],
                "error": null
              }
            }
            """));

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var summary = await client.GetQuoteSummaryAsync(
            "aapl",
            [
                QuoteSummaryModules.AssetProfile,
                QuoteSummaryModules.SummaryDetail,
                QuoteSummaryModules.DefaultKeyStatistics,
                QuoteSummaryModules.FinancialData,
                QuoteSummaryModules.CalendarEvents,
                QuoteSummaryModules.SecFilings,
                QuoteSummaryModules.RecommendationTrend,
                QuoteSummaryModules.UpgradeDowngradeHistory,
                QuoteSummaryModules.EsgScores
            ]);

        Assert.Equal("AAPL", summary.Symbol);
        Assert.Equal("Technology", summary.AssetProfile?.Sector);
        Assert.Equal(164000, summary.AssetProfile?.FullTimeEmployees);
        Assert.Equal("Jane Appleseed", summary.AssetProfile?.CompanyOfficers.Single().Name);
        Assert.Equal(1234567m, summary.AssetProfile?.CompanyOfficers.Single().TotalPay);
        Assert.Equal(189.75m, summary.SummaryDetail?.PreviousClose);
        Assert.Equal(50123000, summary.SummaryDetail?.RegularMarketVolume);
        Assert.Equal(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), summary.SummaryDetail?.ExDividendDate);
        Assert.Equal(31.25m, summary.DefaultKeyStatistics?.ForwardPe);
        Assert.Equal(15123456789, summary.DefaultKeyStatistics?.SharesOutstanding);
        Assert.Equal(0.12m, summary.DefaultKeyStatistics?.FiftyTwoWeekChange);
        Assert.Equal(new DateTimeOffset(2020, 8, 31, 0, 0, 0, TimeSpan.Zero), summary.DefaultKeyStatistics?.LastSplitDate);
        Assert.Equal(190.50m, summary.FinancialData?.CurrentPrice);
        Assert.Equal("buy", summary.FinancialData?.RecommendationKey);
        Assert.Equal(38, summary.FinancialData?.NumberOfAnalystOpinions);
        Assert.Equal(new DateTimeOffset(2024, 5, 2, 0, 0, 0, TimeSpan.Zero), summary.CalendarEvents?.Earnings?.EarningsDates.Single());
        Assert.Equal(90000000000m, summary.CalendarEvents?.Earnings?.RevenueAverage);
        Assert.Equal("10-Q", summary.SecFilings?.Filings.Single().Type);
        Assert.Equal("0m", summary.RecommendationTrend?.Trend.Single().Period);
        Assert.Equal(20, summary.RecommendationTrend?.Trend.Single().Buy);
        Assert.Equal("Example Securities", summary.UpgradeDowngradeHistory?.History.Single().Firm);
        Assert.Equal("up", summary.UpgradeDowngradeHistory?.History.Single().Action);
        Assert.Equal(18.4m, summary.EsgScores?.TotalEsg);
        Assert.Equal("Customer Incidents", summary.EsgScores?.RelatedControversy.Single());
        Assert.Equal("/v10/finance/quoteSummary/AAPL", handler.Requests.Single().Uri.AbsolutePath);
    }

    [Fact]
    public async Task Ticker_TypedQuoteSummaryHelpersRequestExpectedModules()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.AbsolutePath == "/v7/finance/quote")
            {
                return JsonResponse("""
                    {
                      "quoteResponse": {
                        "result": [{
                          "symbol": "AAPL",
                          "shortName": "Apple Inc.",
                          "currency": "USD",
                          "exchange": "NMS",
                          "regularMarketPrice": 191.25,
                          "regularMarketChange": 1.5,
                          "regularMarketPreviousClose": 189.75,
                          "regularMarketVolume": 50123000
                        }]
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=financialData%2CquoteType%2CdefaultKeyStatistics%2CassetProfile%2CsummaryDetail", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "financialData": { "currentPrice": { "raw": 191.25, "fmt": "191.25" } },
                          "quoteType": { "quoteType": "EQUITY", "symbol": "AAPL" },
                          "defaultKeyStatistics": { "forwardPE": { "raw": 30.5, "fmt": "30.5" } },
                          "assetProfile": { "sector": "Technology" },
                          "summaryDetail": { "previousClose": { "raw": 189.75, "fmt": "189.75" } }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=upgradeDowngradeHistory", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "upgradeDowngradeHistory": {
                            "history": [{ "epochGradeDate": 1717200000, "firm": "Example Securities", "toGrade": "Buy", "action": "up" }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=recommendationTrend", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "recommendationTrend": {
                            "trend": [{ "period": "0m", "strongBuy": 4, "buy": 12, "hold": 2 }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=calendarEvents", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "calendarEvents": {
                            "earnings": { "earningsDate": [{ "raw": 1714608000, "fmt": "2024-05-02" }] }
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=secFilings", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "secFilings": {
                            "filings": [{ "date": "2024-04-01", "type": "10-Q", "title": "Quarterly report" }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=esgScores", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "esgScores": {
                            "totalEsg": { "raw": 18.4, "fmt": "18.4" },
                            "peerGroup": "Technology Hardware"
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            throw new InvalidOperationException($"Unexpected request: {request.Uri}");
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));
        var ticker = client.Ticker("aapl");

        var info = await ticker.GetInfoAsync();
        var fastInfo = await ticker.GetFastInfoAsync();
        var recommendations = await ticker.GetRecommendationsAsync();
        var recommendationsSummary = await ticker.GetRecommendationsSummaryAsync();
        var upgradesDowngrades = await ticker.GetUpgradesDowngradesAsync();
        var calendar = await ticker.GetCalendarAsync();
        var filings = await ticker.GetSecFilingsAsync();
        var sustainability = await ticker.GetSustainabilityAsync();

        Assert.Equal("AAPL", info.Symbol);
        Assert.Equal("Technology", info.AssetProfile?.Sector);
        Assert.Equal(191.25m, info.FinancialData?.CurrentPrice);
        Assert.Equal(191.25m, fastInfo?.LastPrice);
        Assert.Equal(50123000, fastInfo?.LastVolume);
        Assert.Equal("Example Securities", recommendations.Single().Firm);
        Assert.Equal("up", upgradesDowngrades.Single().Action);
        Assert.Equal(12, recommendationsSummary?.Trend.Single().Buy);
        Assert.Equal(new DateTimeOffset(2024, 5, 2, 0, 0, 0, TimeSpan.Zero), calendar?.Earnings?.EarningsDates.Single());
        Assert.Equal("10-Q", filings?.Filings.Single().Type);
        Assert.Equal("Technology Hardware", sustainability?.PeerGroup);
    }

    [Fact]
    public async Task Ticker_FinancialAnalysisAndHolderHelpersReturnTypedRows()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.Query.Contains("modules=incomeStatementHistoryQuarterly", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "incomeStatementHistoryQuarterly": {
                            "incomeStatementHistory": [
                              { "endDate": { "raw": 1772323200, "fmt": "2026-03-01" }, "totalRevenue": { "raw": 10 }, "netIncome": { "raw": 1 } },
                              { "endDate": { "raw": 1764547200, "fmt": "2025-12-01" }, "totalRevenue": { "raw": 20 }, "netIncome": { "raw": 2 } },
                              { "endDate": { "raw": 1756684800, "fmt": "2025-09-01" }, "totalRevenue": { "raw": 30 }, "netIncome": { "raw": 3 } },
                              { "endDate": { "raw": 1748736000, "fmt": "2025-06-01" }, "totalRevenue": { "raw": 40 }, "netIncome": { "raw": 4 } }
                            ]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=incomeStatementHistory", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "incomeStatementHistory": {
                            "incomeStatementHistory": [{
                              "endDate": { "raw": 1767139200, "fmt": "2025-12-31" },
                              "totalRevenue": { "raw": 383000000000, "fmt": "383B" },
                              "grossProfit": { "raw": 170000000000, "fmt": "170B" },
                              "netIncome": { "raw": 97000000000, "fmt": "97B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=balanceSheetHistoryQuarterly", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "balanceSheetHistoryQuarterly": {
                            "balanceSheetStatements": [{
                              "endDate": { "raw": 1772323200, "fmt": "2026-03-01" },
                              "totalAssets": { "raw": 360000000000, "fmt": "360B" },
                              "totalLiab": { "raw": 270000000000, "fmt": "270B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=balanceSheetHistory", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "balanceSheetHistory": {
                            "balanceSheetStatements": [{
                              "endDate": { "raw": 1767139200, "fmt": "2025-12-31" },
                              "cash": { "raw": 62000000000, "fmt": "62B" },
                              "totalAssets": { "raw": 352000000000, "fmt": "352B" },
                              "totalStockholderEquity": { "raw": 74000000000, "fmt": "74B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=cashFlowStatementHistoryQuarterly", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "cashFlowStatementHistoryQuarterly": {
                            "cashflowStatements": [
                              { "endDate": { "raw": 1772323200, "fmt": "2026-03-01" }, "totalCashFromOperatingActivities": { "raw": 100 }, "capitalExpenditures": { "raw": -10 } },
                              { "endDate": { "raw": 1764547200, "fmt": "2025-12-01" }, "totalCashFromOperatingActivities": { "raw": 200 }, "capitalExpenditures": { "raw": -20 } },
                              { "endDate": { "raw": 1756684800, "fmt": "2025-09-01" }, "totalCashFromOperatingActivities": { "raw": 300 }, "capitalExpenditures": { "raw": -30 } },
                              { "endDate": { "raw": 1748736000, "fmt": "2025-06-01" }, "totalCashFromOperatingActivities": { "raw": 400 }, "capitalExpenditures": { "raw": -40 } }
                            ]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=cashFlowStatementHistory", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "cashFlowStatementHistory": {
                            "cashflowStatements": [{
                              "endDate": { "raw": 1767139200, "fmt": "2025-12-31" },
                              "netIncome": { "raw": 97000000000, "fmt": "97B" },
                              "totalCashFromOperatingActivities": { "raw": 110000000000, "fmt": "110B" },
                              "capitalExpenditures": { "raw": -12000000000, "fmt": "-12B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=financialData", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "financialData": {
                            "currentPrice": { "raw": 191.25, "fmt": "191.25" },
                            "targetHighPrice": { "raw": 250, "fmt": "250" },
                            "targetLowPrice": { "raw": 160, "fmt": "160" },
                            "targetMeanPrice": { "raw": 210.25, "fmt": "210.25" },
                            "recommendationMean": { "raw": 1.8, "fmt": "1.80" },
                            "numberOfAnalystOpinions": { "raw": 38, "fmt": "38" }
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=earningsTrend", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "earningsTrend": {
                            "trend": [{
                              "period": "0q",
                              "endDate": "2026-09-30",
                              "growth": { "raw": 0.12, "fmt": "12.00%" },
                              "earningsEstimate": {
                                "avg": { "raw": 1.55, "fmt": "1.55" },
                                "low": { "raw": 1.4, "fmt": "1.40" },
                                "high": { "raw": 1.7, "fmt": "1.70" },
                                "yearAgoEps": { "raw": 1.39, "fmt": "1.39" },
                                "numberOfAnalysts": { "raw": 33, "fmt": "33" },
                                "growth": { "raw": 0.115, "fmt": "11.50%" }
                              },
                              "revenueEstimate": {
                                "avg": { "raw": 94000000000, "fmt": "94B" },
                                "low": { "raw": 91000000000, "fmt": "91B" },
                                "high": { "raw": 97000000000, "fmt": "97B" },
                                "yearAgoRevenue": { "raw": 89000000000, "fmt": "89B" },
                                "numberOfAnalysts": { "raw": 31, "fmt": "31" },
                                "growth": { "raw": 0.056, "fmt": "5.60%" }
                              },
                              "epsTrend": {
                                "current": { "raw": 1.55, "fmt": "1.55" },
                                "7daysAgo": { "raw": 1.54, "fmt": "1.54" },
                                "30daysAgo": { "raw": 1.52, "fmt": "1.52" }
                              },
                              "epsRevisions": {
                                "upLast7days": 1,
                                "upLast30days": 3,
                                "downLast30days": 2,
                                "downLast90days": 4
                              }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=earningsHistory", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "earningsHistory": {
                            "history": [{
                              "quarter": { "raw": 1711929600, "fmt": "2024-04-01" },
                              "epsActual": { "raw": 1.53, "fmt": "1.53" },
                              "epsEstimate": { "raw": 1.5, "fmt": "1.50" },
                              "epsDifference": { "raw": 0.03, "fmt": "0.03" },
                              "surprisePercent": { "raw": 0.02, "fmt": "2.00%" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=majorHoldersBreakdown", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "majorHoldersBreakdown": {
                            "insidersPercentHeld": { "raw": 0.02, "fmt": "2.00%" },
                            "institutionsPercentHeld": { "raw": 0.61, "fmt": "61.00%" },
                            "institutionsFloatPercentHeld": { "raw": 0.62, "fmt": "62.00%" },
                            "institutionsCount": { "raw": 6400, "fmt": "6.4k" }
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=institutionOwnership", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "institutionOwnership": {
                            "ownershipList": [{
                              "organization": "Example Capital",
                              "reportDate": { "raw": 1711929600, "fmt": "2024-04-01" },
                              "pctHeld": { "raw": 0.08, "fmt": "8.00%" },
                              "position": { "raw": 1200000000, "fmt": "1.2B" },
                              "value": { "raw": 230000000000, "fmt": "230B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=fundOwnership", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "fundOwnership": {
                            "ownershipList": [{
                              "organization": "Example Index Fund",
                              "reportDate": { "raw": 1714521600, "fmt": "2024-05-01" },
                              "pctHeld": { "raw": 0.05, "fmt": "5.00%" },
                              "position": { "raw": 750000000, "fmt": "750M" },
                              "value": { "raw": 144000000000, "fmt": "144B" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=netSharePurchaseActivity", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "netSharePurchaseActivity": {
                            "period": "6m",
                            "buyInfoCount": { "raw": 12, "fmt": "12" },
                            "buyInfoShares": { "raw": 100000, "fmt": "100k" },
                            "sellInfoCount": { "raw": 4, "fmt": "4" },
                            "sellInfoShares": { "raw": 25000, "fmt": "25k" },
                            "netInfoShares": { "raw": 75000, "fmt": "75k" },
                            "totalInsiderShares": { "raw": 3000000, "fmt": "3M" }
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=insiderTransactions", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "insiderTransactions": {
                            "transactions": [{
                              "filerName": "Jane Appleseed",
                              "filerRelation": "Officer",
                              "startDate": { "raw": 1717200000, "fmt": "2024-06-01" },
                              "transaction": "Sale",
                              "shares": { "raw": 1000, "fmt": "1k" },
                              "value": { "raw": 190000, "fmt": "190k" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            if (request.Uri.Query.Contains("modules=insiderHolders", StringComparison.Ordinal))
            {
                return JsonResponse("""
                    {
                      "quoteSummary": {
                        "result": [{
                          "insiderHolders": {
                            "holders": [{
                              "name": "Jane Appleseed",
                              "relation": "Officer",
                              "latestTransDate": { "raw": 1717200000, "fmt": "2024-06-01" },
                              "positionDirect": { "raw": 50000, "fmt": "50k" }
                            }]
                          }
                        }],
                        "error": null
                      }
                    }
                    """);
            }

            throw new InvalidOperationException($"Unexpected request: {request.Uri}");
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));
        var ticker = client.Ticker("aapl");

        var income = await ticker.IncomeStatementAsync();
        var quarterlyIncome = await ticker.QuarterlyIncomeStatementAsync();
        var balance = await ticker.BalanceSheetAsync();
        var quarterlyBalance = await ticker.QuarterlyBalanceSheetAsync();
        var cashFlow = await ticker.CashFlowAsync();
        var quarterlyCashFlow = await ticker.QuarterlyCashFlowAsync();
        var ttmIncome = await ticker.TtmIncomeStatementAsync();
        var ttmCashFlow = await ticker.TtmCashFlowAsync();
        var targets = await ticker.AnalystPriceTargetsAsync();
        var earningsEstimates = await ticker.EarningsEstimateAsync();
        var revenueEstimates = await ticker.RevenueEstimateAsync();
        var earningsHistory = await ticker.EarningsHistoryAsync();
        var epsTrend = await ticker.EpsTrendAsync();
        var epsRevisions = await ticker.EpsRevisionsAsync();
        var growth = await ticker.GrowthEstimatesAsync();
        var majorHolders = await ticker.MajorHoldersAsync();
        var institutional = await ticker.InstitutionalHoldersAsync();
        var mutualFund = await ticker.MutualFundHoldersAsync();
        var insiderPurchases = await ticker.InsiderPurchasesAsync();
        var insiderTransactions = await ticker.InsiderTransactionsAsync();
        var insiderRoster = await ticker.InsiderRosterHoldersAsync();

        Assert.Equal(383000000000m, income.Single().TotalRevenue);
        Assert.Equal(4, quarterlyIncome.Count);
        Assert.Equal(352000000000m, balance.Single().TotalAssets);
        Assert.Equal(270000000000m, quarterlyBalance.Single().TotalLiab);
        Assert.Equal(110000000000m, cashFlow.Single().TotalCashFromOperatingActivities);
        Assert.Equal(4, quarterlyCashFlow.Count);
        Assert.Equal(100m, ttmIncome?.TotalRevenue);
        Assert.Equal(10m, ttmIncome?.NetIncome);
        Assert.Equal(1000m, ttmCashFlow?.TotalCashFromOperatingActivities);
        Assert.Equal(-100m, ttmCashFlow?.CapitalExpenditures);
        Assert.Equal(210.25m, targets?.TargetMeanPrice);
        Assert.Equal(38, targets?.NumberOfAnalystOpinions);
        Assert.Equal(1.55m, earningsEstimates.Single().Average);
        Assert.Equal("0q", earningsEstimates.Single().Period);
        Assert.Equal(94000000000m, revenueEstimates.Single().Average);
        Assert.Equal(1.53m, earningsHistory.Single().EpsActual);
        Assert.Equal(1.54m, epsTrend.Single().SevenDaysAgo);
        Assert.Equal(3, epsRevisions.Single().UpLast30days);
        Assert.Equal(0.12m, growth.Single().Growth);
        Assert.Equal(0.61m, majorHolders?.InstitutionsPercentHeld);
        Assert.Equal("Example Capital", institutional.Single().Organization);
        Assert.Equal(0.05m, mutualFund.Single().PctHeld);
        Assert.Equal(75000, insiderPurchases?.NetInfoShares);
        Assert.Equal("Sale", insiderTransactions.Single().Transaction);
        Assert.Equal(50000, insiderRoster.Single().PositionDirect);
    }

    [Fact]
    public async Task Contract_FundFixture_MatchesYfinanceFundsDataOutputs()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(ReadFixture("quote-summary-vti-fund.json")));

        using var client = new YahooFinanceClient(new HttpClient(handler));
        var ticker = client.Ticker("vti");

        var data = await ticker.FundsDataAsync();
        var topHoldings = await ticker.FundTopHoldingsAsync();
        var operations = data.FundOperations.ToDictionary(row => row.Attribute);
        var equity = data.EquityHoldings.ToDictionary(row => row.Average);

        Assert.Equal("VTI", data.Symbol);
        Assert.Equal("ETF", data.QuoteType);
        Assert.StartsWith("The fund employs an indexing investment approach", data.Description);
        Assert.Equal("Large Blend", data.FundOverview?.CategoryName);
        Assert.Equal("Vanguard", data.FundOverview?.Family);
        Assert.Equal("Exchange Traded Fund", data.FundOverview?.LegalType);
        Assert.Equal(0.0003m, operations["Annual Report Expense Ratio"].Fund);
        Assert.Equal(0.0075m, operations["Annual Report Expense Ratio"].CategoryAverage);
        Assert.Equal(1730000000000m, operations["Total Net Assets"].Fund);
        Assert.Equal(0.0125m, data.AssetClasses.CashPosition);
        Assert.Equal(0.9825m, data.AssetClasses.StockPosition);
        Assert.Equal("AAPL", topHoldings.First().Symbol);
        Assert.Equal("Apple Inc.", topHoldings.First().Name);
        Assert.Equal(0.061m, topHoldings.First().HoldingPercent);
        Assert.Equal(24.1m, equity["Price/Earnings"].Fund);
        Assert.Equal(23.5m, equity["Price/Earnings"].CategoryAverage);
        Assert.Equal(0.303m, data.SectorWeightings["technology"]);
        Assert.Equal(0.0m, data.BondRatings["aaa"]);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal("/v10/finance/quoteSummary/VTI", request.Uri.AbsolutePath);
            Assert.Contains("modules=quoteType%2CsummaryProfile%2CtopHoldings%2CfundProfile", request.Uri.Query);
        });
    }

    [Fact]
    public async Task Contract_SectorAndIndustryFixtures_MatchYfinanceDomainOutputs()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.AbsolutePath == "/v1/finance/sectors/technology")
            {
                return JsonResponse(ReadFixture("sector-technology.json"));
            }

            if (request.Uri.AbsolutePath == "/v1/finance/industries/software-infrastructure")
            {
                return JsonResponse(ReadFixture("industry-software-infrastructure.json"));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Uri}");
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var sector = await client.GetSectorAsync("Technology", "us");
        var industry = await client.Industry("Software-Infrastructure", "us").DataAsync();

        Assert.Equal("technology", sector.Key);
        Assert.Equal("US", sector.Region);
        Assert.Equal("Technology", sector.Name);
        Assert.Equal("^YH101", sector.Symbol);
        Assert.Equal(779, sector.Overview.CompaniesCount);
        Assert.Equal(21400000000000m, sector.Overview.MarketCap);
        Assert.Equal(0.318m, sector.Overview.MarketWeight);
        Assert.Equal("Microsoft Corporation", sector.TopCompanies.First().Name);
        Assert.Equal(0.071m, sector.TopCompanies.First().MarketWeight);
        Assert.Equal("Technology Select Sector SPDR Fund", sector.TopEtfs["XLK"]);
        Assert.Equal("Vanguard Information Technology Index Fund", sector.TopMutualFunds["VITAX"]);
        Assert.Equal(["software-infrastructure", "semiconductors"], sector.Industries.Select(row => row.Key).ToArray());

        Assert.Equal("software-infrastructure", industry.Key);
        Assert.Equal("Technology", industry.SectorName);
        Assert.Equal(173, industry.Overview.CompaniesCount);
        Assert.Equal("MSFT", industry.TopCompanies.Single().Symbol);
        Assert.Equal("ORCL", industry.TopPerformingCompanies.Single().Symbol);
        Assert.Equal(141.75m, industry.TopPerformingCompanies.Single().LastPrice);
        Assert.Equal("NET", industry.TopGrowthCompanies.Single().Symbol);
        Assert.Equal(0.284m, industry.TopGrowthCompanies.Single().GrowthEstimate);
        Assert.All(handler.Requests, request => Assert.Contains("region=US", request.Uri.Query));
    }

    [Fact]
    public async Task Contract_MarketFixtures_ParseTypedStatusAndRegionalLimitation()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Uri.AbsolutePath == "/v6/finance/quote/marketSummary")
            {
                return JsonResponse(ReadFixture("market-summary-us.json"));
            }

            if (request.Uri.Query.Contains("market=EUROPE", StringComparison.Ordinal))
            {
                return JsonResponse(ReadFixture("market-status-europe-yahoo-limited.json"));
            }

            return JsonResponse(ReadFixture("market-status-us.json"));
        });

        using var client = new YahooFinanceClient(new HttpClient(handler));

        var us = await client.GetMarketAsync(MarketRegion.US);
        var europe = await client.GetMarketAsync(MarketRegion.Europe);

        Assert.Equal(43800.25m, us.SummaryByExchange["DJI"].RegularMarketPrice);
        Assert.NotNull(us.Status);
        Assert.Equal("us", us.Status.Id);
        Assert.Equal("closed", us.Status.Status);
        Assert.Equal(new DateTimeOffset(2026, 6, 26, 9, 30, 0, TimeSpan.FromHours(-4)), us.Status.Open);
        Assert.Equal("America/New_York", us.Status.Timezone?.Tz);
        Assert.Equal("EDT", us.Status.Timezone?.Short);
        Assert.NotNull(us.RawStatus);

        Assert.Null(europe.Status);
        Assert.NotNull(europe.RawStatus);
    }

    [Fact]
    public async Task FileYahooFinanceAuthStore_RoundTripsAndClearsState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "YFSharp.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "auth.json");
        var store = new FileYahooFinanceAuthStore(path);

        try
        {
            await store.SaveAsync(new YahooFinanceAuthState
            {
                Cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["A3"] = "session-cookie"
                },
                Crumb = "crumb-token",
                Strategy = YahooFinanceCookieStrategy.Csrf,
                Timestamp = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero)
            });

            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal("session-cookie", loaded.Cookies["A3"]);
            Assert.Equal("crumb-token", loaded.Crumb);
            Assert.Equal(YahooFinanceCookieStrategy.Csrf, loaded.Strategy);
            Assert.Equal(new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero), loaded.Timestamp);

            await store.ClearAsync();
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddYFSharp_RegistersTypedClientOptionsAndInterface()
    {
        var authStore = new InMemoryYahooFinanceAuthStore();
        var services = new ServiceCollection();

        services.AddYFSharp(options => options with
        {
            AuthStore = authStore,
            MaxRetries = 2,
            RateLimitBackoff = _ => TimeSpan.Zero,
            UserAgent = "YFSharp.Tests/1.0"
        });

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<YahooFinanceClientOptions>();
        Assert.Same(authStore, options.AuthStore);
        Assert.Equal(2, options.MaxRetries);
        Assert.IsType<YahooFinanceClient>(provider.GetRequiredService<YahooFinanceClient>());
        Assert.IsType<YahooFinanceClient>(provider.GetRequiredService<IYahooFinanceClient>());
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string ReadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Yahoo", fileName);
        return File.ReadAllText(path);
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        string Body,
        string CookieHeader,
        IReadOnlyDictionary<string, string> Headers);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubHttpMessageHandler(Func<CapturedRequest, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var cookieHeader = request.Headers.TryGetValues("Cookie", out var values)
                ? string.Join("; ", values)
                : string.Empty;
            var allHeaders = request.Content is null
                ? request.Headers
                : request.Headers.Concat(request.Content.Headers);
            var headers = allHeaders
                .ToDictionary(
                    header => header.Key,
                    header => string.Join(", ", header.Value),
                    StringComparer.OrdinalIgnoreCase);

            var captured = new CapturedRequest(request.Method, request.RequestUri!, body, cookieHeader, headers);
            Requests.Add(captured);
            return responder(captured);
        }
    }
}
