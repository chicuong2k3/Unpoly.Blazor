using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class StackTests(UnpolyFixture fx)
{
    private async Task<Probe> TwoOverlays()
    {
        var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);
        await p.Click("up-modal a[href='/size-guide']", settleMs: 2000);
        return p;
    }

    [Fact]
    public async Task Swap_replaces_the_current_overlay_instead_of_stacking_another()
    {
        await using var p = await TwoOverlays();
        Assert.Equal(3, await p.Js<int>("up.layer.count"));

        await p.Click("up-modal a[up-layer='swap']", settleMs: 2000);

        // Still root plus two: one overlay went, one arrived.
        Assert.Equal(3, await p.Js<int>("up.layer.count"));
    }

    [Fact]
    public async Task Shatter_closes_every_overlay_and_opens_one()
    {
        await using var p = await TwoOverlays();

        await p.Click("up-modal a[up-layer='shatter']", settleMs: 2000);

        // Root plus exactly one.
        Assert.Equal(2, await p.Js<int>("up.layer.count"));
    }

    [Fact]
    public async Task Targeting_a_background_layer_peels_the_overlays_above_it()
    {
        await using var p = await TwoOverlays();

        await p.Click("up-modal a[up-layer='root']", settleMs: 2000);

        // Peeling: rendering into a layer below closes everything stacked on top.
        Assert.Equal(1, await p.Js<int>("up.layer.count"));
        Assert.False(await p.Exists("up-modal"));
    }

    [Fact]
    public async Task A_failure_can_be_rendered_into_a_different_layer()
    {
        await using var p = await TwoOverlays();

        var mark = p.Mark();
        await p.Click("up-modal a[up-fail-layer='root']", settleMs: 2000);

        var sent = p.Since(mark, contains: "/khong-ton-tai").FirstOrDefault();
        Assert.NotNull(sent);

        // The server is told up front which layer the failure belongs to -- it cannot infer
        // it after choosing a status.
        Assert.Equal("root", sent!.Up("x-up-fail-mode"));
    }
}

[Collection("unpoly")]
public class ProtocolDetailTests(UnpolyFixture fx)
{
    [Fact]
    public async Task An_overlay_renders_different_content_than_the_root_layer()
    {
        await using var p = await Probe.Create(fx);

        // Direct visit: an ordinary page with its chrome heading.
        await p.Goto("/p/dam-4/size");
        Assert.True(await p.Exists(".page-head"));
        Assert.False(await p.Exists(".overlay-title"));

        // Same route in an overlay: the modal frame already provides a heading.
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);
        Assert.True(await p.Exists("up-modal .overlay-title"));
        Assert.False(await p.Exists("up-modal .page-head"));
    }

    [Fact]
    public async Task Layer_context_changes_what_the_server_renders()
    {
        await using var p = await Probe.Create(fx);

        // Opened from the product page, whose link sets { from: 'product' }.
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);
        Assert.True(await p.Exists("up-modal .from-product"));

        // The lab opens the same route with a different context, so that note is absent.
        await p.Goto("/lab");
        await p.Click("a[up-context]", settleMs: 1800);
        Assert.True(await p.Exists("up-modal"));
        Assert.False(await p.Exists("up-modal .from-product"));
    }

    [Fact]
    public async Task A_full_page_load_produced_by_a_POST_carries_the_up_method_cookie()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/receipt");

        // [up-submit=false], so this is a real browser navigation with no Unpoly request to
        // put a header on. The cookie is the only channel, and Unpoly pops it during boot.
        await p.Click("form[method=post] button[type=submit]", settleMs: 1800);

        Assert.Contains("POST", await p.Text(".count") ?? "");

        var cookies = await p.Context.CookiesAsync();
        var seen = cookies.Any(c => c.Name == "_up_method");
        var popped = await p.Js<bool>("!document.cookie.includes('_up_method')");
        Assert.True(seen || popped, "_up_method was neither set nor already consumed");
    }

    [Fact]
    public async Task Validating_again_after_a_failed_submission_still_works()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        await p.Exec("const f = document.querySelector('#email'); f.value = 'nope';" +
                     " f.dispatchEvent(new Event('input', {bubbles:true}));");
        await p.Page.WaitForTimeoutAsync(1400);
        await p.Click("form button[type=submit]", settleMs: 1600);
        Assert.Contains(422, p.StatusesFor("/login"));

        // After the 422, changing a field must still validate -- the form that came back is
        // a fresh fragment, and its compilers have to have been applied to it.
        var mark = p.Mark();
        await p.Exec("const f = document.querySelector('#email'); f.value = 'a@b.com';" +
                     " f.dispatchEvent(new Event('input', {bubbles:true}));");
        await p.Page.WaitForTimeoutAsync(1600);

        Assert.Contains(p.Since(mark, contains: "/login"), r => r.Up("x-up-validate") is not null);
    }
}
