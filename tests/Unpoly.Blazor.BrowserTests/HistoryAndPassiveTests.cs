using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class HistoryTests(UnpolyFixture fx)
{
    [Fact]
    public async Task X_Up_Title_sets_the_document_title()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");
        await p.Click("a[href='/lab/titled']", settleMs: 1800);

        Assert.Contains("server", (await p.Js<string>("document.title")).ToLowerInvariant());
    }

    [Fact]
    public async Task X_Up_Location_wins_over_the_URL_that_was_requested()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");
        await p.Click("a[href='/lab/relocated']", settleMs: 1800);

        Assert.Equal("/lab/somewhere-else", await p.Js<string>("location.pathname"));
    }

    [Fact]
    public async Task Up_history_false_swaps_without_touching_the_address_bar()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        var before = await p.Js<string>("location.pathname");
        await p.Click("a[up-history='false']", settleMs: 1800);

        Assert.Equal(before, await p.Js<string>("location.pathname"));
        Assert.True(await p.Exists(".card"));
    }

    [Fact]
    public async Task Infinite_scroll_appends_without_replacing_or_duplicating()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");

        Assert.Equal(12, await p.Count(".listing .card"));

        // [up-defer=reveal] is the trigger, so scroll rather than click -- doing both loads
        // the same page twice. It also keeps firing while the trigger stays in view, so one
        // scroll legitimately pulls several pages.
        await p.Exec("window.scrollTo(0, document.body.scrollHeight);");
        await p.Page.WaitForTimeoutAsync(3000);

        var grown = await p.Count(".listing .card");
        Assert.True(grown > 12, $"still {grown} cards, so it replaced instead of appending");
        Assert.Equal(30, grown);

        // The real risk of appending is appending the same slice twice.
        Assert.Equal(grown, await p.Js<int>(
            "new Set(Array.from(document.querySelectorAll('.listing .card a'))" +
            ".map(a => a.getAttribute('href'))).size"));

        // One target list did two jobs: .listing:after appended, .more was replaced.
        Assert.False(await p.Exists(".more a"));
        Assert.True(await p.Js<bool>("document.querySelector('.more')?.textContent?.includes('hết') ?? false"));
    }
}

[Collection("unpoly")]
public class PassiveUpdateTests(UnpolyFixture fx)
{
    private static int CountOf(string? text) =>
        int.TryParse(new string((text ?? "").Where(char.IsDigit).ToArray()), out var n) ? n : -1;

    [Fact]
    public async Task A_flash_an_event_and_a_hungry_badge_all_survive_the_redirect()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        var before = await p.Text(".cart-badge");

        await p.Click("a[up-layer]", settleMs: 1800);
        await p.Click("up-modal form.sizes button[value=M]", settleMs: 1800);

        await p.Exec("window.__cart = null;" +
                     " up.on('cart:changed', (e) => window.__cart = e.count);");
        await p.Click("form[method=post] button.add-to-cart", settleMs: 2000);

        Assert.NotNull(await p.Text("[up-flashes] .flash"));

        // The handler redirects, so all three are produced by the GET that follows. Without
        // POST-redirect-GET the badge renders the count from before the click: the layout's
        // render tree is built before the page's form handler runs.
        Assert.Equal(CountOf(before) + 1, await p.Js<int>("window.__cart ?? -1"));
        Assert.Equal(CountOf(before) + 1, CountOf(await p.Text(".cart-badge")));
    }

    [Fact]
    public async Task Polling_keeps_going_and_an_unchanged_poll_is_answered_304()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        var mark = p.Mark();
        await p.Page.WaitForTimeoutAsync(5000);

        Assert.NotEmpty(p.Since(mark, contains: "/p/dam-4"));

        // [up-poll] echoes the fragment's [up-etag] as If-None-Match. This is the Phase B
        // conditional-request work paying off: an unchanged poll costs an empty 304.
        Assert.Contains(304, p.StatusesFor("/p/dam-4"));
    }
}
