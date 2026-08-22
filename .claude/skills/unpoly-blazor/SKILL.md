---
name: unpoly-blazor
description: Use when working in a Blazor static SSR app that has the Unpoly.Blazor package or project reference — wiring Unpoly into a Blazor Web App, adding [up-target]/[up-nav]/[up-follow] to links and forms, reading or writing X-Up-* headers from a component, or debugging why a fragment swap does nothing, why page chrome vanished, or why an interactive component went dead after a swap.
---

# Unpoly.Blazor

Server-side adapter for [Unpoly 3](https://unpoly.com) on **Blazor static SSR**.

## The one idea

Unpoly requests **full pages** and extracts the target fragment **on the client**. It does
not need fragment-only endpoints. Every route stays a real, bookmarkable page.

So this library is not a rendering engine. It is a typed wrapper over Unpoly's
[server protocol](https://unpoly.com/up.protocol) plus `UpChrome`, which skips rendering
page chrome when the client asked for a fragment.

If you come from htmx: do **not** build fragment endpoints here. That instinct is wrong for
Unpoly and produces routes that break without JavaScript.

## Setup

Four things, all required.

```csharp
// Program.cs
builder.Services.AddUnpoly(o => o.MainTargets = [".content"]);
...
app.UseUnpoly();        // BEFORE UseAntiforgery. Sets Vary — skipping it is a cache bug.
app.UseAntiforgery();
```

```razor
@* App.razor *@
<head>
    <UpChrome>
        <meta ... /> <link rel="stylesheet" ... />
        <UnpolyHead />
    </UpChrome>

    <HeadOutlet />   @* MUST stay outside UpChrome, or <PageTitle> stops working *@
</head>
<body>
    <Routes />
    @* Do NOT add blazor.web.js — see mistakes below *@
</body>
```

```razor
@* MainLayout.razor *@
<UpChrome><NavMenu /></UpChrome>
<main class="content">@Body</main>   @* the target itself is never wrapped *@
<UpChrome><Footer /></UpChrome>
```

```razor
@* any page that needs the headers *@
@code {
    [CascadingParameter] public HttpContext Ctx { get; set; } = default!;
}
```

## What works today

The library is built phase by phase. **Only these exist**; everything else throws
`NotImplementedException` carrying the phase and the guide URL that explains it.

| Read the request | |
|---|---|
| `Ctx.IsUnpoly()` | request came from Unpoly (`X-Up-Version`) |
| `Ctx.UpTarget()` | the raw selector, **may be a list**: `".content, .flash"` |
| `Ctx.UpTargets()` | that list, split and trimmed |
| `Ctx.UpFailTarget()` / `UpFailTargets()` | selector used on failure — anything outside 2xx and 304 |
| `Ctx.IsUpValidating()` | validation-only request — **must not persist anything** |
| `Ctx.UpValidatingFields()` | the fields, **space** separated by Unpoly, batched into one request |
| `Ctx.IsUpValidatingUnknown()` | `:unknown` — validate the whole form |
| `Ctx.IsUpFragment()` | a specific fragment was asked for, so chrome can be skipped |
| `Ctx.WantsNothing()` | target is `:none` |

| Write the response | |
|---|---|
| `Ctx.UpRetarget(".sidebar")` | swap something else than requested; `":none"` swaps nothing |
| `Ctx.UpVary("X-Up-Target")` | merges into `Vary`; `UseUnpoly()` already does this |
| `Ctx.UpExpireCache("/shop/*")` | mark cached URLs stale — still rendered, then refetched |
| `Ctx.UpKeepCache()` | keep the cache a non-GET would otherwise clear |
| `Ctx.UpEvictCache("/cart")` | drop outright; stale content is never shown again |
| `Ctx.UpNotModified(etag, lastModified)` | publishes the version, returns true when the client is current (response becomes 304 — render nothing) |

| Components | |
|---|---|
| `<UnpolyHead />` | assets, config, CSRF wiring. Once, in `<head>` |
| `<UpChrome Provides=".nav">…</UpChrome>` | content rendered only on full-page requests. **`Provides` is required** if anything inside can be targeted from outside |
| `Ctx.UpWantsAny(".nav")` | did either branch ask for one of these selectors |

**Never existed, do not reach for them:** `UpClearCache` (in no current guide) and
`UpReloadFromTime` (deprecated by Unpoly — use `Last-Modified` through `UpNotModified`).

## Reach for HTML first

**Most of Unpoly needs no server code.** Before writing C#, ask whether the server has to
*decide* anything. If not, the answer is an attribute or a config line, and adding a C#
helper for it is pure overhead.

These have no server side at all — do not go looking for one:

| Want | Use | Not |
|---|---|---|
| A fallback when the target is missing | `[up-fallback=".content"]` | any C# |
| An optional target that must not fail the swap | `.unread-count:maybe` | a null check |
| Append or prepend instead of replace | `.tasks:after` / `.tasks:before` | two endpoints |
| Target the element that was clicked | `:origin` | passing an id to the server |
| Highlight the current nav item | `[up-nav]` → `.up-current` | comparing URLs in C# |
| Loading state | `.up-active`, `.up-loading` in CSS | a busy flag |
| A progress bar | on by default; `up.network.config.lateDelay` | any C# |
| Never show a spinner for this request | `[up-background]` | any C# |
| Poll a fragment | `[up-poll]` `[up-interval]` | a timer |
| Update a region from *any* response | `[up-hungry]` | targeting it everywhere |
| Keep an element across swaps | `[up-keep]` | re-rendering it |
| Confirm before following | `[up-confirm="Sure?"]` | a server round-trip |
| Change navigation defaults | `o.ExtraScript = "up.fragment.config.navigateOptions.transition = 'cross-fade'"` | a C# options class |
| Antiforgery on forms | Blazor's `EditForm` hidden input | anything extra |

## Client-side attributes worth knowing

None of these have a C# side, which is exactly why an agent reaches for one and invents a
helper instead. They are what you should be writing in the markup.

**Links**

| | |
|---|---|
| `[up-instant]` | follow on mousedown instead of click |
| `[up-preload]` | prefetch on hover |
| `[up-follow=false]` | opt this link out; a real full page load |
| `[up-confirm="Sure?"]` | ask before the request is made |
| `[up-cache=false]` | skip the 15-second cache for this link |
| `[up-background]` | never show the progress bar for this request |

**Forms**

| | |
|---|---|
| `[up-validate]` | revalidate the field's form group on blur |
| `[up-watch-event="input"]` `[up-watch-delay=400]` | revalidate while typing, debounced |
| `[up-disable]` | disable the form while it is in flight |
| `[up-autosubmit]` | submit as soon as a value changes |
| `[up-submit=false]` | opt this form out |
| `required`, `type=email` | HTML5 validation runs *before* any request |
| `<button name="intent" value="x">` | a second submit button; arrives as ordinary form data |

**Fragments**

| | |
|---|---|
| `[up-keep]` | survive swaps |
| `[up-hungry]` | update from *any* response, without being targeted |
| `[up-poll]` `[up-interval]` | reload on a timer |
| `[up-etag]` `[up-time]` | give one fragment its own version, so it can be answered 304 alone |
| `[up-nav]` | keep `.up-current` in sync |

**CSS Unpoly sets for you** — style these and loading state costs no JavaScript:
`.up-current`, `.up-active`, `.up-loading`.

**Config, through `o.ExtraScript`** — no C# API earns its place for a config string:

```csharp
o.ExtraScript = """
    up.fragment.config.navigateOptions.transition = 'cross-fade';

    // treat a 200 carrying a header as a failure
    let badStatus = up.network.config.fail;
    up.network.config.fail = (r) => badStatus(r) || r.header('X-Unauthorized');
    """;
```

## The `fail` prefix

Unpoly picks a different render option when a response fails, by prefixing it: `target` /
`failTarget`, `scroll` / `failScroll`, `onRendered` / `onFailRendered`. In HTML that is
`[up-fail-target]`, `[up-fail-scroll]`, `[up-on-fail-rendered]`.

Options consumed **before** the request (`url`, `method`, `confirm`) have no twin — nothing
knows yet that it will fail. Options used for both (`history`, `fallback`) take an optional
override such as `{ history: true, failHistory: false }`.

**Only three of them reach the server**, so do not go hunting for a C# helper for the rest:

| Client option | Header | Available here |
|---|---|---|
| `failTarget` | `X-Up-Fail-Target` | `Ctx.UpFailTarget()` / `UpFailTargets()` |
| `failLayer` / `failMode` | `X-Up-Fail-Mode` | not yet |
| `failContext` | `X-Up-Fail-Context` | not yet |
| everything else | — | client-only, by design |

Failure is any status **outside 2xx and 304**. Answer **422** for an invalid form so the
fail target is used.

## Typed helpers that do not exist yet

These are **not capability gaps** — the feature works today, the library just has no typed
wrapper. Calling the method throws; the HTML does the job now, and raw header access always
works via `Ctx.Request.Headers["X-Up-…"]`.

| Feature | Throws | Do this instead, today |
|---|---|---|
| Overlays / modals / drawers | `UpOpenLayer` `UpAcceptLayer` `UpDismissLayer` `UpMode` `UpContext<T>` | `[up-layer=new modal]`, `[up-size]`, `[up-accept-location="/things"]`, `[up-on-accepted]`, and `<button up-dismiss>` inside. A modal that opens a CRUD route, saves, closes and refreshes the list behind it needs **zero** C# |
| Form validation round-trip | `IsUpValidating` `UpValidatingField` | `[up-validate]` on the field. Server-side, read `X-Up-Validate` yourself and **do not persist** when it is present |
| Autosubmitting filters | — | `[up-autosubmit]` + `[up-watch-delay=300]` |
| Disable a form while in flight | — | `[up-disable]` |
| Document title on a fragment-only response | `UpTitle` | keep `<HeadOutlet />` outside `<UpChrome>` and `<PageTitle>` keeps working |
| Server-emitted events | `UpEmit` | write `X-Up-Events` directly if needed; the value is a JSON array of objects keyed by `"type"` |

If a task genuinely needs the server to decide — close this overlay, retarget that
response, emit this event — say the typed helper is not implemented yet rather than
inventing one.

## Mistakes to check first when something is broken

1. **`blazor.web.js` is still in `App.razor`.** With `interactivity=None` it only adds
   enhanced navigation and enhanced forms — exactly Unpoly's job. Both present means they
   fight over clicks and submits. Remove the script tag.

2. **A swap did nothing.** The selector must exist in **both** the response and the current
   page. Unpoly resolves it on both sides; that is by design, not a bug.

3. **Nav or footer vanished from a normal page load.** Something treated the request as a
   fragment. `X-Up-Target` is a *list*: `"body, .flash"` is still a whole-page request.

4. **An interactive component went silent after a swap.** A region rendered with
   `@rendermode InteractiveServer` or `WebAssembly` must never be inside a swapped fragment.
   Its Blazor markers only activate through Blazor's own updater, so Unpoly swapping it
   produces dead DOM with no error and no console message. Keep such regions outside the
   target, or mark them `[up-keep]`.

5. **`<PageTitle>` stopped updating.** `<HeadOutlet />` was moved inside `<UpChrome>`.

6. **The `.content` element is missing from a fragment response.** The target itself was
   wrapped in `<UpChrome>`. Wrap the chrome around it, never the target.

10. **A swap that targets the nav, a cart badge, or anything else inside the chrome does
    nothing.** `UpChrome` stripped it from the response, so the selector is absent and the
    swap finds nothing — no error, no console message. Declare it:
    `<UpChrome Provides=".site-nav, .cart-badge">`.

9. **An invalid form submission looks like a success.** The handler answered 200. Unpoly
   treats anything outside 2xx and 304 as failure — answer **422** so the fail target is
   used. And guard the handler with `IsUpValidating()`: a validation request asks for a
   fresh form state, not for the action to happen.

7. **A handler appears to run twice.** Unpoly caches GET responses for 15 seconds
   (`cacheExpireAge`) and revalidates: it renders the cached copy, then refetches and
   renders again. Keep GET handlers idempotent.

8. **Someone called `UpExpireCache` after a POST.** Usually redundant — Unpoly clears the
   entire cache after any non-GET by itself. The header is for expiring a *subset*, or from
   a GET. To stop the automatic clearing, use `UpKeepCache()`.

## CSRF

Handled. `UnpolyHead` feeds the ASP.NET antiforgery token into
`up.protocol.config.csrfToken`, and Unpoly sends it on every unsafe request by itself.
Do not add an `up:request:load` listener for this, and do not add a
`<meta name="csrf-token">` tag.

## Options

```csharp
builder.Services.AddUnpoly(o =>
{
    o.MainTargets = [".content"];      // used when a link declares no [up-target]
    o.HandleAllLinksAndForms = true;   // default: every <a href> and <form> goes through Unpoly
    o.InstantAllLinks = false;         // follow on mousedown; changes click semantics
    o.PreloadAllLinks = false;         // preload on hover; multiplies server load
    o.AntiforgeryHeaderName = "RequestVerificationToken";
    o.ExtraScript = null;              // raw JS appended after the generated config
});
```

Opt a single element out with `[up-follow=false]`, `[up-instant=false]`,
`[up-preload=false]`, `[up-submit=false]`.

## Where to look things up

Unpoly's own docs are the reference; this library only covers the server half.
Start at <https://unpoly.com/up.protocol> for headers, <https://unpoly.com/targeting-fragments>
for selectors, <https://unpoly.com/handling-everything> for the config knobs above.
