namespace Jubin.Data;

/// <summary>
/// In-memory cart and one-shot flash message. Static because this is a single-user lab:
/// a real app would key both by session. The point here is the wire behaviour, not the storage.
/// </summary>
public static class Cart
{
    private static readonly List<string> Items = [];
    private static string? pendingFlash;
    private static (string Type, int Count, string Slug)? pendingEvent;

    public static int Count => Items.Count;

    public static DateTimeOffset LastChanged { get; private set; } =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static string ETag => $"\"cart-{Count}-{LastChanged.ToUnixTimeSeconds()}\"";

    public static void Add(string slug, string name)
    {
        Items.Add(slug);
        LastChanged = DateTimeOffset.UtcNow;
        pendingFlash = $"Đã thêm {name} vào giỏ.";
        pendingEvent = ("cart:changed", Count, slug);
    }

    /// <summary>Reads the pending event and clears it. Survives the redirect a PRG needs.</summary>
    public static (string Type, int Count, string Slug)? TakeEvent()
    {
        var e = pendingEvent;
        pendingEvent = null;
        return e;
    }

    /// <summary>Reads the message and clears it. A flash is shown once, not until replaced.</summary>
    public static string? TakeFlash()
    {
        var f = pendingFlash;
        pendingFlash = null;
        return f;
    }
}
