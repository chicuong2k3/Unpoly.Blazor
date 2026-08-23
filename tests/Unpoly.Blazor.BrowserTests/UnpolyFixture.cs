using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
    private Process? app;
    private IPlaywright? playwright;

    /// <summary>
    /// A free port chosen at startup. A fixed port fails on any machine where something else
    /// already holds it, or where a previous run has not finished dying yet -- which reads as
    /// "a bunch of tests fail" rather than as a port conflict.
    /// </summary>
    public string BaseUrl { get; private set; } = "http://localhost:5288";

    public IBrowser Browser { get; private set; } = default!;
    public HttpClient Http { get; } = new();

    public async Task InitializeAsync()
    {
        var root = FindRepoRoot();
        BaseUrl = $"http://localhost:{FreePort()}";

        app = Process.Start(new ProcessStartInfo("dotnet",
            $"run --project \"{Path.Combine(root, "sample", "Jubin")}\" --urls {BaseUrl}")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("could not start the sample app");

        // Drain both pipes. Redirecting without reading fills the OS buffer and blocks the
        // child: the sample logs a line per request, so it froze a few dozen requests in and
        // every later navigation timed out.
        var log = new System.Text.StringBuilder();
        app.OutputDataReceived += (_, e) => { if (e.Data is not null) log.AppendLine(e.Data); };
        app.ErrorDataReceived += (_, e) => { if (e.Data is not null) log.AppendLine(e.Data); };
        app.BeginOutputReadLine();
        app.BeginErrorReadLine();

        await WaitForApp(log);

        try
        {
            playwright = await Playwright.CreateAsync();
            Browser = await playwright.Chromium.LaunchAsync(new()
            {
                // HEADED=1 to watch it run.
                Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            });
        }
        catch (PlaywrightException ex)
        {
            throw new InvalidOperationException(
                "Chromium is not installed for Playwright, so every browser test would fail. Run:\n" +
                "  dotnet build\n" +
                "  pwsh tests/Unpoly.Blazor.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium\n\n" +
                ex.Message, ex);
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task WaitForApp(System.Text.StringBuilder log)
    {
        for (var i = 0; i < 90; i++)
        {
            if (app is { HasExited: true })
                throw new InvalidOperationException(
                    $"the sample app exited with code {app.ExitCode} before answering:\n{log}");

            try
            {
                var res = await Http.GetAsync(BaseUrl + "/");
                if (res.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }

            await Task.Delay(500);
        }

        throw new TimeoutException($"the sample app never answered on {BaseUrl}:\n{log}");
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
