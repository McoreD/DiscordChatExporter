using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Utils;
using Xunit;

namespace DiscordChatExporter.Cli.Tests.Specs;

public class AsyncCacheSpecs
{
    [Fact]
    public async Task Parallel_metadata_requests_share_one_fetch()
    {
        var cache = new AsyncCache<string, int>();
        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var fetches = 0;
        var requests = Enumerable
            .Range(0, 12)
            .Select(_ =>
                cache
                    .GetOrAddAsync(
                        "guild/roles",
                        () =>
                        {
                            Interlocked.Increment(ref fetches);
                            return new ValueTask<int>(completion.Task);
                        }
                    )
                    .AsTask()
            )
            .ToArray();

        Assert.Equal(1, fetches);
        completion.SetResult(42);
        Assert.All(await Task.WhenAll(requests), value => Assert.Equal(42, value));
        Assert.Equal(
            42,
            await cache.GetOrAddAsync("guild/roles", () => throw new InvalidOperationException())
        );
    }

    [Fact]
    public async Task Different_metadata_keys_are_fetched_independently()
    {
        var cache = new AsyncCache<string, int>();
        Assert.Equal(1, await cache.GetOrAddAsync("guild/roles", () => ValueTask.FromResult(1)));
        Assert.Equal(2, await cache.GetOrAddAsync("guild/channels", () => ValueTask.FromResult(2)));
    }

    [Fact]
    public async Task Failed_fetches_can_be_retried()
    {
        var cache = new AsyncCache<string, int>();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache
                .GetOrAddAsync(
                    "guild/roles",
                    () => ValueTask.FromException<int>(new InvalidOperationException())
                )
                .AsTask()
        );
        Assert.Equal(42, await cache.GetOrAddAsync("guild/roles", () => ValueTask.FromResult(42)));
    }

    [Fact]
    public async Task Canceled_fetches_can_be_retried()
    {
        var cache = new AsyncCache<string, int>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            cache
                .GetOrAddAsync(
                    "guild/roles",
                    () => ValueTask.FromCanceled<int>(new CancellationToken(true))
                )
                .AsTask()
        );
        Assert.Equal(42, await cache.GetOrAddAsync("guild/roles", () => ValueTask.FromResult(42)));
    }
}
