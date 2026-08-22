using Jubin.Components;
using Unpoly.Blazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddUnpoly(o =>
{
    // Links and forms without an explicit [up-target] swap this region.
    o.MainTargets = [".content"];
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
