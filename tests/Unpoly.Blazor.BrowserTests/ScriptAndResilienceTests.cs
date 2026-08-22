using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class CompilerTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_compiler_runs_on_load_and_receives_its_up_data()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        Assert.Equal("true", await p.Js<string?>("document.querySelector('[data-gallery]')?.dataset?.galleryLive ?? null"));
        Assert.Equal(1, await p.Js<int>("window.Gallery.liveCount()"));

        // Read the value the widget was handed rather than timing how fast slides move --
        // a timing assertion tests the clock, and the first version of this check failed
        // for exactly that reason. 1500 is the widget's own default.
        Assert.Equal("900", await p.Js<string?>("document.querySelector('[data-gallery]')?.dataset?.galleryInterval ?? null"));
    }

    [Fact]
    public async Task The_widget_survives_repeated_swaps_and_the_destructor_stops_the_old_ones()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        for (var i = 0; i < 3; i++)
        {
            await p.Click(".site-nav a[href='/shop']");
            await p.Click(".card a", settleMs: 1600);
        }

        Assert.Equal("true", await p.Js<string?>("document.querySelector('[data-gallery]')?.dataset?.galleryLive ?? null"));

        // Without the returned destructor every swap leaves another timer running against
        // detached DOM. Nothing visible reveals it; the count does.
        Assert.Equal(1, await p.Js<int>("window.Gallery.liveCount()"));
    }

    [Fact]
    public async Task The_asset_marker_survives_fragment_responses()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Click(".card a", settleMs: 1600);

        // Assets are only tracked in <head>, so cutting the head had silently disabled
        // up:assets:changed. One marker outside UpChrome buys it back.
        Assert.True(await p.Exists("meta[up-asset]"));
    }
}

[Collection("unpoly")]
public class ScrollAndFocusTests(UnpolyFixture fx)
{
    [Fact]
    public async Task Navigating_scrolls_to_the_top_and_Back_restores_the_fragment()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");

        await p.Js<object?>("window.scrollTo(0, 900)");
        await p.Page.WaitForTimeoutAsync(400);
        var scrolled = await p.Js<int>("Math.round(window.scrollY)");

        await p.Click(".site-nav a[href='/shop/dam']", settleMs: 1800);
        Assert.True(await p.Js<int>("Math.round(window.scrollY)") < scrolled);

        await p.Js<object?>("history.back()");

        // Sample over time: restoration re-renders up.history.config.restoreTargets
        // (default ["body"]) and then settles.
        var seen = new List<int>();
        for (var i = 0; i < 12; i++)
        {
            await p.Page.WaitForTimeoutAsync(350);
            seen.Add(await p.Js<int>("Math.round(window.scrollY)"));
        }

        Assert.Equal("/shop", await p.Js<string>("location.pathname"));

        // Scroll is NOT restored by default. Recorded rather than asserted the other way:
        // the guide makes it opt-in via [up-scroll=restore], and nothing here opts in.
        Assert.Equal(0, seen.Max());
    }

    [Fact]
    public async Task Focus_lands_inside_the_new_fragment()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Click(".site-nav a[href='/shop/ao']", settleMs: 1800);

        Assert.Equal("in .content", await p.Js<string>(
            "(() => { const a = document.activeElement;" +
            " return a && a.closest('.content') ? 'in .content' : (a?.tagName || 'none'); })()"));
    }
}

[Collection("unpoly")]
public class WithoutJavaScriptTests(UnpolyFixture fx)
{
    public static TheoryData<string, string> Routes => new()
    {
        { "/", "card" },
        { "/shop", "card" },
        { "/p/dam-4", "detail" },
        { "/login", "form-wrap" },
        { "/p/dam-4/size", "sizes" },
        { "/size-guide", "size-table" },
    };

    /// <summary>
    /// Measured with a plain HTTP client, not a browser with scripting turned off: disabling
    /// script execution also stops the evaluation used to inspect the page, so the browser
    /// cannot report on itself. An HTTP client IS a browser without JavaScript.
    ///
    /// This is the property that makes Unpoly worth choosing here over a fragment-only
    /// approach, and the easiest one to lose without noticing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Every_route_renders_without_JavaScript(string path, string marker)
    {
        var res = await fx.Http.GetAsync(UnpolyFixture.BaseUrl + path);
        Assert.True(res.IsSuccessStatusCode, $"{path} answered {(int)res.StatusCode}");
        Assert.Contains(marker, await res.Content.ReadAsStringAsync());
    }
}
