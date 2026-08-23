---
name: unpoly-blazor
description: Use when working in a Blazor static SSR app that has the Unpoly.Blazor package or project reference — wiring Unpoly into a Blazor Web App, adding [up-target]/[up-nav]/[up-follow] to links and forms, reading or writing X-Up-* headers from a component, or debugging why a fragment swap does nothing, why page chrome vanished, why a poll returns 500, or why an interactive component went dead after a swap.
---

# Unpoly.Blazor

Server-side adapter for [Unpoly 3](https://unpoly.com) on **Blazor static SSR**.

Every API below has a runnable example. Copy the example rather than inferring the shape from
the signature — several of these methods carry a *contract* the signature cannot express, and
that is where the real bugs come from.

> **Writing markup? Read `ATTRIBUTES.md` next to this file.** It goes concept by concept —
> targeting, link behaviour, forms, feedback, caching, passive updates, overlays, history,
> animation, compilers, asset changes — saying *when* you reach for each Unpoly attribute,
> which one, a Razor snippet, and what goes wrong. This file is the C# API and its
> contracts; that file is the markup half, and most of Unpoly lives there.

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
app.UseUnpoly();        // BEFORE UseAntiforgery. Sets Vary, and empties 304 bodies.
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
<UpChrome Provides=".site-nav, .cart-badge"><NavMenu /></UpChrome>
<main class="content">@Body</main>   @* the target itself is never wrapped *@
<UpChrome><Footer /></UpChrome>
```

```razor
@* any page that needs the headers *@
@code {
    [CascadingParameter] public HttpContext Ctx { get; set; } = default!;
}
```

## Reading the request

### `IsUnpoly()` — did this come from Unpoly at all

```razor
@code {
    protected override void OnParametersSet()
    {
        // False for a bookmark, a bot, or a browser with JS off. Never let a route REQUIRE
        // this to be true -- that is exactly how a page stops working without JavaScript.
        if (Ctx.IsUnpoly()) Ctx.UpEmit("page:viewed", new { path = Ctx.Request.Path.Value });
    }
}
```

### `UpTarget()` / `UpTargets()` — what the client asked for

```csharp
Ctx.UpTarget();    // ".content, .flash"  -- RAW, and often a LIST
Ctx.UpTargets();   // [".content", ".flash"]
```

Comparing the raw string is the classic bug: `UpTarget() == ".content"` is false when the
client sent `".content, .flash"`. Use `UpTargets()` or `UpWantsAny()`.

### `UpWantsAny(selectors)` — did either branch ask for one of these

```razor
@* Build the expensive sidebar only when someone actually wants it. *@
@if (!Ctx.IsUpFragment() || Ctx.UpWantsAny(".sidebar, .filters"))
{
    <Sidebar Facets="Search.Facets()" />
}
```

### `IsUpFragment()` — is this a partial request

True only when a *specific* fragment was asked for. A target of `body`, `html`, `:main` or
`:layer` is a whole-page request and returns false. `UpChrome` uses it; you rarely call it.

### `WantsNothing()` — target is `:none`

```razor
@code {
    async Task Ping()
    {
        await Analytics.RecordAsync();

        // The client wants no content back. Answer 204 and render nothing.
        if (Ctx.WantsNothing()) Ctx.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
```

### `UpFailTarget()` / `UpFailTargets()` — where a failure will land

Failure is any status **outside 2xx and 304**. Read it when you need to know where an error
will be rendered; usually you just answer 422 and let Unpoly do the routing.

### `IsUpValidating()` / `UpValidatingFields()` / `IsUpValidatingUnknown()`

**The contract: a validating request must not persist anything.** It asks "what would this
form look like if I submitted it", nothing more.

```razor
@code {
    async Task Submit()
    {
        Errors = Validate(Model);

        // MUST come before any write. Unpoly fires this on blur and while typing; without
        // the guard, every keystroke would create an order.
        if (Ctx.IsUpValidating())
        {
            // SPACE separated, and batched: one request may carry several fields.
            // [":unknown"] means "validate the whole form".
            var fields = Ctx.UpValidatingFields();
            if (!Ctx.IsUpValidatingUnknown())
                Errors = Errors.Where(e => fields.Contains(e.Field)).ToList();
            return;
        }

        if (Errors.Count > 0)
        {
            // 422, not 200. A 200 makes Unpoly treat the invalid form as a success and swap
            // the ordinary target.
            Ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return;
        }

        await Orders.PlaceAsync(Model);
    }
}
```

### `UpMode()` / `UpFailMode()` / `UpOriginMode()` / `IsUpOverlay()`

```razor
@* One route, two presentations. Bookmarking /size-guide still gives a full page. *@
@if (!Ctx.IsUpOverlay())
{
    <h1>Size guide</h1>
    <BackLink />
}
<table class="size-table">...</table>

@code {
    // UpMode()       -> "modal" | "drawer" | "popup" | "cover" | "root"
    // UpOriginMode() -> the layer that ASKED, which may differ from the one being rendered
}
```

### `UpContext<T>()` / `UpFailContext<T>()` — the layer's own JSON state

```razor
@code {
    record LayerCtx(string From, int? ProductId);

    protected override void OnParametersSet()
    {
        // Malformed JSON returns null instead of throwing -- the context is client-supplied.
        var layer = Ctx.UpContext<LayerCtx>();
        ShowBuyButton = layer?.From != "admin";
    }
}
```

Set it from HTML with `up-context='{ "from": "product" }'`, change it from the server with
`UpSetContext`. **If a response depends on context it must be in `Vary`** — `UseUnpoly()`
already lists `X-Up-Context`.

## Writing the response

### `UpRetarget(selector)` — swap something other than what was asked for

```razor
@code {
    async Task AddToCart()
    {
        if (!await Stock.ReserveAsync(Id))
        {
            // The link wanted .cart-badge; it gets the error banner instead.
            Ctx.UpRetarget(".flash");
            Flash = "Hết hàng";
        }
    }
}
```

`Ctx.UpRetarget(":none")` swaps nothing at all.

### `UpNotModified(etag, lastModified)` — conditional requests

**The contract, and the one that bites hardest:** it returns true when the client is already
current, sets the status to 304, and a 304 **must carry no body**.

```razor
@* Guard the whole markup. Not for the bytes -- UseUnpoly() drops those for you -- but to
   skip the render and the queries behind it. *@
@if (!NotModified)
{
    <ul class="listing">
        @foreach (var p in Items) { <ProductCard Item="p" /> }
    </ul>
}

@code {
    bool NotModified;

    protected override void OnParametersSet()
    {
        // Publishes ETag / Last-Modified, and reports whether the client already has it.
        NotModified = Ctx.UpNotModified(Catalog.ETag, Catalog.LastModified);
        if (NotModified) return;      // <- skip the expensive part

        Items = Catalog.Query(Category);
    }
}
```

Pair it with `[up-etag]` on the fragment so a poll costs an empty 304 instead of a render:

```razor
<p class="stock" up-poll up-interval="4000" up-etag="@Cart.ETag">@Cart.Count món</p>
```

Only GET and HEAD are ever answered 304. On a POST, `If-None-Match` means optimistic
concurrency rather than caching, so the method returns false and leaves the status alone —
otherwise the form submission would silently do nothing.

### `UpExpireCache` / `UpEvictCache` / `UpKeepCache` — the client-side cache

```csharp
// EXPIRE: cached copies are still shown, then refetched behind the user's back.
Ctx.UpExpireCache("/shop/*");

// EVICT: dropped outright. Stale content is never shown again -- for when showing the old
// value would be wrong, not merely old (a price, a permission, a balance).
Ctx.UpEvictCache("/cart");

// KEEP: Unpoly clears the WHOLE cache after any non-GET by itself. This opts out.
Ctx.UpKeepCache();
```

The argument is a [URL pattern](https://unpoly.com/url-patterns), not a glob: `"/shop/*"`
any segment, `"/orders/$id"` a capture, `"/a /b"` alternatives, `"-/admin/*"` an exclusion.
Calling `UpExpireCache()` after a POST is usually redundant — Unpoly already cleared
everything.

### `UpTitle(title)` — the document title on a fragment response

```csharp
Ctx.UpTitle($"{product.Name} — JUBIN");   // quoting and escaping are handled for you
```

Usually unnecessary: keep `<HeadOutlet />` outside `<UpChrome>` and `<PageTitle>` keeps
working on fragment responses by itself.

### `UpLocation(url)` / `UpMethod(m)` / `UpMethodCookie()`

```razor
@code {
    void Search()
    {
        // What Unpoly should record in the address bar, when it differs from the real URL.
        Ctx.UpLocation($"/shop?q={Uri.EscapeDataString(Query)}");
    }

    async Task Checkout()
    {
        await Orders.PlaceAsync();

        // A full page load produced by a non-GET. Without it, Unpoly records the wrong
        // method for the history entry and Back re-issues it as a GET.
        Ctx.UpMethodCookie();
        Nav.NavigateTo("/receipt");
    }
}
```

### `UpEmit(type, props)` — raise a client-side event from the server

```csharp
Ctx.UpEmit("cart:changed", new { count = Cart.Count });
Ctx.UpEmit("flash:show",   new { text = "Đã thêm vào giỏ" });   // accumulates; both fire
```

```js
up.on('cart:changed', (event) => {
  document.querySelector('.cart-badge').textContent = event.count
})
```

### Layers: `UpOpenLayer` / `UpAcceptLayer` / `UpDismissLayer` / `UpSetContext`

```razor
@code {
    void Pick(string size)
    {
        // ACCEPT: the sub-task finished. The opener's [up-on-accepted] receives this as `value`.
        Ctx.UpAcceptLayer(new { size, label = $"Size {size}" });
    }

    void Cancel()
    {
        // DISMISS: the user backed out. NOT interchangeable with accept -- the opener has
        // two separate callbacks, and backing out must not look like a result.
        Ctx.UpDismissLayer("changed mind");
    }

    protected override void OnParametersSet()
    {
        // The SERVER decides this response opens in an overlay, even though the link asked
        // for an ordinary swap.
        if (Ctx.Request.Query.ContainsKey("serverOpens"))
            Ctx.UpOpenLayer(new { mode = "drawer", size = "large" });

        // Hand the layer a changed context, readable by later requests from that layer.
        Ctx.UpSetContext(new { from = "product", productId = Id });
    }
}
```

The opening side needs no C# at all:

```html
<a href="/products/5/size" up-layer="new modal" up-size="small"
   up-context='{ "from": "product" }'
   up-on-accepted="onChosen(value)"
   up-on-dismissed="console.log('backed out')">Choose size</a>
```

The point of an overlay is what stays *behind* it: the opener keeps its scroll, its state and
its unfinished input. Render a real route into it, and it stays bookmarkable when opened
directly — use `IsUpOverlay()` to drop chrome the modal frame already provides.

### `UpVary(...)` — declare what the response depends on

`UseUnpoly()` already sets `X-Up-Target, X-Up-Version, X-Up-Mode, X-Up-Context` on every
response. Call it directly only for a header of your own:

```csharp
Ctx.UpVary("X-Tenant");   // merges into Vary, never overwrites it
```

## Components

### `<UpChrome>` — content rendered only on full-page requests

```razor
<UpChrome Provides=".site-nav, .cart-badge">
    <nav class="site-nav">...<span class="cart-badge">@Cart.Count</span></nav>
</UpChrome>
```

`ChildContent` is never *invoked* on a fragment request, so queries inside it never run —
that, not the byte count, is where the saving comes from.

**`Provides` is required if anything inside can be targeted from outside.** Without it a link
that targets `.cart-badge` gets a response the chrome was stripped from, the selector is
absent, and the swap silently does nothing.

### `<UnpolyHead />` — assets, config, CSRF

Once, inside `<head>`, inside `<UpChrome>`.

## Reach for HTML first

**Most of Unpoly needs no server code.** Before writing C#, ask whether the server has to
*decide* anything. If not, the answer is an attribute or a config line.

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
| Antiforgery on forms | Blazor's `<AntiforgeryToken />` | anything extra |

### Attributes with no C# side — which is exactly why an agent invents a helper for them

**Links:** `[up-instant]` follow on mousedown · `[up-preload]` prefetch on hover ·
`[up-follow=false]` opt out · `[up-confirm]` · `[up-cache=false]` skip the 15-second cache ·
`[up-background]` no progress bar.

**Forms:** `[up-validate]` revalidate on blur · `[up-watch-event="input"]`
`[up-watch-delay=400]` revalidate while typing, debounced · `[up-disable]` disable in flight ·
`[up-autosubmit]` submit on change · `[up-submit=false]` opt out · `required` and `type=email`
run *before* any request · `<button name="intent" value="x">` arrives as ordinary form data.

**Fragments:** `[up-keep]` · `[up-hungry]` · `[up-poll]` `[up-interval]` · `[up-etag]`
`[up-time]` · `[up-nav]` · `[up-source]` · `[up-flashes]`.

**CSS Unpoly sets for you:** `.up-current`, `.up-active`, `.up-loading`.

### Compilers — the replacement for `DOMContentLoaded`

Unpoly inserts DOM that no `DOMContentLoaded` will ever see. **Return a destructor** for
anything global, or every swap leaks another instance and nothing visible tells you.

```js
up.compiler('[data-gallery]', (element, data, meta) => {
  const timer = setInterval(() => advance(element), data.interval ?? 1500)

  // meta describes the render pass: meta.layer, meta.revalidating, meta.response
  element.dataset.layerMode = meta.layer.mode

  return () => clearInterval(timer)     // <- without this, one timer per swap, forever
})
```

```html
<div data-gallery up-data='{ "interval": 900 }'>...</div>
```

### Config, through `o.ExtraScript`

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

Only three reach the server: `X-Up-Fail-Target` (`UpFailTarget()`), `X-Up-Fail-Mode`
(`UpFailMode()`) and `X-Up-Fail-Context` (`UpFailContext<T>()`). Everything else is
client-only by design.

## Mistakes to check first when something is broken

1. **A poll or a reload answers 500, and the error page cannot even render.**
   `Writing to the response body is invalid for responses with status code 304`. The page
   called `UpNotModified()` and then rendered anyway — in Blazor static SSR a component
   cannot decline to render after the fact, so the guard has to be in the markup.
   `UseUnpoly()` drops the body so this cannot crash, but the render still costs what it
   costs: wrap the markup in `@if (!NotModified)`. **Without `UseUnpoly()` this is a hard
   500 raised after the response has started — no error page, dead connection.**

2. **`blazor.web.js` is still in `App.razor`.** With `interactivity=None` it only adds
   enhanced navigation and enhanced forms — exactly Unpoly's job. Both present means they
   fight over clicks and submits. Remove the script tag.

3. **A swap did nothing.** The selector must exist in **both** the response and the current
   page. Unpoly resolves it on both sides; that is by design, not a bug.

4. **A swap targeting the nav, a cart badge, or anything else inside the chrome does
   nothing.** `UpChrome` stripped it from the response. Declare it:
   `<UpChrome Provides=".site-nav, .cart-badge">`.

5. **Nav or footer vanished from a normal page load.** Something treated the request as a
   fragment. `X-Up-Target` is a *list*: `"body, .flash"` is still a whole-page request.

6. **An interactive component went silent after a swap.** A region rendered with
   `@rendermode InteractiveServer` or `WebAssembly` must never be inside a swapped fragment.
   Its Blazor markers only activate through Blazor's own updater, so Unpoly swapping it
   produces dead DOM with no error and no console message. Keep it outside the target, or
   mark it `[up-keep]`.

7. **`<PageTitle>` stopped updating.** `<HeadOutlet />` was moved inside `<UpChrome>`.

8. **The `.content` element is missing from a fragment response.** The target itself was
   wrapped in `<UpChrome>`. Wrap the chrome around it, never the target.

9. **An invalid form submission looks like a success.** The handler answered 200. Answer
   **422**. And guard the handler with `IsUpValidating()` before any write.

10. **An `[up-on-accepted]` callback runs but nothing changes.** These attributes are
    evaluated as **one expression**. A multi-line body of statements silently does nothing:
    the overlay closes, the value arrives, no DOM changes, no console error. Put the body in
    a named function and keep the attribute to one call.

11. **An `[up-hungry]` region never updates, or shows a stale value.** Three silent causes:
    it sits inside `<UpChrome>` without `Provides`; Unpoly cannot derive a selector for it;
    or it lives in the layout and reflects something a page handler just did — in Blazor SSR
    **the layout renders before the page's handler**, so it swaps correctly and shows the
    previous value. Markup position does not help; that one needs POST-redirect-GET.

12. **A third-party widget dies after a swap, or the page slows down over time.** Use
    `up.compiler` and return a destructor. Application scripts belong in `<head defer>`:
    a `<script>` in the body re-executes every time that region is swapped.

13. **A handler appears to run twice.** Unpoly caches GET responses for 15 seconds and
    revalidates: it renders the cached copy, then refetches and renders again. Keep GET
    handlers idempotent.

14. **An `[up-accept]` attribute is silently ignored.** It holds
    [relaxed JSON](https://unpoly.com/relaxed-json), and a hand-built string breaks on the
    first apostrophe in the data. Serialize it:
    `up-accept="@JsonSerializer.Serialize(new { slug, name })"`.

15. **`up.emit()` fired but the overlay did not close.** Close conditions listen on the
    *layer*; `up.emit()` lands on `document`. Use `up.layer.emit()`.

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
<https://unpoly.com/up.protocol> for headers, <https://unpoly.com/targeting-fragments> for
selectors, <https://unpoly.com/handling-everything> for the config knobs above.
`CONCEPTS.md` in this repo maps every section of every Unpoly guide to the place in the
sample that exercises it, and `ATTRIBUTES.md` next to this file maps concepts to attributes.
