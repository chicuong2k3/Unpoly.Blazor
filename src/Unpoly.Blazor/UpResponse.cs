using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// X-Up-Title — set the document title from a response that carries no &lt;title&gt;.
    ///
    /// The value is a **JSON-encoded** string: the quotes are part of the header, so a title
    /// of Playlist browser is sent as <c>X-Up-Title: "Playlist browser"</c>. Sending it bare
    /// is the mistake worth guarding against, which is why this method does the encoding.
    ///
    /// Not needed here while &lt;HeadOutlet /&gt; stays outside &lt;UpChrome&gt; — the real
    /// &lt;title&gt; survives fragment responses. Reach for it when a page wants a title
    /// different from its &lt;PageTitle&gt;.
    /// 📖 https://unpoly.com/X-Up-Title
    /// </summary>
    public static void UpTitle(this HttpContext c, string title)
        => c.Response.Headers["X-Up-Title"] = JsonSerializer.Serialize(title);

    /// <summary>
    /// X-Up-Location — the URL Unpoly should show, as a plain string.
    ///
    /// Without it Unpoly uses the request URL. Send it when the response is not what the
    /// request URL suggests: a redirect Unpoly cannot see, or a canonical form.
    /// 📖 https://unpoly.com/updating-history
    /// </summary>
    public static void UpLocation(this HttpContext c, string url)
        => c.Response.Headers["X-Up-Location"] = url;

    /// <summary>
    /// X-Up-Method — the HTTP method Unpoly should record for this location, plain text.
    ///
    /// Unpoly assumes a redirect landed on GET, which is right for 301, 302 and 303 and
    /// wrong for 307 and 308. It also cannot see a redirect to the *same* URL with a
    /// different method, such as POST /users to GET /users. Send it in those two cases.
    /// 📖 https://unpoly.com/X-Up-Method
    /// </summary>
    public static void UpMethod(this HttpContext c, string method)
        => c.Response.Headers["X-Up-Method"] = method.ToUpperInvariant();

    /// <summary>
    /// The _up_method cookie — tells Unpoly that the FULL PAGE it is booting on was produced
    /// by a non-GET request. Headers cannot carry that: the browser navigated normally, so no
    /// Unpoly request was involved to put a header on.
    ///
    /// Unpoly pops the cookie (reads it, then deletes it) during boot, so it is single-use.
    /// Set it only when rendering a full document in response to a non-GET.
    /// 📖 https://unpoly.com/X-Up-Method
    /// </summary>
    public static void UpMethodCookie(this HttpContext c, string? method = null)
        => c.Response.Cookies.Append("_up_method", (method ?? c.Request.Method).ToUpperInvariant(),
            new CookieOptions { Path = "/", HttpOnly = false, IsEssential = true });

    // ─────────────────────────────────────────────────────────────
    // PHASE F · Events                         📖 /flashes
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Events — emit client-side events from the server, alongside the fragment update.
    ///
    /// The header is a JSON array; calling this more than once accumulates into it. Every
    /// entry needs a <c>type</c>, which is why it is a separate parameter rather than
    /// something the caller might forget inside <paramref name="props"/>.
    ///
    /// Events land on <c>document</c> by default. Pass <c>layer: "current"</c> in
    /// <paramref name="props"/> to emit on the updated layer instead.
    ///
    /// Non-ASCII is escaped, because Unpoly states plainly that HTTP headers may only carry
    /// US-ASCII. A message with Vietnamese text in it would otherwise be an invalid header.
    /// 📖 https://unpoly.com/X-Up-Events
    /// </summary>
    public static void UpEmit(this HttpContext c, string type, object? props = null)
    {
        var existing = c.Response.Headers["X-Up-Events"].ToString();

        var events = string.IsNullOrEmpty(existing)
            ? new JsonArray()
            : JsonNode.Parse(existing)!.AsJsonArray();

        var e = props is null
            ? new JsonObject()
            : JsonSerializer.SerializeToNode(props)!.AsObject().DeepClone().AsObject();

        e["type"] = type;
        events.Add(e);

        c.Response.Headers["X-Up-Events"] = events.ToJsonString();
    }

    private static JsonArray AsJsonArray(this JsonNode node) => (JsonArray)node;
}
