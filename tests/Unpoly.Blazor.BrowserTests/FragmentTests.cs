using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class FragmentTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_link_swaps_a_fragment_without_reloading_the_page()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");

        // Mark the live nav element. If it survives, no full page load happened.
        await p.Js<string>("document.querySelector('.site-nav').dataset.probe = 'kept'");

        var mark = p.Mark();
        await p.Click(".site-nav a[href='/shop/dam']");

        var sent = p.Since(mark, contains: "/shop/dam").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal(".content", sent!.Up("x-up-target"));

        Assert.Equal("Đầm", await p.Text(".page-head h2"));
        Assert.Equal("kept", await p.Js<string?>("document.querySelector('.site-nav')?.dataset?.probe ?? null"));
    }

    [Fact]
    public async Task Up_nav_moves_up_current_without_the_nav_being_swapped()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");
        await p.Click(".site-nav a[href='/shop/dam']");

        Assert.Equal("/shop/dam",
            await p.Js<string?>("document.querySelector('.site-nav a.up-current')?.getAttribute('href') ?? null"));
    }

    [Fact]
    public async Task A_chrome_selector_reaches_the_server_and_UpChrome_Provides_keeps_it_alive()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        var mark = p.Mark();
        await p.Click("a[up-target='.content, .site-nav']");

        var sent = p.Since(mark, contains: "/shop").FirstOrDefault();
        Assert.NotNull(sent);
        Assert.Equal(".content, .site-nav", sent!.Up("x-up-target"));

        // Without Provides the chrome is stripped and the swap silently finds nothing.
        Assert.True(await p.Exists(".site-nav"));
    }

    [Fact]
    public async Task Targeting_none_answers_204_and_leaves_the_page_alone()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        // By attribute: the product page grew a second form, and form[method=post] silently
        // started matching the wrong one.
        await p.Click("form[up-target=':none'] button[type=submit]");

        Assert.Contains(204, p.StatusesFor("/p/dam-4"));
        Assert.True(await p.Exists(".detail"));
    }
}
