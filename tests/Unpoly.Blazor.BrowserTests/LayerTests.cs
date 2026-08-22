using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class LayerTests(UnpolyFixture fx)
{
    [Fact]
    public async Task An_overlay_opens_and_the_opener_stays_intact_behind_it()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);

        Assert.True(await p.Exists("up-modal"));

        // The product page is not replaced. That is what makes it a subinteraction rather
        // than a navigation.
        Assert.True(await p.Exists(".detail"));
    }

    [Fact]
    public async Task Accepting_closes_the_overlay_and_hands_the_value_to_the_opener()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);
        await p.Click("up-modal form.sizes button[value=M]", settleMs: 1800);

        Assert.False(await p.Exists("up-modal"));
        Assert.Equal("M", await p.Text(".chosen-size"));

        // [up-on-accepted] ran on the opener. It is evaluated as ONE expression, which is
        // why the callback is a named function -- a multi-line body silently does nothing.
        Assert.False(await p.Js<bool>("document.querySelector('.add-to-cart').disabled"));
    }

    [Fact]
    public async Task Dismissing_closes_the_overlay_without_a_value()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);

        await p.Js<object?>("document.querySelector('up-modal button[up-dismiss]').click()");
        await p.Page.WaitForTimeoutAsync(1200);

        Assert.False(await p.Exists("up-modal"));
        Assert.Equal("chưa chọn", await p.Text(".chosen-size"));
    }

    [Fact]
    public async Task The_server_can_open_a_drawer_from_a_link_that_never_asked_for_one()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[href*='serverOpens=1']", settleMs: 2000);

        // X-Up-Open-Layer overrides what the link requested.
        Assert.True(await p.Exists("up-drawer"));
    }

    [Fact]
    public async Task An_overlay_request_carries_mode_and_context_and_the_server_echoes_them()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        var mark = p.Mark();
        await p.Click("a[up-context]", settleMs: 1800);

        var sent = p.Since(mark, contains: "/size").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal("modal", sent!.Up("x-up-mode"));
        Assert.Contains("flavour", sent.Up("x-up-context") ?? "");

        Assert.True(await p.Js<bool>(
            "document.querySelector('up-modal')?.textContent?.includes('flavour') ?? false"));
    }

    [Fact]
    public async Task Overlays_stack_and_closing_the_top_reveals_the_one_below()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");
        await p.Click("a[up-layer]", settleMs: 1800);
        await p.Click("up-modal a[href='/size-guide']", settleMs: 2000);

        Assert.Equal(2, await p.Count("up-modal"));

        // Root plus two overlays. With only one open, root/parent/current/any all mean the
        // same thing, which is why ten sections of /layer-option sat unexercised.
        Assert.Equal(3, await p.Js<int>("up.layer.count"));
        Assert.True(await p.Js<bool>(
            "document.querySelector('up-modal:last-of-type')?.textContent?.includes('overlay thứ hai') ?? false"));

        await p.Js<object?>("document.querySelector('up-modal:last-of-type button[up-dismiss]').click()");
        await p.Page.WaitForTimeoutAsync(1400);

        Assert.Equal(1, await p.Count("up-modal"));
        Assert.True(await p.Exists("up-modal form.sizes"));
        Assert.True(await p.Exists(".detail"));
    }
}
