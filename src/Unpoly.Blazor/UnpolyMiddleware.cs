using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Unpoly.Blazor;

public static class UnpolyMiddleware
{
    /// <summary>
    /// Declares that responses depend on Unpoly's request headers.
    ///
    /// Any endpoint may branch on X-Up-Target (see <see cref="UpChrome"/>), so the whole
    /// pipeline is marked rather than the individual components that happen to branch —
    /// a component that forgets would produce a cache poisoning bug that is invisible in
    /// development and only shows up behind a CDN.
    ///
    /// Register it before UseAntiforgery so the header is set for every response.
    /// 📖 https://unpoly.com/optimizing-responses
    /// </summary>
    public static IApplicationBuilder UseUnpoly(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            ctx.Response.OnStarting(() =>
            {
                // X-Up-Mode and X-Up-Context are here because a response may legitimately
                // differ by them -- different content for an overlay, or content that depends
                // on layer context. Without them two layers share one cache entry.
                // 📖 https://unpoly.com/context
                ctx.UpVary("X-Up-Target", "X-Up-Version", "X-Up-Mode", "X-Up-Context");
                return Task.CompletedTask;
            });
            await next();
        });
}
