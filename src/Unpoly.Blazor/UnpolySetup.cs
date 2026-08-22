using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;

namespace Unpoly.Blazor;

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

    /// <summary>Header carrying the antiforgery token for requests that are not form submissions.</summary>
    public string AntiforgeryHeaderName { get; set; } = "RequestVerificationToken";

    /// <summary>Raw JS appended after the generated config, to tune up.* without forking UnpolyHead.</summary>
    public string? ExtraScript { get; set; }
}

public static class UnpolySetup
{
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
