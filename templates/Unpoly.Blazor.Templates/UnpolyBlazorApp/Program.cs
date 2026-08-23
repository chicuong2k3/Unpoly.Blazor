using Unpoly.Blazor;
using UnpolyBlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// Links and forms without an explicit [up-target] swap this region.
// 📖 https://unpoly.com/handling-everything
builder.Services.AddUnpoly(o => o.MainTargets = [".content"]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Sets Vary for Unpoly's request headers and empties 304 bodies. Must run before
// UseAntiforgery. 📖 https://unpoly.com/optimizing-responses
app.UseUnpoly();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
