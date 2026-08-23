using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class PreviewTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_preview_mutates_the_DOM_before_the_server_answers_and_is_reverted()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        await p.Exec("window.__previewRan = 0;");
        await p.Page.ClickAsync("a[up-preview='lab-skeleton']");

        // The route takes 1.2s, so the preview is observable while it is in flight.
        Assert.True(await p.Poll("!!document.querySelector('.lab-skeleton')", timeoutMs: 2000),
            "the skeleton never appeared before the response");

        await p.Page.WaitForTimeoutAsync(2000);

        // Unpoly reverts every preview once the response arrives -- no cleanup code needed.
        Assert.False(await p.Exists(".lab-skeleton"));
        Assert.True(await p.Js<int>("window.__previewRan ?? 0") > 0);
    }

    [Fact]
    public async Task Previews_chain_and_take_parameters()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        await p.Page.ClickAsync("a[up-preview*='lab-dim']");

        // Both previews ran: one inserted, the other set a style from its own parameter.
        Assert.True(await p.Poll("!!document.querySelector('.lab-skeleton')", timeoutMs: 2000));
        Assert.True(await p.Poll("document.querySelector('.content')?.style?.opacity === '0.2'",
            timeoutMs: 2000), "the parameterised preview did not apply its opacity");
    }

    [Fact]
    public async Task Optimistic_rendering_clones_a_template_before_the_server_replies()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/sub");

        Assert.True(await p.Exists("#optimistic-row"));

        await p.Page.ClickAsync("a[up-preview='lab-optimistic']");

        // The row is cloned from a <template> already in the response, so it appears without
        // a second request -- and Unpoly removes it when the real content arrives.
        Assert.True(await p.Poll("!!document.querySelector('.is-optimistic')", timeoutMs: 2000),
            "the optimistic row never appeared");

        await p.Page.WaitForTimeoutAsync(2000);
        Assert.False(await p.Exists(".is-optimistic"));
    }
}
