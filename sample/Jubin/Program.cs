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
// Marks every response as varying by Unpoly's request headers, because UpChrome
// branches the body on X-Up-Target. 📖 https://unpoly.com/optimizing-responses
app.UseUnpoly();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
