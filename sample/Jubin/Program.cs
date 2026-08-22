using Jubin.Components;
using Unpoly.Blazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddUnpoly(o =>
{
    // Links and forms without an explicit [up-target] swap this region.
    o.MainTargets = [".content"];

    o.ExtraScript = """
        // Customizing navigation defaults. Navigation applies a bundle of options that
        // up.render() does not; this overrides one of them for every navigating feature.
        // \📖 https://unpoly.com/handling-everything
        up.fragment.config.navigateOptions.transition = 'cross-fade';

        // Customizing failure detection. A 200 carrying X-Unauthorized is treated as a
        // failure, so it renders into the fail target instead of the success one.
        // \📖 https://unpoly.com/failed-responses
        let badStatus = up.network.config.fail;
        up.network.config.fail = (response) => badStatus(response) || response.header('X-Unauthorized');
        """;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Phase B observation aid: log every request so cache revalidation is visible.
// Click one link in the browser and count the lines. 📖 https://unpoly.com/caching
var hits = 0;
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Path.StartsWithSegments("/_content") && !ctx.Request.Path.StartsWithSegments("/_framework"))
    {
        var target = ctx.UpTarget() is { } t ? $" target={t}" : "";
        Console.WriteLine($"[{++hits,3}] {ctx.Request.Method} {ctx.Request.Path}{target}");
    }
    await next();
});

// Marks every response as varying by Unpoly's request headers, because UpChrome
// branches the body on X-Up-Target. 📖 https://unpoly.com/optimizing-responses
app.UseUnpoly();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
