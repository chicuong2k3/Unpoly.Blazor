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
                ctx.UpVary("X-Up-Target", "X-Up-Version");
                return Task.CompletedTask;
            });
            await next();
        });
}
