using System.Text.Json;
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

    /// <summary>
    /// X-Up-Expire-Cache — mark cache entries stale. Expired content is still rendered
    /// immediately, then refetched; it is not thrown away.
    ///
    /// Rarely needed: Unpoly already expires the ENTIRE cache after any non-GET request.
    /// Reach for this only to expire a subset, or to expire from a GET.
    ///
    /// <paramref name="urlPattern"/> is a URL glob such as "/notes/*", or "*" for everything.
    /// 📖 https://unpoly.com/X-Up-Expire-Cache
    /// </summary>
    public static void UpExpireCache(this HttpContext c, string urlPattern = "*")
        => c.Response.Headers["X-Up-Expire-Cache"] = urlPattern;

    /// <summary>
    /// X-Up-Expire-Cache: false — keep the cache that a non-GET request would otherwise clear.
    /// For a POST that changed nothing the user can see, such as recording an analytics hit.
    /// </summary>
    public static void UpKeepCache(this HttpContext c)
        => c.Response.Headers["X-Up-Expire-Cache"] = "false";

    /// <summary>
    /// X-Up-Evict-Cache — drop entries outright. Unlike expiry, evicted content is never
    /// rendered again, so the user waits for the network instead of seeing a stale flash.
    /// Use it when stale content would be wrong to show at all, not merely out of date.
    /// 📖 https://unpoly.com/caching
    /// </summary>
    public static void UpEvictCache(this HttpContext c, string urlPattern = "*")
        => c.Response.Headers["X-Up-Evict-Cache"] = urlPattern;

    // ─────────────────────────────────────────────────────────────
    // PHASE B · Conditional requests           📖 /conditional-requests
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Publishes the fragment's version and reports whether the client already has it.
    ///
    /// Returns true when the client's copy is current: the response is set to 304 and the
    /// caller must render NOTHING. Unpoly uses this for reloading, cache revalidation and
    /// polling, so on a polled fragment it turns most requests into an empty 304.
    ///
    /// Pass an <paramref name="etag"/> (a content hash) or a <paramref name="lastModified"/>
    /// time, or both. Last-Modified is compared at whole-second precision because that is
    /// all an HTTP date carries — without truncating, a sub-second difference would make
    /// every comparison miss.
    /// 📖 https://unpoly.com/conditional-requests
    /// </summary>
    public static bool UpNotModified(this HttpContext c, string? etag = null, DateTimeOffset? lastModified = null)
    {
        // Safe methods only. On a POST, If-None-Match means optimistic concurrency (answer
        // 412), not caching — and answering 304 there would skip the handler entirely, so
        // the form submission would silently do nothing.
        if (!HttpMethods.IsGet(c.Request.Method) && !HttpMethods.IsHead(c.Request.Method))
            return false;

        if (etag is not null) c.Response.Headers.ETag = etag;

        var truncated = lastModified?.AddTicks(-(lastModified.Value.Ticks % TimeSpan.TicksPerSecond));
        if (truncated is not null) c.Response.Headers.LastModified = truncated.Value.ToString("R");

        var fresh = etag is not null && MatchesETag(c, etag)
                 || truncated is not null && NotNewerThan(c, truncated.Value);

        if (fresh) c.Response.StatusCode = StatusCodes.Status304NotModified;
        return fresh;
    }

    private static bool MatchesETag(HttpContext c, string etag)
    {
        var header = c.Request.Headers.IfNoneMatch.ToString();
        if (string.IsNullOrEmpty(header)) return false;
        if (header.Trim() == "*") return true;

        foreach (var candidate in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // A cache may weaken a strong tag on the way back, so compare past the W/ prefix.
            var bare = candidate.StartsWith("W/", StringComparison.Ordinal) ? candidate[2..] : candidate;
            var mine = etag.StartsWith("W/", StringComparison.Ordinal) ? etag[2..] : etag;
            if (bare == mine) return true;
        }
        return false;
    }

    private static bool NotNewerThan(HttpContext c, DateTimeOffset lastModified)
    {
        var header = c.Request.Headers.IfModifiedSince.ToString();
        return DateTimeOffset.TryParse(header, out var since) && lastModified <= since;
    }

    // ─────────────────────────────────────────────────────────────
    // PHASE D · Layers                         📖 /closing-overlays
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Open-Layer — the SERVER decides this response opens in a new overlay, even though
    /// the link asked for an ordinary swap.
    ///
    /// Pass null for Unpoly's defaults, or an object of render options:
    /// <c>UpOpenLayer(new { mode = "drawer", size = "large", target = "#menu" })</c>.
    /// 📖 https://unpoly.com/opening-overlays
    /// </summary>
    public static void UpOpenLayer(this HttpContext c, object? options = null)
        => c.Response.Headers["X-Up-Open-Layer"] = options is null ? "{}" : JsonSerializer.Serialize(options);

    /// <summary>
    /// X-Up-Accept-Layer — close the overlay as a SUCCESS, handing a value back to whatever
    /// opened it. The opener's [up-on-accepted] receives it as <c>value</c>.
    ///
    /// Accept and dismiss are not interchangeable: accept means the sub-task completed and
    /// the parent interaction should continue with the result; dismiss means the user backed
    /// out. 📖 https://unpoly.com/closing-overlays
    /// </summary>
    public static void UpAcceptLayer(this HttpContext c, object? value = null)
        => c.Response.Headers["X-Up-Accept-Layer"] = JsonSerializer.Serialize(value);

    /// <summary>
    /// X-Up-Dismiss-Layer — close the overlay because the user backed out. The value is a
    /// dismissal *reason*, not a result. 📖 https://unpoly.com/closing-overlays
    /// </summary>
    public static void UpDismissLayer(this HttpContext c, object? value = null)
        => c.Response.Headers["X-Up-Dismiss-Layer"] = JsonSerializer.Serialize(value);

    /// <summary>
    /// X-Up-Context — hand the layer a changed context object. It persists for that layer and
    /// comes back on its next request.
    ///
    /// Anything that reads the context must also Vary on it, or two layers with different
    /// context share one cache entry. <c>UseUnpoly()</c> already lists X-Up-Context.
    /// 📖 https://unpoly.com/context
    /// </summary>
    public static void UpSetContext(this HttpContext c, object context)
        => c.Response.Headers["X-Up-Context"] = JsonSerializer.Serialize(context);

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
