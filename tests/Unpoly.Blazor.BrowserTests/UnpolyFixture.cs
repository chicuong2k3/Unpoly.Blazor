using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Unpoly.Blazor.BrowserTests;

/// <summary>
/// Starts the sample app and a browser once for the whole run.
///
/// The app is launched as a child process rather than hosted in-process: Playwright needs a
/// real socket, and WebApplicationFactory's TestServer has none.
/// </summary>
public sealed class UnpolyFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://localhost:5288";

    private Process? app;
    private IPlaywright? playwright;

    public IBrowser Browser { get; private set; } = default!;
    public HttpClient Http { get; } = new();

    public async Task InitializeAsync()
    {
        var root = FindRepoRoot();

        app = Process.Start(new ProcessStartInfo("dotnet",
            $"run --no-build --project \"{Path.Combine(root, "sample", "Jubin")}\" --urls {BaseUrl}")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("could not start the sample app");

        // Drain both pipes. Redirecting without reading fills the OS buffer and blocks the
        // child: the sample logs a line per request, so it froze a few dozen requests in and
        // every later navigation timed out.
        app.OutputDataReceived += (_, _) => { };
        app.ErrorDataReceived += (_, _) => { };
        app.BeginOutputReadLine();
        app.BeginErrorReadLine();

        await WaitForApp();

        playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new()
        {
            // PWDEBUG=1 or HEADED=1 to watch it run.
            Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
        });
    }

    private async Task WaitForApp()
    {
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var res = await Http.GetAsync(BaseUrl + "/");
                if (res.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(500);
        }

        throw new TimeoutException($"the sample app never answered on {BaseUrl}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Unpoly.Blazor.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        playwright?.Dispose();

        if (app is { HasExited: false })
        {
            app.Kill(entireProcessTree: true);
            app.WaitForExit(5000);
        }
        app?.Dispose();
        Http.Dispose();
    }
}

[CollectionDefinition("unpoly")]
public sealed class UnpolyCollection : ICollectionFixture<UnpolyFixture>;
