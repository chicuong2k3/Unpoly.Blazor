using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;

namespace Unpoly.Blazor;

/// <summary>
/// Settings for the generated Unpoly config, applied by <see cref="UnpolyHead"/>.
/// Most of Unpoly is configured in HTML attributes; only what the whole app shares lives here.
/// </summary>
public sealed class UnpolyOptions
{
    /// <summary>
    /// Selector for the main content region. Unpoly uses it when a link declares no [up-target].
    /// 📖 https://unpoly.com/targeting-fragments
    /// </summary>
    public string[] MainTargets { get; set; } = [".content"];

    /// <summary>
    /// Route every &lt;a href&gt; and &lt;form&gt; through Unpoly so [up-follow] does not have
    /// to be sprinkled everywhere. 📖 https://unpoly.com/handling-everything
    /// </summary>
    public bool HandleAllLinksAndForms { get; set; } = true;

    /// <summary>
    /// Follow every link on mousedown instead of click, which feels roughly 100ms faster.
    /// Off by default: it changes click semantics, so a link that shows a confirm dialog or
    /// that the user drags rather than clicks needs [up-instant=false].
    /// 📖 https://unpoly.com/handling-everything
    /// </summary>
    public bool InstantAllLinks { get; set; }

    /// <summary>
    /// Preload every link on hover. Off by default because it multiplies server load: on a
    /// product listing, sweeping the mouse across the grid fires a request per card.
    /// Only worth enabling once caching is understood — preloading fills the same cache that
    /// revalidation later refetches. 📖 https://unpoly.com/handling-everything
    /// </summary>
    public bool PreloadAllLinks { get; set; }

    /// <summary>Header carrying the antiforgery token for requests that are not form submissions.</summary>
    public string AntiforgeryHeaderName { get; set; } = "RequestVerificationToken";

    /// <summary>Raw JS appended after the generated config, to tune up.* without forking UnpolyHead.</summary>
    public string? ExtraScript { get; set; }
}

/// <summary>Registration for <see cref="UnpolyOptions"/> and the antiforgery header Unpoly sends.</summary>
public static class UnpolySetup
{
    /// <summary>
    /// Registers Unpoly's options and tells ASP.NET which header carries the antiforgery
    /// token, which it otherwise refuses to read from a header at all.
    /// </summary>
    public static IServiceCollection AddUnpoly(this IServiceCollection services, Action<UnpolyOptions>? configure = null)
    {
        var options = new UnpolyOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // ASP.NET only accepts the token from a header when HeaderName is set explicitly.
        services.Configure<AntiforgeryOptions>(a => a.HeaderName = options.AntiforgeryHeaderName);
        return services;
    }
}
