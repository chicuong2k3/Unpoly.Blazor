using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class CachingTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_fresh_cache_hit_makes_no_request_at_all()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Click(".site-nav a[href='/shop/ao']");

        var mark = p.Mark();
        await p.Click(".site-nav a[href='/shop/dam']");
        await p.Click(".site-nav a[href='/shop/ao']");

        // "One click, two requests" was wrong as a blanket claim. Within cacheExpireAge
        // (15s) a cached click costs nothing.
        Assert.Empty(p.Since(mark, exactPath: "/shop/ao"));
    }

    [Fact]
    public async Task An_expired_cache_hit_revalidates_exactly_once()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Click(".site-nav a[href='/shop/dam']");

        // Only an EXPIRED entry is rendered stale and then refetched. That refetch is the
        // second render pass the guide describes.
        await p.Page.WaitForTimeoutAsync(17_000);

        var mark = p.Mark();
        await p.Click(".site-nav a[href='/shop/dam']", settleMs: 1800);

        Assert.Single(p.Since(mark, exactPath: "/shop/dam"));
    }

    [Fact]
    public async Task A_mutation_is_reflected_rather_than_served_stale()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");

        // The version element, not "the first .meta" -- that selector matched something else
        // after the swap and the check passed while measuring nothing.
        var before = await p.Text(".version");
        await p.Click("form.refresh button[type=submit]", settleMs: 1800);
        var after = await p.Text(".version");

        Assert.NotNull(before);
        Assert.NotEqual(before, after);
        Assert.Contains("catalog-", after);
    }
}

[Collection("unpoly")]
public class PreloadTests(UnpolyFixture fx)
{
    [Fact]
    public async Task Preload_fires_on_a_hover_held_past_the_delay()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        var mark = p.Mark();
        await p.Hover("a[href='/lab/slow?case=preload']");

        // Exactly one. Each demo link points at a distinct slow URL: pointing them all at
        // one fast page let the 15s cache swallow the second hover, which read as broken.
        Assert.Single(p.Since(mark, exactPath: "/lab/slow?case=preload"));
    }

    [Fact]
    public async Task Preload_on_insert_fires_with_no_interaction_at_all()
    {
        await using var p = await Probe.Create(fx);
        var mark = p.Mark();
        await p.Goto("/lab");
        await p.Page.WaitForTimeoutAsync(1200);

        Assert.Single(p.Since(mark, exactPath: "/lab/slow?case=preload-insert"));
    }

    [Fact]
    public async Task Instant_fires_on_mousedown_before_the_button_is_released()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        var mark = p.Mark();
        await p.PressOnly("a[href='/lab/slow?case=instant']");

        Assert.Single(p.Since(mark, exactPath: "/lab/slow?case=instant"));
        await p.Release();
    }
}

[Collection("unpoly")]
public class ProgressBarTests(UnpolyFixture fx)
{
    [Fact]
    public async Task The_bar_appears_on_a_slow_response_and_leaves_when_it_settles()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        await p.Page.ClickAsync("a[href='/lab/slow?case=progress']");
        Assert.True(await p.Poll("!!document.querySelector('up-progress-bar')"),
            "up-progress-bar never appeared while the 1.2s route loaded");

        await p.Page.WaitForTimeoutAsync(1800);
        Assert.False(await p.Exists("up-progress-bar"));
    }

    [Fact]
    public async Task A_background_request_shows_no_bar_even_when_slow()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        await p.Page.ClickAsync("a[href='/lab/slow?case=background']");
        Assert.False(await p.Poll("!!document.querySelector('up-progress-bar')", timeoutMs: 2000));
    }
}
