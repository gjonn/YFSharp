using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using YFSharp.Internal;
using YFSharp.Models;

namespace YFSharp;

public sealed class YahooWebSocketClient : IAsyncDisposable
{
    public const string DefaultUrl = "wss://streamer.finance.yahoo.com/?version=2";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly HashSet<string> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _heartbeatCancellation;
    private Task? _heartbeatTask;
    private bool _disposed;

    public Uri Url { get; }

    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(3);

    public TimeSpan SubscriptionRefreshInterval { get; set; } = TimeSpan.FromSeconds(15);

    public int ReceiveBufferSize { get; set; } = 64 * 1024;

    public YahooWebSocketClient(string url = DefaultUrl)
        : this(new Uri(url, UriKind.Absolute))
    {
    }

    public YahooWebSocketClient(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        Url = url;
    }

    public IReadOnlyCollection<string> Subscriptions
    {
        get
        {
            _connectionLock.Wait();
            try
            {
                return _subscriptions.Order(StringComparer.Ordinal).ToArray();
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }

    public static StreamingPrice DecodeMessage(string base64Message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Message);

        try
        {
            return YahooPricingDataDecoder.Decode(Convert.FromBase64String(base64Message));
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            throw new YahooFinanceException("Yahoo websocket message could not be decoded.", exception);
        }
    }

    public static StreamingPrice DecodeJsonMessage(string jsonMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonMessage);

        try
        {
            using var document = JsonDocument.Parse(jsonMessage);
            if (!document.RootElement.TryGetProperty("message", out var messageElement)
                || messageElement.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Yahoo websocket JSON did not include a string message property.");
            }

            return DecodeMessage(messageElement.GetString()!);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentException)
        {
            throw new YahooFinanceException("Yahoo websocket JSON message could not be decoded.", exception);
        }
    }

    public Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default) =>
        SubscribeAsync([symbol], cancellationToken);

    public async Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var normalizedSymbols = NormalizeSymbols(symbols);

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var symbol in normalizedSymbols)
            {
                _subscriptions.Add(symbol);
            }

            var socket = await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false);
            await SendSubscribeSnapshotCoreAsync(socket, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default) =>
        UnsubscribeAsync([symbol], cancellationToken);

    public async Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        var normalizedSymbols = NormalizeSymbols(symbols);

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var symbol in normalizedSymbols)
            {
                _subscriptions.Remove(symbol);
            }

            if (_webSocket?.State == WebSocketState.Open)
            {
                await SendJsonCoreAsync(
                    _webSocket,
                    new UnsubscribeMessage(normalizedSymbols),
                    cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async IAsyncEnumerable<StreamingPrice> StreamAsync(
        IEnumerable<string>? symbols = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        if (symbols is not null)
        {
            await SubscribeAsync(symbols, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            string? jsonMessage;
            try
            {
                jsonMessage = await ReceiveTextMessageAsync(socket, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or IOException)
            {
                await ResetConnectionAfterDelayAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (jsonMessage is null)
            {
                await ResetConnectionAfterDelayAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            yield return DecodeJsonMessage(jsonMessage);
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_heartbeatCancellation is not null)
        {
            await _heartbeatCancellation.CancelAsync().ConfigureAwait(false);
        }

        if (_heartbeatTask is not null)
        {
            try
            {
                await _heartbeatTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseSocketCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CloseAsync(CancellationToken.None).ConfigureAwait(false);
        _heartbeatCancellation?.Dispose();
        _connectionLock.Dispose();
    }

    private async Task<ClientWebSocket> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await EnsureConnectedCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<ClientWebSocket> EnsureConnectedCoreAsync(CancellationToken cancellationToken)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            EnsureHeartbeatStarted();
            return _webSocket;
        }

        await CloseSocketCoreAsync(CancellationToken.None).ConfigureAwait(false);

        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        await socket.ConnectAsync(Url, cancellationToken).ConfigureAwait(false);

        _webSocket = socket;
        if (_subscriptions.Count > 0)
        {
            await SendSubscribeSnapshotCoreAsync(socket, cancellationToken).ConfigureAwait(false);
        }

        EnsureHeartbeatStarted();
        return socket;
    }

    private void EnsureHeartbeatStarted()
    {
        if (_heartbeatTask is { IsCompleted: false })
        {
            return;
        }

        _heartbeatCancellation?.Dispose();
        _heartbeatCancellation = new CancellationTokenSource();
        _heartbeatTask = Task.Run(
            () => SendPeriodicSubscriptionsAsync(_heartbeatCancellation.Token),
            CancellationToken.None);
    }

    private async Task SendPeriodicSubscriptionsAsync(CancellationToken cancellationToken)
    {
        if (SubscriptionRefreshInterval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(SubscriptionRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_webSocket?.State == WebSocketState.Open && _subscriptions.Count > 0)
                    {
                        await SendSubscribeSnapshotCoreAsync(_webSocket, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException or IOException)
                {
                    await CloseSocketCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    _connectionLock.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task ResetConnectionAfterDelayAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseSocketCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }

        if (ReconnectDelay > TimeSpan.Zero)
        {
            await Task.Delay(ReconnectDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendSubscribeSnapshotCoreAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        if (_subscriptions.Count == 0)
        {
            return;
        }

        var symbols = _subscriptions.Order(StringComparer.Ordinal).ToArray();
        await SendJsonCoreAsync(socket, new SubscribeMessage(symbols), cancellationToken).ConfigureAwait(false);
    }

    private static Task SendJsonCoreAsync<T>(
        ClientWebSocket socket,
        T message,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        return socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private async Task<string?> ReceiveTextMessageAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[Math.Max(1024, ReceiveBufferSize)];
        using var message = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType is not WebSocketMessageType.Text and not WebSocketMessageType.Binary)
            {
                throw new YahooFinanceException($"Unexpected Yahoo websocket message type: {result.MessageType}.");
            }

            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(message.ToArray());
    }

    private async Task CloseSocketCoreAsync(CancellationToken cancellationToken)
    {
        if (_webSocket is null)
        {
            return;
        }

        var socket = _webSocket;
        _webSocket = null;

        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is WebSocketException or ObjectDisposedException)
        {
            socket.Abort();
        }
        finally
        {
            socket.Dispose();
        }
    }

    private static string[] NormalizeSymbols(IEnumerable<string> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        var normalized = symbols
            .Select(symbol =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
                return symbol.Trim().ToUpperInvariant();
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one symbol is required.", nameof(symbols));
        }

        return normalized;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record SubscribeMessage(IReadOnlyList<string> Subscribe);

    private sealed record UnsubscribeMessage(IReadOnlyList<string> Unsubscribe);
}
