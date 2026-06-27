using System.Text.Json;
using YFSharp.Internal;

namespace YFSharp;

public interface IYahooFinanceAuthStore
{
    ValueTask<YahooFinanceAuthState?> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(YahooFinanceAuthState state, CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

public sealed record YahooFinanceAuthState
{
    public Dictionary<string, string> Cookies { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? Crumb { get; init; }

    public YahooFinanceCookieStrategy Strategy { get; init; } = YahooFinanceCookieStrategy.Basic;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum YahooFinanceCookieStrategy
{
    Basic,
    Csrf
}

public sealed class InMemoryYahooFinanceAuthStore : IYahooFinanceAuthStore
{
    private readonly object _lock = new();
    private YahooFinanceAuthState? _state;

    public InMemoryYahooFinanceAuthStore()
    {
    }

    public InMemoryYahooFinanceAuthStore(YahooFinanceAuthState state)
    {
        _state = Clone(state);
    }

    public ValueTask<YahooFinanceAuthState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            return ValueTask.FromResult(_state is null ? null : Clone(_state));
        }
    }

    public ValueTask SaveAsync(YahooFinanceAuthState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _state = Clone(state);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _state = null;
        }

        return ValueTask.CompletedTask;
    }

    private static YahooFinanceAuthState Clone(YahooFinanceAuthState state) =>
        state with
        {
            Cookies = new Dictionary<string, string>(state.Cookies, StringComparer.OrdinalIgnoreCase)
        };
}

public sealed class FileYahooFinanceAuthStore : IYahooFinanceAuthStore
{
    private readonly string _path;

    public FileYahooFinanceAuthStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Auth store path cannot be empty.", nameof(path));
        }

        _path = path;
    }

    public async ValueTask<YahooFinanceAuthState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return await JsonSerializer.DeserializeAsync<YahooFinanceAuthState>(
                stream,
                YahooJson.SerializerOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public async ValueTask SaveAsync(YahooFinanceAuthState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    state,
                    YahooJson.SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
        catch (DirectoryNotFoundException)
        {
        }

        return ValueTask.CompletedTask;
    }
}
