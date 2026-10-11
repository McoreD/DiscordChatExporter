using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordChatExporter.Core.Utils;

internal sealed class AsyncCache<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _entries = new();

    public async ValueTask<TValue> GetOrAddAsync(TKey key, Func<ValueTask<TValue>> factory)
    {
        var entry = _entries.GetOrAdd(
            key,
            _ => new Lazy<Task<TValue>>(
                () => factory().AsTask(),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        try
        {
            return await entry.Value;
        }
        catch
        {
            // Remove this failed request without evicting a newer replacement.
            ((ICollection<KeyValuePair<TKey, Lazy<Task<TValue>>>>)_entries).Remove(
                new KeyValuePair<TKey, Lazy<Task<TValue>>>(key, entry)
            );
            throw;
        }
    }
}
