using Microsoft.AspNetCore.Http;

namespace Unpoly.Blazor;

/// <summary>
/// Reads Unpoly's request headers.
/// Spec: https://unpoly.com/up.protocol (12 request headers)
///
/// In Blazor static SSR, obtain the HttpContext with:
///     [CascadingParameter] public HttpContext Ctx { get; set; } = default!;
/// </summary>
public static class UpRequest
{
    // ─────────────────────────────────────────────────────────────
    // PHASE A · Fragments and targeting        📖 /targeting-fragments
    // ─────────────────────────────────────────────────────────────

    /// <summary>X-Up-Version — its presence means the request came from Unpoly.</summary>
    public static bool IsUnpoly(this HttpContext c) => c.Request.Headers.ContainsKey("X-Up-Version");

    /// <summary>X-Up-Version — the client's Unpoly version, e.g. "3.10.2".</summary>
    public static string? UpVersion(this HttpContext c) => c.Request.Headers["X-Up-Version"];

    /// <summary>
    /// X-Up-Target — the selector the client will swap on a SUCCESSFUL response.
    /// May be a list (".content, .flash") or a pseudo-target (:main, :layer, :none).
    /// </summary>
    public static string? UpTarget(this HttpContext c) => c.Request.Headers["X-Up-Target"];

    /// <summary>X-Up-Fail-Target — the selector used when the response is 4xx/5xx. 📖 /failed-responses</summary>
    public static string? UpFailTarget(this HttpContext c) => c.Request.Headers["X-Up-Fail-Target"];

    /// <summary>Splits X-Up-Target into trimmed selectors. Empty when the header is absent.</summary>
    public static string[] UpTargets(this HttpContext c) =>
        c.UpTarget() is { Length: > 0 } t
            ? t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>
    /// True when the client asked for specific fragments rather than the whole page.
    /// Use it to skip rendering chrome (nav/footer) — see <see cref="UpChrome"/>.
    /// 📖 /optimizing-responses
    /// </summary>
    public static bool IsUpFragment(this HttpContext c)
    {
        if (!c.IsUnpoly()) return false;

        var targets = c.UpTargets();
        if (targets.Length == 0) return false;

        // Both branches must be considered. Unpoly treats any status outside 2xx and 304 as
        // a failure and swaps X-Up-Fail-Target instead, but the chrome is rendered by the
        // layout before the page has decided on a status — so a form declaring
        // [up-fail-target=body] would otherwise get a body swap with no nav in it.
        // Rendering chrome that turns out unnecessary costs bytes; omitting it breaks a page.
        // 📖 https://unpoly.com/failed-responses
        foreach (var t in targets.Concat(c.UpFailTargets()))
            if (BaseTarget(t) is "body" or "html" or ":main" or ":layer")
                return false;

        return true;
    }

    /// <summary>X-Up-Fail-Target, split and trimmed. Empty when the header is absent.</summary>
    public static string[] UpFailTargets(this HttpContext c) =>
        c.UpFailTarget() is { Length: > 0 } t
            ? t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>True when the client wants no content at all (:none). Safe to answer 204.</summary>
    public static bool WantsNothing(this HttpContext c) => c.UpTargets() is [":none"];

    /// <summary>
    /// Modifiers Unpoly appends to a target. They change how the match is applied, never
    /// what is matched, so they must be stripped before a selector is classified.
    /// 📖 https://unpoly.com/targeting-fragments
    /// </summary>
    private static readonly string[] TargetModifiers = [":before", ":after", ":maybe", ":content"];

    /// <summary>
    /// Strips trailing modifiers so ".tasks:after" compares as ".tasks" and "body:after"
    /// still compares as "body". Loops because more than one may be appended.
    /// </summary>
    private static string BaseTarget(string target)
    {
        bool stripped;
        do
        {
            stripped = false;
            foreach (var modifier in TargetModifiers)
            {
                if (!target.EndsWith(modifier, StringComparison.Ordinal)) continue;

                target = target[..^modifier.Length];
                stripped = true;
                break;
            }
        }
        while (stripped);

        return target;
    }

    // ─────────────────────────────────────────────────────────────
    // PHASE C · Forms                          📖 /validation
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Validate — the client wants validation feedback only and the handler
    /// MUST NOT persist anything.
    /// 📖 https://unpoly.com/validation
    /// </summary>
    public static bool IsUpValidating(this HttpContext c) => c.Request.Headers.ContainsKey("X-Up-Validate");

    /// <summary>
    /// X-Up-Validate — the fields being validated, **space** separated (not comma):
    /// Unpoly batches several fields into one request, e.g. "email password".
    /// Empty when the client could not name them — see <see cref="IsUpValidatingUnknown"/>.
    /// 📖 https://unpoly.com/X-Up-Validate
    /// </summary>
    public static string[] UpValidatingFields(this HttpContext c) =>
        c.Request.Headers["X-Up-Validate"].ToString() is { Length: > 0 } v && v != ":unknown"
            ? v.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>
    /// X-Up-Validate: :unknown — the client is validating but cannot say which fields.
    /// Two causes: the origin was not a field, or the list exceeded
    /// up.protocol.config.maxHeaderSize and was collapsed to avoid a 413 from
    /// intermediary infrastructure. Either way, validate the whole form.
    /// </summary>
    public static bool IsUpValidatingUnknown(this HttpContext c) =>
        c.Request.Headers["X-Up-Validate"].ToString() == ":unknown";

    // ─────────────────────────────────────────────────────────────
    // PHASE D · Layers                         📖 /layer-terminology
    // ─────────────────────────────────────────────────────────────

    /// <summary>X-Up-Mode — target layer mode: "root", "modal", "drawer", "popup", "cover". TODO Phase D</summary>
    public static string? UpMode(this HttpContext c)
        => throw new NotImplementedException("Phase D · 📖 https://unpoly.com/layer-terminology");

    /// <summary>X-Up-Fail-Mode — the layer mode used when the response fails. TODO Phase D</summary>
    public static string? UpFailMode(this HttpContext c)
        => throw new NotImplementedException("Phase D");

    /// <summary>
    /// X-Up-Origin-Mode — mode of the layer that ISSUED the request.
    /// Differs from the target mode when a new overlay is being opened. TODO Phase D
    /// </summary>
    public static string? UpOriginMode(this HttpContext c)
        => throw new NotImplementedException("Phase D");

    /// <summary>True when the request targets an overlay (mode != root). TODO Phase D</summary>
    public static bool IsUpOverlay(this HttpContext c)
        => throw new NotImplementedException("Phase D");

    /// <summary>X-Up-Context — the layer's JSON state object. Travels BOTH ways. TODO Phase D · 📖 /context</summary>
    public static T? UpContext<T>(this HttpContext c)
        => throw new NotImplementedException("Phase D · 📖 https://unpoly.com/context");

    /// <summary>X-Up-Fail-Context — the layer context used when the response fails. TODO Phase D</summary>
    public static T? UpFailContext<T>(this HttpContext c)
        => throw new NotImplementedException("Phase D");

    // X-Up-Reload-From-Time is deliberately absent. Unpoly deprecated it in favour of the
    // standard Last-Modified header, and supporting it would require the client to load
    // unpoly-migrate.js. See UpResponse.UpNotModified for the supported path.
    // 📖 https://unpoly.com/X-Up-Reload-From-Time
    //
    // X-Up-Clear-Cache is absent for the same kind of reason: it appears in no current guide.
    // Use UpExpireCache or UpEvictCache.
}
