using Microsoft.Playwright;

namespace Unpoly.Blazor.BrowserTests;

public sealed record Sent(string Method, string Url, IReadOnlyDictionary<string, string> Headers)
{
    public string? Up(string header) =>
        Headers.TryGetValue(header.ToLowerInvariant(), out var v) ? v : null;
}

/// <summary>
/// A page plus a record of everything it put on the wire.
///
/// Requests are deduplicated by URL+method within a mark window rather than counted raw:
/// the Python harness this replaces once double-counted every request and produced a
/// "finding" that survived two wrong explanations.
/// </summary>
public sealed class Probe : IAsyncDisposable
{
    private readonly List<Sent> requests = [];
    private readonly List<(string Url, int Status)> responses = [];

    private Probe(IBrowserContext context, IPage page)
    {
        Context = context;
        Page = page;
    }

    public IBrowserContext Context { get; }
    public IPage Page { get; }

    public static async Task<Probe> Create(UnpolyFixture fx)
    {
        var context = await fx.Browser.NewContextAsync(new() { ViewportSize = new() { Width = 1360, Height = 900 } });
        var page = await context.NewPageAsync();
        var probe = new Probe(context, page);

        page.Request += (_, r) => probe.requests.Add(new Sent(r.Method, r.Url, r.Headers));
        page.Response += (_, r) => probe.responses.Add((r.Url, r.Status));

        return probe;
    }

    // ---------------------------------------------------------------- basics

    public async Task Goto(string path)
    {
        await Page.GotoAsync(UnpolyFixture.BaseUrl + path);
        await Page.WaitForTimeoutAsync(600);
    }

    /// <summary>Evaluates a single EXPRESSION and returns its value.</summary>
    public Task<T> Js<T>(string expression) => Page.EvaluateAsync<T>($"() => {expression}");

    /// <summary>
    /// Runs a block of STATEMENTS. Passing several statements to <see cref="Js{T}"/> is a
    /// syntax error -- an arrow function without braces holds one expression -- and it fails
    /// silently as "the listener never fired" rather than as a script error.
    /// </summary>
    public Task Exec(string statements) => Page.EvaluateAsync($"() => {{ {statements} }}");

    public async Task<string?> Text(string selector) =>
        await Js<string?>($"document.querySelector({Q(selector)})?.textContent?.trim() ?? null");

    public Task<bool> Exists(string selector) =>
        Js<bool>($"!!document.querySelector({Q(selector)})");

    public Task<int> Count(string selector) =>
        Js<int>($"document.querySelectorAll({Q(selector)}).length");

    private static string Q(string s) => System.Text.Json.JsonSerializer.Serialize(s);

    // ---------------------------------------------------------------- interaction

    public async Task Click(string selector, int settleMs = 1400)
    {
        await Page.ClickAsync(selector);
        await Page.WaitForTimeoutAsync(settleMs);
    }

    /// <summary>Hover and hold. [up-preload] needs more than up.link.config.preloadDelay (90ms).</summary>
    public async Task Hover(string selector, int holdMs = 800)
    {
        await Page.HoverAsync(selector);
        await Page.WaitForTimeoutAsync(holdMs);
    }

    /// <summary>
    /// Press without releasing. The only way to see [up-instant], which fires on mousedown:
    /// a normal click cannot distinguish it from an ordinary follow.
    /// </summary>
    public async Task PressOnly(string selector, int holdMs = 800)
    {
        // Scroll it into view first: a bounding box for an element below the fold gives
        // coordinates outside the viewport, and the press lands on nothing.
        var locator = Page.Locator(selector).First;
        await locator.ScrollIntoViewIfNeededAsync();

        var box = await locator.BoundingBoxAsync()
                  ?? throw new InvalidOperationException($"no box for {selector}");

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.WaitForTimeoutAsync(holdMs);
    }

    public Task Release() => Page.Mouse.UpAsync();

    /// <summary>
    /// True if the expression becomes true at any point. Needed for states that exist only
    /// while a request is in flight -- a settled page never shows them, so a single
    /// assertion afterwards would always pass for the wrong reason.
    /// </summary>
    public async Task<bool> Poll(string expression, int timeoutMs = 3000, int stepMs = 50)
    {
        for (var waited = 0; waited < timeoutMs; waited += stepMs)
        {
            if (await Js<bool>(expression)) return true;
            await Page.WaitForTimeoutAsync(stepMs);
        }
        return false;
    }

    // ---------------------------------------------------------------- traffic

    public int Mark() => requests.Count;

    public IReadOnlyList<Sent> Since(int mark, string? contains = null, string? exactPath = null) =>
        requests.Skip(mark)
                .Where(r => exactPath is null || r.Url == UnpolyFixture.BaseUrl + exactPath)
                .Where(r => contains is null || r.Url.Contains(contains, StringComparison.Ordinal))
                .ToList();

    public IReadOnlyList<int> StatusesFor(string contains) =>
        responses.Where(r => r.Url.Contains(contains, StringComparison.Ordinal))
                 .Select(r => r.Status).ToList();

    public async ValueTask DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();
    }
}
