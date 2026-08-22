using Microsoft.AspNetCore.Http;

namespace Unpoly.Blazor;

/// <summary>
/// Writes Unpoly's response headers.
/// Spec: https://unpoly.com/up.protocol (14 response headers + 1 cookie)
/// </summary>
public static class UpResponse
{
    // ─────────────────────────────────────────────────────────────
    // PHASE A · Targeting                      📖 /targeting-fragments
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Target — tell the client to swap a different selector than the one it asked for.
    /// Note: on a 4xx/5xx status the client uses its fail target, and this header still overrides it.
    /// Pass ":none" to make the client swap nothing.
    /// </summary>
    public static void UpRetarget(this HttpContext c, string cssSelector)
        => c.Response.Headers["X-Up-Target"] = cssSelector;

    // ─────────────────────────────────────────────────────────────
    // PHASE B · Cache                          📖 /caching
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Vary — declare which request headers changed this response body.
    /// Required whenever the server branches on X-Up-Target: without it a shared cache may
    /// hand a fragment response to a full page load, or the reverse.
    /// Merges with any Vary already set instead of overwriting it.
    /// 📖 https://unpoly.com/optimizing-responses
    /// </summary>
    public static void UpVary(this HttpContext c, params string[] requestHeaderNames)
    {
        var existing = c.Response.Headers.Vary.ToString();

        var merged = existing
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(requestHeaderNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        c.Response.Headers.Vary = string.Join(", ", merged);
    }

    /// <summary>X-Up-Expire-Cache — mark cache entries stale; Unpoly refetches them when needed. TODO Phase B</summary>
    public static void UpExpireCache(this HttpContext c, string pattern = "*")
        => throw new NotImplementedException("Phase B · 📖 https://unpoly.com/caching");

    /// <summary>
    /// X-Up-Evict-Cache — drop entries from the cache entirely.
    /// Unlike expiry, evicted content is never reused. TODO Phase B
    /// </summary>
    public static void UpEvictCache(this HttpContext c, string pattern = "*")
        => throw new NotImplementedException("Phase B · 📖 https://unpoly.com/caching");

    /// <summary>X-Up-Clear-Cache — legacy header; check the spec before using it. TODO Phase B</summary>
    public static void UpClearCache(this HttpContext c, string pattern = "*")
        => throw new NotImplementedException("Phase B");

    // ─────────────────────────────────────────────────────────────
    // PHASE D · Layers                         📖 /closing-overlays
    // ─────────────────────────────────────────────────────────────

    /// <summary>X-Up-Open-Layer — the SERVER opens an overlay with the returned HTML. TODO Phase D · 📖 /opening-overlays</summary>
    public static void UpOpenLayer(this HttpContext c, object options)
        => throw new NotImplementedException("Phase D · 📖 https://unpoly.com/opening-overlays");

    /// <summary>
    /// X-Up-Accept-Layer — close the overlay SUCCESSFULLY, handing a value back to the opener.
    /// This is the "accept" half of the accept/dismiss pair. TODO Phase D
    /// </summary>
    public static void UpAcceptLayer(this HttpContext c, object? value = null)
        => throw new NotImplementedException("Phase D · 📖 https://unpoly.com/closing-overlays");

    /// <summary>X-Up-Dismiss-Layer — close the overlay because the user CANCELLED. Not a success. TODO Phase D</summary>
    public static void UpDismissLayer(this HttpContext c, object? value = null)
        => throw new NotImplementedException("Phase D · 📖 https://unpoly.com/closing-overlays");

    // ─────────────────────────────────────────────────────────────
    // PHASE E · History                        📖 /updating-history
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Title — set the document title when the response carries no &lt;head&gt;.
    /// The value is a JSON-encoded string, not a bare string. TODO Phase E
    /// </summary>
    public static void UpTitle(this HttpContext c, string title)
        => throw new NotImplementedException("Phase E · 📖 https://unpoly.com/updating-history");

    /// <summary>X-Up-Location — report the response's real URL after redirects. TODO Phase E</summary>
    public static void UpLocation(this HttpContext c, string url)
        => throw new NotImplementedException("Phase E");

    /// <summary>X-Up-Method — the real HTTP method of the final response. Pairs with the _up_method cookie. TODO Phase E</summary>
    public static void UpMethod(this HttpContext c, string method)
        => throw new NotImplementedException("Phase E");

    // ─────────────────────────────────────────────────────────────
    // PHASE F · Events                         📖 /flashes
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Events — emit client-side events from the server. Calling this more than once
    /// accumulates into the same header, whose value is a JSON array of objects keyed by "type".
    /// TODO Phase F
    /// </summary>
    public static void UpEmit(this HttpContext c, string type, object? props = null)
        => throw new NotImplementedException("Phase F · 📖 https://unpoly.com/up.event");
}
