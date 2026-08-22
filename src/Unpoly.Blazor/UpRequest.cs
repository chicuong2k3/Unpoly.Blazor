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

        // A whole-page target anywhere in the list means we must still render everything.
        // "body:after" appends into <body>, so it is a whole-page target too.
        foreach (var t in targets)
            if (BaseTarget(t) is "body" or "html" or ":main" or ":layer")
                return false;

        return true;
    }

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
    /// X-Up-Validate — the client only wants validation feedback and MUST NOT persist anything.
    /// TODO Phase C
    /// </summary>
    public static bool IsUpValidating(this HttpContext c)
        => throw new NotImplementedException("Phase C · 📖 https://unpoly.com/validation");

    /// <summary>
    /// X-Up-Validate — the name of the field that changed.
    /// An empty string or ":unknown" means the whole form.
    /// TODO Phase C
    /// </summary>
    public static string? UpValidatingField(this HttpContext c)
        => throw new NotImplementedException("Phase C · 📖 https://unpoly.com/validation");

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

    // ─────────────────────────────────────────────────────────────
    // PHASE B · Conditional requests           📖 /conditional-responses
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// X-Up-Reload-From-Time — [up-poll] sends the timestamp of the content currently on screen.
    /// If nothing is newer than that, answer 304 with an empty body.
    /// TODO Phase B
    /// </summary>
    public static DateTimeOffset? UpReloadFromTime(this HttpContext c)
        => throw new NotImplementedException("Phase B · 📖 https://unpoly.com/conditional-responses");
}
