using Xunit;

namespace Unpoly.Blazor.BrowserTests;

[Collection("unpoly")]
public class FormTests(UnpolyFixture fx)
{
    private const string TypeEmail =
        "(() => { const f = document.querySelector('#email'); f.value = 'khong-phai-email';" +
        " f.dispatchEvent(new Event('input', {bubbles:true})); })()";

    [Fact]
    public async Task Validating_a_field_sends_X_Up_Validate_and_shows_the_error_without_submitting()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        var mark = p.Mark();
        await p.Exec(TypeEmail + ";");
        await p.Page.WaitForTimeoutAsync(1600);

        var validating = p.Since(mark, contains: "/login")
                          .FirstOrDefault(r => r.Up("x-up-validate") is not null);
        Assert.NotNull(validating);
        Assert.Equal("Model.Email", validating!.Up("x-up-validate"));

        Assert.NotNull(await p.Text(".validation-message"));
    }

    [Fact]
    public async Task An_invalid_submit_answers_422()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        await p.Exec(TypeEmail + ";");
        await p.Js<object?>("(() => { const w = document.querySelector('#password'); w.value = '1';" +
                            " w.dispatchEvent(new Event('input', {bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(1200);
        await p.Click("form button[type=submit]");

        // Unpoly treats anything outside 2xx and 304 as failure. Answering 200 would make an
        // invalid form look like a success.
        Assert.Contains(422, p.StatusesFor("/login"));
    }

    [Fact]
    public async Task A_validating_request_does_not_perform_the_action()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        // VALID credentials, so only the guard stops it. Proven by an absence: the success
        // state never appears. Checking for an error message would show nothing either way.
        await p.Js<object?>(
            "(() => { const e=document.querySelector('#email'); e.value='a@b.com';" +
            " e.dispatchEvent(new Event('input',{bubbles:true}));" +
            " const w=document.querySelector('#password'); w.value='secret123';" +
            " w.dispatchEvent(new Event('input',{bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(1800);

        Assert.False(await p.Js<bool>("document.body.textContent.includes('thành công')"));
    }

    [Fact]
    public async Task Up_disable_disables_the_form_in_flight_and_it_recovers()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        await p.Js<object?>(
            "(() => { const e=document.querySelector('#email'); e.value='a@b.com';" +
            " e.dispatchEvent(new Event('input',{bubbles:true}));" +
            " const w=document.querySelector('#password'); w.value='secret123';" +
            " w.dispatchEvent(new Event('input',{bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(1200);

        await p.Page.ClickAsync("form button[type=submit]");
        Assert.True(await p.Poll(
            "(() => { const f=document.querySelector('form');" +
            " return !!f && (f.matches('[aria-busy=true]')" +
            " || !!f.querySelector('input:disabled, button:disabled')); })()"),
            "the form was never seen disabled mid-submit");

        await p.Page.WaitForTimeoutAsync(1500);
        Assert.True(await p.Poll("!document.querySelector('form input:disabled')", timeoutMs: 2000));
    }

    [Fact]
    public async Task A_validating_request_re_renders_a_dependent_fragment()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/login");

        var before = await p.Text(".strength strong");

        // `change`, not `input`: [up-validate] watches change unless a field opts into
        // [up-watch-event=input], and only the email field does.
        await p.Js<object?>("(() => { const w = document.querySelector('#password'); w.value = 'abc';" +
                            " w.dispatchEvent(new Event('change', {bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(1800);
        var weak = await p.Text(".strength strong");
        Assert.NotEqual(before, weak);

        await p.Js<object?>("(() => { const w = document.querySelector('#password'); w.value = 'abcdefghijk';" +
                            " w.dispatchEvent(new Event('change', {bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(1800);

        // It reflects the current value, not merely "there was an error".
        Assert.NotEqual(weak, await p.Text(".strength strong"));
    }

    [Fact]
    public async Task Autosubmit_filters_on_change_and_watch_delay_collapses_a_burst()
    {
        await using var p = await Probe.Create(fx);
        await p.Goto("/shop");

        var before = await p.Count(".card");
        var mark = p.Mark();
        await p.Js<object?>("(() => { const s=document.querySelector('select[name=maxPrice]');" +
                            " s.value='400000'; s.dispatchEvent(new Event('change',{bubbles:true})); })()");
        await p.Page.WaitForTimeoutAsync(2000);

        Assert.NotEmpty(p.Since(mark, contains: "/shop"));
        Assert.True(await p.Count(".card") < before);

        // Three rapid changes landing on a value never requested before, so a cache hit
        // cannot masquerade as debouncing: 0 would prove nothing, 3 would mean no debounce.
        mark = p.Mark();
        await p.Js<object?>("(() => { const s=document.querySelector('select[name=maxPrice]');" +
                            " for (const v of ['600000','400000','900000']) {" +
                            "   s.value=v; s.dispatchEvent(new Event('change',{bubbles:true})); } })()");
        await p.Page.WaitForTimeoutAsync(2500);

        Assert.Single(p.Since(mark, contains: "/shop"));
        Assert.Equal("900000", await p.Js<string?>("document.querySelector('select[name=maxPrice]')?.value ?? null"));
    }
}
