using IronGate.Core.Storage;

namespace IronGate.Tests.Core.Storage;

public class InMemoryRateLimitStoreTests
{
    private readonly InMemoryRateLimitStore _store = new();

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsZero()
    {
        var count = await _store.GetAsync("missing-key");

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetAsync_AfterSet_ReturnsStoredCount()
    {
        await _store.SetAsync("key", 3, TimeSpan.FromMinutes(1));

        var count = await _store.GetAsync("key");

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task SetAsync_OverwritesPreviousValue()
    {
        await _store.SetAsync("key", 3, TimeSpan.FromMinutes(1));
        await _store.SetAsync("key", 7, TimeSpan.FromMinutes(1));

        var count = await _store.GetAsync("key");

        Assert.Equal(7, count);
    }

    [Fact]
    public async Task GetAsync_AfterExpiry_ReturnsZero()
    {
        await _store.SetAsync("key", 5, TimeSpan.FromMilliseconds(50));

        await Task.Delay(100); // wait for expiry

        var count = await _store.GetAsync("key");

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetAsync_BeforeExpiry_ReturnsStoredCount()
    {
        await _store.SetAsync("key", 5, TimeSpan.FromSeconds(10));

        var count = await _store.GetAsync("key");

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetAsync_DifferentKeys_AreIndependent()
    {
        await _store.SetAsync("key-a", 1, TimeSpan.FromMinutes(1));
        await _store.SetAsync("key-b", 9, TimeSpan.FromMinutes(1));

        Assert.Equal(1, await _store.GetAsync("key-a"));
        Assert.Equal(9, await _store.GetAsync("key-b"));
    }
}
