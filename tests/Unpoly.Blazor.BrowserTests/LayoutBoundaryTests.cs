using Microsoft.Playwright;
using Xunit;

namespace Unpoly.Blazor.BrowserTests;

/// <summary>
/// UpLayout across the sample's two Blazor layouts: MainLayout (main = .content, with the
/// shop header, nav and footer) and CheckoutLayout (main = .checkout-main, none of that).
///
/// Every check marks the live window first. A full page load throws the marker away, so its
/// survival is what separates "Unpoly swapped body" from "the browser navigated".
/// </summary>
[Collection("unpoly")]
public class LayoutBoundaryTests(UnpolyFixture fx)
{
    const string Marker = "window.__layoutProbe";

    /// <summary>Clicks, and returns the response to the first request for <paramref name="path"/>.</summary>
    static Task<IResponse> ClickFor(Probe p, string selector, string path) =>
        p.Page.RunAndWaitForResponseAsync(
            () => p.Click(selector),
            r => r.Url == p.BaseUrl + path && r.Request.Method == "GET");

    /// <summary>What the page looks like right now, as one string an assertion can print.</summary>
    static async Task<string> Seen(Probe p) =>
        $"url={p.Page.Url} marker={await p.Js<string?>($"{Marker} ?? null")} " +
        $"nav={await p.Count(".site-nav")} header={await p.Count(".site-header")} " +
        $"footer={await p.Count(".site-footer")} content={await p.Count(".content")} " +
        $"checkout-main={await p.Count(".checkout-main")} checkout-header={await p.Count(".checkout-header")}";

    [Fact]
    public async Task A_link_into_another_layout_is_widened_to_body_and_drops_the_shop_chrome()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Exec($"{Marker} = 'kept'");

        var mark = p.Mark();
        var res = await ClickFor(p, "a.checkout-link", "/checkout");

        // The client asked for the shop's main: it only knows the page it is on.
        var sent = p.Since(mark, exactPath: "/checkout").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal(".content", sent!.Up("x-up-target"));

        // The server answered for the whole page instead.
        Assert.Equal("body", await res.HeaderValueAsync("x-up-target"));

        var seen = await Seen(p);
        Assert.True(p.Page.Url == p.BaseUrl + "/checkout", seen);
        Assert.True(seen.Contains("marker=kept"), "full page reload: " + seen);
        Assert.True(await p.Count(".site-nav") == 0, "shop nav survived: " + seen);
        Assert.True(await p.Count(".site-header") == 0, "shop header survived: " + seen);
        Assert.True(await p.Count(".content") == 0, "shop main survived: " + seen);
        Assert.True(await p.Count(".checkout-main") == 1, "no checkout main: " + seen);
        Assert.True(await p.Count(".checkout-header") == 1, "checkout chrome stripped: " + seen);
        Assert.Equal("Thanh toán", await p.Text(".checkout-main .page-head h2"));
    }

    [Fact]
    public async Task The_way_back_from_checkout_restores_the_shop_chrome()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/checkout");
        await p.Exec($"{Marker} = 'kept'");

        var mark = p.Mark();
        var res = await ClickFor(p, "a.back-to-shop", "/shop");

        // On /checkout the first main target present is .checkout-main.
        var sent = p.Since(mark, exactPath: "/shop").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal(".checkout-main", sent!.Up("x-up-target"));
        Assert.Equal("body", await res.HeaderValueAsync("x-up-target"));

        var seen = await Seen(p);
        Assert.True(p.Page.Url == p.BaseUrl + "/shop", seen);
        Assert.True(seen.Contains("marker=kept"), "full page reload: " + seen);
        // UpChrome rendered in full because the retarget ended fragment rendering.
        Assert.True(await p.Count(".site-nav") == 1, "shop nav missing: " + seen);
        Assert.True(await p.Count(".site-footer") == 1, "shop footer missing: " + seen);
        Assert.True(await p.Count(".content") == 1, "shop main missing: " + seen);
        Assert.True(await p.Count(".checkout-main") == 0, "checkout main survived: " + seen);
        Assert.Equal("Tất cả sản phẩm", await p.Text(".content .page-head h2"));
    }

    [Fact]
    public async Task A_link_inside_one_layout_still_swaps_only_the_main_target()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Exec($"{Marker} = 'kept'; document.querySelector('.site-nav').dataset.probe = 'kept'");

        var mark = p.Mark();
        var res = await ClickFor(p, ".site-nav a[href='/shop/dam']", "/shop/dam");

        var sent = p.Since(mark, exactPath: "/shop/dam").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal(".content", sent!.Up("x-up-target"));

        // UpLayout left this one alone: no retarget, and the chrome was not even rendered.
        var retarget = await res.HeaderValueAsync("x-up-target");
        var body = await res.TextAsync();
        Assert.True(retarget is null, $"X-Up-Target: {retarget} on a same-layout link");
        Assert.False(body.Contains("site-header"), $"chrome rendered into a {body.Length}-byte fragment response");

        var seen = await Seen(p);
        Assert.True(seen.Contains("marker=kept"), "full page reload: " + seen);
        Assert.Equal("kept", await p.Js<string?>("document.querySelector('.site-nav')?.dataset?.probe ?? null"));
        Assert.Equal("Đầm", await p.Text(".page-head h2"));
    }
}
