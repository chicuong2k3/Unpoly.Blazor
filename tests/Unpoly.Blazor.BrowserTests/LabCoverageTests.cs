using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class LayerMatrixTests(UnpolyFixture fx)
{
    [Fact]
    public async Task Layers_stack_arbitrarily_deep_and_each_knows_its_depth()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/layers");

        // Once an overlay is open, the same link exists on every layer. Name the one in the
        // topmost layer, or Playwright refuses an ambiguous selector.
        await p.Click(".open-deeper", settleMs: 1600);
        for (var i = 0; i < 2; i++)
            await p.Click("up-modal:last-of-type .open-deeper", settleMs: 1600);

        // Root plus three. Enough depth for closest, ancestor and subtree to differ.
        Assert.Equal(4, await p.Js<int>("up.layer.count"));
        Assert.Equal("Layer 3", await p.Text("up-modal:last-of-type .overlay-title"));
    }

    [Fact]
    public async Task An_overlay_can_be_opened_from_a_form_as_well_as_a_link()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/layers");
        await p.Click(".open-deeper", settleMs: 1600);

        await p.Click("up-modal form[up-layer] button[type=submit]", settleMs: 1800);

        Assert.True(await p.Exists("up-drawer"));
    }

    [Fact]
    public async Task An_overlay_closes_on_an_event_it_was_opened_with()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/layers");
        await p.Click(".open-deeper", settleMs: 1600);
        Assert.True(await p.Exists("up-modal"));

        // [up-accept-event=lab:done]: emitting it from inside closes the layer, no server
        // involved at all.
        await p.Click("up-modal button[onclick*='lab:done']", settleMs: 1600);

        Assert.False(await p.Exists("up-modal"));
    }

    [Fact]
    public async Task Accepting_and_dismissing_work_from_JavaScript_too()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/layers");
        await p.Click(".open-deeper", settleMs: 1600);

        await p.Click("up-modal button[onclick*='up.layer.accept']", settleMs: 1400);
        Assert.False(await p.Exists("up-modal"));

        await p.Click(".open-deeper", settleMs: 1600);
        await p.Click("up-modal button[onclick*='up.layer.dismiss']", settleMs: 1400);
        Assert.False(await p.Exists("up-modal"));
    }
}

[Collection("unpoly")]
public class SubinteractionTests(UnpolyFixture fx)
{
    [Fact]
    public async Task An_overlay_can_accept_with_a_value_without_touching_the_server()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/sub");
        await p.Click("a[up-on-accepted*='addCollectionOption']", settleMs: 1800);

        var mark = p.Mark();
        // [up-accept] closes with a value client-side; nothing is submitted.
        await p.Click("up-modal button[up-accept]", settleMs: 1600);

        Assert.False(await p.Exists("up-modal"));
        Assert.Empty(p.Since(mark).Where(r => r.Method == "POST"));

        // The accepted value became a selected <option> on the opener.
        Assert.True(await p.Js<bool>(
            "document.querySelector('.collection-select')?.selectedIndex > 0"));
    }

    [Fact]
    public async Task Awaiting_a_subinteraction_from_JavaScript_resolves_with_the_value()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/sub");

        await p.Exec("window.__asked = null;" +
                     " up.layer.ask({ url: '/lab/sub/pick', size: 'small' })" +
                     "   .then(v => window.__asked = v.slug, () => window.__asked = 'dismissed');");
        await p.Page.WaitForTimeoutAsync(1800);

        await p.Click("up-modal button[up-accept]", settleMs: 1600);

        Assert.Equal("spring-summer-26", await p.Js<string?>("window.__asked ?? null"));
    }
}

[Collection("unpoly")]
public class ScriptCoverageTests(UnpolyFixture fx)
{
    [Fact]
    public async Task All_three_ways_of_registering_a_destructor_run()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/script");

        // up.destroy(selector) takes one element; there are three probes.
        await p.Exec("window.__destroyed = [];" +
                     " document.querySelectorAll('.probe-destructor').forEach(e => up.destroy(e));");
        await p.Page.WaitForTimeoutAsync(800);

        // Returned function, returned array, and up.destructor() must all fire.
        var kinds = await p.Js<string>("JSON.stringify(window.__destroyed.sort())");
        Assert.Equal("[\"array\",\"register\",\"return\"]", kinds);
    }

    [Fact]
    public async Task A_compiler_receives_data_from_every_source()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/script");

        // [up-data] arrives parsed as the second argument; the compiler puts it on [title].
        Assert.Contains("warm", await p.Js<string?>("document.querySelector('.probe-data')?.title ?? null"));

        // up.data() reads the same thing from outside a compiler.
        Assert.Contains("warm", await p.Js<string>("JSON.stringify(up.data('.probe-data'))"));
    }

    [Fact]
    public async Task A_compiler_can_see_whether_the_pass_was_a_revalidation()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/p/dam-4");

        // The third compiler argument describes the render pass -- otherwise invisible.
        Assert.Equal("root", await p.Js<string?>("document.querySelector('[data-gallery]')?.dataset?.layerMode ?? null"));
        Assert.NotNull(await p.Js<string?>("document.querySelector('[data-gallery]')?.dataset?.revalidating ?? null"));
    }

    [Fact]
    public async Task A_new_asset_version_raises_up_assets_changed()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/script");

        await p.Exec("window.__assetsChanged = 0;");
        await p.Click("a[href='/lab/script?v=2']", settleMs: 2000);

        // Only assets in <head> are tracked, which is why the marker lives outside UpChrome.
        Assert.True(await p.Js<int>("window.__assetsChanged ?? 0") > 0);
    }
}

[Collection("unpoly")]
public class FragmentLifecycleTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_loaded_response_can_be_refused_before_it_is_rendered()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/fragment");

        var before = await p.Text(".page-head .count");
        await p.Click("a[href='/lab/fragment/refused']", settleMs: 1800);

        // The request happened; the render did not. up:fragment:loaded called preventDefault.
        Assert.NotEmpty(p.Since(0, contains: "/lab/fragment/refused"));
        Assert.Equal(before, await p.Text(".page-head .count"));
    }

    [Fact]
    public async Task Up_on_rendered_runs_and_a_failure_goes_to_the_fail_target()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/fragment");

        await p.Exec("window.__rendered = 0;");
        await p.Click("a[href='/lab/fragment/ok']", settleMs: 1800);
        Assert.True(await p.Js<int>("window.__rendered ?? 0") > 0);

        await p.Goto("/lab/fragment");
        await p.Click("a[href='/lab/fragment/boom']", settleMs: 1800);

        // A 500 renders into .result-box rather than breaking the page.
        Assert.Contains(500, p.StatusesFor("/lab/fragment/boom"));
        Assert.True(await p.Exists(".page-head"));
    }

    [Fact]
    public async Task Up_keep_preserves_a_live_element_across_a_swap()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/fragment");

        await p.Exec("document.querySelector('.kept-input').value = 'không được mất';");
        await p.Click("a[href='/lab/fragment/ok'][up-target='.content']", settleMs: 1800);

        // The element stayed attached, so its value survived a swap of its parent.
        Assert.Equal("không được mất", await p.Js<string?>("document.querySelector('.kept-input')?.value ?? null"));
    }

    [Fact]
    public async Task Local_content_and_templates_render_without_a_request()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab/fragment");

        var mark = p.Mark();
        await p.Click("a[up-content]", settleMs: 1200);
        Assert.Contains("tại chỗ", await p.Text(".result-box") ?? "");

        await p.Click("a[up-document='#lab-template']", settleMs: 1200);
        Assert.Contains("clone từ template", await p.Text(".result-box") ?? "");

        // Neither touched the network.
        Assert.Empty(p.Since(mark).Where(r => r.Url.Contains("/lab/fragment/")));
    }
}

[Collection("unpoly")]
public class MotionAndEventTests(UnpolyFixture fx)
{
    [Fact]
    public async Task A_custom_transition_is_registered_and_usable()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        Assert.True(await p.Js<bool>("typeof up.transition === 'function'"));
        Assert.True(await p.Exists("a[up-transition='lab-slide']"));

        await p.Click("a[up-transition='lab-slide']", settleMs: 2000);
        Assert.True(await p.Exists(".card"));
    }

    [Fact]
    public async Task Up_emit_raises_an_event_that_a_listener_sees()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/lab");

        await p.Exec("window.__pinged = 0;");

        // [up-emit] from HTML, then up.emit() from JavaScript: the same bus the server's
        // UpEmit writes to.
        await p.Click("button[up-emit]", settleMs: 600);
        await p.Click("button[onclick*='up.emit']", settleMs: 600);

        Assert.Equal(2, await p.Js<int>("window.__pinged ?? 0"));
    }
}
