# Concept → attribute → Razor

For each Unpoly concept: **when you reach for it**, **which attribute**, a Razor snippet, and
what goes wrong. Ordered by how often you need it.

Read `SKILL.md` for the C# API and its contracts. Use this file while writing markup.

**The default is: no attribute at all.** `AddUnpoly` routes every `<a>` and `<form>` through
Unpoly already, and they swap `MainTargets`. Add an attribute only when you want something
*different* from that.

---

## Targeting — "which part of the page should change"

**When:** the default (`.content`) is wrong — you want to update something smaller, several
regions, or a region that is not where the link lives.

| Want | Attribute |
|---|---|
| Swap one region | `up-target=".listing"` |
| Swap several in one response | `up-target=".listing, .cart-badge"` |
| Append / prepend instead of replace | `up-target=".listing:after"` / `:before` |
| A region that may be absent, without failing the swap | `up-target=".listing, .unread:maybe"` |
| Update the element that was clicked | `up-target=":origin"` |
| Fall back when the target is missing | `up-fallback="body"` |
| Update just the layer's main region | `up-target=":main"` |
| Want no content back at all | `up-target=":none"` |

```razor
@* One response, two jobs: append rows, then replace the trigger. *@
<a href="@NextUrl" up-target=".listing:after, .more">Xem thêm</a>

@* Update only the row that was clicked. *@
@foreach (var item in Items)
{
    <tr class="row" id="row-@item.Id">
        <td><a href="/items/@item.Id/toggle" up-target=":origin">@item.State</a></td>
    </tr>
}
```

**Pitfall:** the selector must exist in **both** the response and the current page. Unpoly
resolves it on both sides. A swap that silently does nothing is almost always this.

**Pitfall:** forgetting `:after` on pagination replaces page 1 with page 2 instead of
extending the list.

---

## Link behaviour — "make this click feel faster, or opt it out"

**When:** a specific link deserves different treatment from the rest.

| Want | Attribute | What it costs |
|---|---|---|
| Prefetch on hover | `up-preload` | one request per hover — never put it on every card in a grid |
| Follow on mousedown | `up-instant` | changes click semantics; breaks drag and `[up-confirm]` |
| Load when scrolled into view | `up-defer="reveal"` | pair with `up-follow="false"` |
| Ask before doing it | `up-confirm="Xoá thật?"` | none |
| Skip the 15-second cache | `up-cache="false"` | a request that could have been free |
| No progress bar for this one | `up-background` | none |
| Let the browser handle it normally | `up-follow="false"` | a real full page load |

```razor
<a href="/p/@Item.Slug" up-preload up-instant>@Item.Name</a>

<a href="/report.pdf" up-follow="false">Tải PDF</a>          @* not a fragment at all *@

<a href="/items/@Id/delete" up-confirm="Xoá thật?" up-target=".listing">Xoá</a>
```

**Pitfall:** `up-instant` + `up-confirm` on the same link fires the dialog on mousedown, which
users experience as the page reacting before they clicked.

---

## Forms — "validate, submit, disable"

**When:** any `<form>`. These are the attributes; the C# contract is in `SKILL.md`
(`IsUpValidating()` before any write, 422 on invalid).

| Want | Attribute |
|---|---|
| Revalidate a field when the user leaves it | `up-validate` on the input |
| Revalidate while typing, debounced | `up-watch-event="input"` `up-watch-delay="400"` |
| Submit as soon as a value changes | `up-autosubmit` on the form |
| Disable every control while in flight | `up-disable` |
| Send errors somewhere other than the target | `up-fail-target=".form-wrap"` |
| Opt this form out entirely | `up-submit="false"` |

```razor
<form method="post" @formname="login" @onsubmit="Submit"
      up-target=".content" up-fail-target=".form-wrap" up-disable>
    <AntiforgeryToken />

    @* Validates on blur (the `change` event). *@
    <input name="email" type="email" required up-validate />

    @* Validates while typing instead. Without up-watch-delay this fires per keystroke. *@
    <input name="password" type="password" required
           up-validate up-watch-event="input" up-watch-delay="400" />

    <button type="submit">Đăng nhập</button>
</form>
```

**Filter forms are `method="get"`.** Filters are a view of data, so they belong in the URL:
the result stays bookmarkable, Back works, and Unpoly can cache it. A POST breaks all three
and clears Unpoly's cache on every change.

```razor
<form method="get" up-autosubmit up-watch-delay="300" up-target=".listing">
    <select name="collection">...</select>

    @* Without JavaScript there is no autosubmit. Leave a way through. *@
    <noscript><button type="submit">Lọc</button></noscript>
</form>
```

**Pitfall:** `up-validate` without the `IsUpValidating()` guard in the handler means every
blur runs the real action.

**Pitfall:** answering 200 for an invalid form makes Unpoly treat it as success and swap the
success target. Answer **422**.

---

## Loading feedback — "show that something is happening"

**When:** almost never write code for this. Unpoly sets classes; you write CSS.

| Want | How |
|---|---|
| Style the link/form that started the request | `.up-active` |
| Style the fragment being replaced | `.up-loading` |
| Highlight the current nav item | `[up-nav]` on the container → `.up-current` |
| Custom class names | `up-active-class="mine"` `up-loading-class="mine"` |
| Turn feedback off for one link | `up-feedback="false"` |
| Skeleton / spinner / optimistic content | `up-preview="name"` |

```razor
@* [up-nav] compares hrefs to the current URL for you. No C# comparing paths. *@
<nav class="site-nav" up-nav>
    <a href="/">Trang chủ</a>
    <a href="/shop">Cửa hàng</a>
</nav>

<a href="/shop" up-preview="skeleton">Cửa hàng</a>
```

```css
.up-current { font-weight: 700; }
.up-active  { opacity: .6; }
.up-loading { animation: pulse 1s infinite; }
```

**Pitfall:** writing a `bool Loading` field in C#. Static SSR has already finished rendering
by the time the request is in flight — the loading state only exists on the client.

---

## Caching and conditional requests — "don't refetch what hasn't changed"

**When:** a fragment reloads often (polling, revalidation) or the page is expensive.

| Want | Where |
|---|---|
| Give one fragment its own version | `up-etag="@Catalog.ETag"` or `up-time` |
| Exclude a fragment from versioning | `up-etag="false"` |
| Answer 304 when the client is current | `Ctx.UpNotModified(etag, lastModified)` in C# |
| Mark cached URLs stale | `Ctx.UpExpireCache("/shop/*")` |
| Drop cached URLs outright | `Ctx.UpEvictCache("/cart")` |

```razor
@* The guard is not optional. UpNotModified() may have set 304, and a 304 must render
   nothing. UseUnpoly() drops the bytes so it cannot crash, but the guard is what skips
   the query behind it. *@
@if (!NotModified)
{
    <div class="listing" up-etag="@Catalog.ETag">...</div>
}

@code {
    bool NotModified;

    protected override void OnParametersSet()
    {
        NotModified = Ctx.UpNotModified(Catalog.ETag, Catalog.LastModified);
        if (NotModified) return;

        Items = Catalog.Query(Category);
    }
}
```

**Expire vs evict:** expire still *shows* the cached copy, then refetches behind the user.
Evict never shows it again. Use evict when the old value would be *wrong* rather than merely
old — a price, a balance, a permission.

**Pitfall:** a handler that appears to run twice. Unpoly caches GET responses for 15 seconds
and revalidates: renders the cached copy, refetches, renders again. Keep GET handlers
idempotent.

---

## Passive updates — "this region updates itself"

**When:** a region must stay current without the user targeting it.

| Want | Attribute |
|---|---|
| Update from *any* response, untargeted | `up-hungry` |
| Reload on a timer | `up-poll` `up-interval="4000"` |
| Stop polling | `up-poll="false"` |
| Survive being swapped | `up-keep` |
| Force a replacement anyway, once | `up-use-keep="false"` on the link |
| Collect server messages | `up-flashes` |
| Say where a fragment came from | `up-source="/cart"` |

```razor
@* HUNGRY: three rules, all learned the hard way.
   1. never inside <UpChrome> without Provides -- the selector gets stripped
   2. must carry a derivable selector, so give it an [id]
   3. in Blazor, the layout renders BEFORE the page's handler, so if it reflects something
      a handler just did, the handler must POST-redirect-GET *@
<a href="/cart" id="cart-badge" class="cart-badge" up-hungry>Giỏ (@Cart.Count)</a>

@* POLL: the etag is what makes it cheap. Without it this costs a full render every
   4 seconds, per open tab, forever. *@
<p class="stock" up-poll up-interval="4000" up-etag="@Cart.ETag">
    Còn @Stock.Count sản phẩm
</p>

@* KEEP: the value, focus and scroll position all survive a swap of the parent. *@
<input class="note" up-keep name="note" />

@* KEEP UNTIL THE DATA CHANGES, rather than forever. *@
<div class="player" up-keep up-data='{ "track": "@Track.Id" }'>...</div>

@* FLASHES: once, in the layout. Unpoly moves messages here from any response, including
   from an overlay closing onto the layer beneath. *@
<div up-flashes></div>
```

**Pitfall:** a flash rendered into a cached GET response shows again when that entry is
replayed. Hold it server-side until the GET after the mutation consumes it.

---

## Overlays — "a sub-task, without losing this page"

**When:** the user must do something small and come back — pick a size, confirm, edit one
field. The point is what stays *behind*: scroll, state, unfinished input.

| Want | Attribute |
|---|---|
| Open in an overlay | `up-layer="new modal"` (or `drawer`, `popup`, `cover`) |
| Size it | `up-size="small"` |
| Give it starting state | `up-context='{ "from": "product" }'` |
| Handle a success | `up-on-accepted="onChosen(value)"` |
| Handle a cancel | `up-on-dismissed="..."` |
| Close with a value, no request | `up-accept="@Json"` on a button inside |
| Close as a cancel | `up-dismiss` |
| Close when a URL is reached | `up-accept-location="/orders/$id"` |
| Close on an event | `up-accept-event="order:placed"` |
| Control how it can be dismissed | `up-dismissable="key button outside"` |
| Target a different layer | `up-layer="root"` / `"parent"` / `"any"` |

```razor
@* Opening side -- no C# at all. *@
<a href="/p/@Slug/size" up-layer="new modal" up-size="small"
   up-context='{ "from": "product" }'
   up-on-accepted="applySize(value)">Chọn size</a>

@* Inside the overlay route. SERIALIZE the value: the attribute holds relaxed JSON, and a
   hand-built string breaks on the first apostrophe in the data, silently. *@
<button type="button"
        up-accept="@JsonSerializer.Serialize(new { size, label = $"Size {size}" })">@size</button>
<button type="button" up-dismiss>Huỷ</button>

@* Same route, opened directly, is still a real page: drop only the chrome the modal
   frame already provides. *@
@if (!Ctx.IsUpOverlay())
{
    <h2>Chọn size</h2>
    <a class="back" href="/p/@Slug">← Quay lại</a>
}
```

**Accept ≠ dismiss.** Accept means the sub-task finished and the opener continues with the
result. Dismiss means the user backed out. The opener has two separate callbacks.

**Pitfall:** `up-on-accepted` holds **one expression**. A multi-line body of statements
silently does nothing — no DOM change, no console error. Put it in a named function.

**Pitfall:** `up.emit()` lands on `document`, but close conditions listen on the *layer*. Use
`up.layer.emit()`.

---

## History — "what the address bar and Back should do"

**When:** a swap should or should not become a history entry.

| Want | Attribute |
|---|---|
| Don't record this navigation | `up-history="false"` |
| Record it even though it normally would not | `up-history="true"` |
| Record a different URL than the one fetched | `Ctx.UpLocation("/shop?q=...")` in C# |
| An overlay with no visible history | `up-history="false"` on the layer |
| Restore scroll on Back | `up-scroll="restore"` |

```razor
@* A filter that should not fill the history with one entry per keystroke. *@
<form method="get" up-autosubmit up-history="false" up-target=".listing">...</form>
```

**Pitfall:** scroll position is **not** restored by default. Back re-renders and lands at the
top unless a link opts in with `up-scroll="restore"`.

---

## Animation — "make the change readable"

**When:** the swap is large enough that an instant replacement is disorienting.

| Want | Attribute |
|---|---|
| Animate this swap | `up-transition="cross-fade"` |
| Animate an insertion / removal | `up-animation="move-from-right"` |
| Tune it | `up-duration="300"` `up-easing="ease-out"` |
| App-wide default | `up.fragment.config.navigateOptions.transition` via `o.ExtraScript` |

```razor
<a href="/shop" up-target=".content" up-transition="cross-fade" up-duration="200">Cửa hàng</a>
```

```csharp
o.ExtraScript = "up.fragment.config.navigateOptions.transition = 'cross-fade';";
```

**Pitfall:** transitions need both an old and a new element. Appending with `:after` has no
old element, so a transition there does nothing — use `up-animation` instead.

---

## Scripts and data — "attach behaviour to inserted DOM"

**When:** any third-party widget, chart, map, editor, or carousel.

| Want | How |
|---|---|
| Run code on matching elements, forever | `up.compiler('.sel', fn)` |
| Pass data from Razor to it | `up-data='{ "interval": 900 }'` |
| Clean up | **return a destructor** from the compiler |
| Read data from outside a compiler | `up.data('.sel')` |
| Fire a server-driven event | `Ctx.UpEmit("cart:changed", new { count })` in C# |
| Fire a client event from markup | `up-emit="lab:ping"` |

```razor
@* [up-data] arrives as the compiler's second argument, already parsed. *@
<div class="gallery" up-data='{ "interval": @Interval }'>...</div>
```

```js
// wwwroot/js/app.js, loaded from <head defer>
up.compiler('.gallery', function (element, data, meta) {
  const timer = setInterval(() => advance(element), data.interval ?? 1500)

  // meta describes the render pass: meta.revalidating, meta.layer.mode
  element.dataset.layerMode = meta?.layer?.mode ?? 'root'

  return () => clearInterval(timer)   // without this, one timer per swap, forever
})
```

**Pitfall:** `DOMContentLoaded` never sees a fragment Unpoly inserted. That is what
`up.compiler` replaces.

**Pitfall:** a `<script>` in the body re-executes every time that region is swapped.
Application scripts belong in `<head defer>`.

**Pitfall:** a region rendered `@rendermode InteractiveServer` must never sit inside a swapped
fragment. Its Blazor markers only activate through Blazor's own updater, so Unpoly swapping it
produces dead DOM with no error and no console message. Keep it outside the target, or mark it
`[up-keep]`.

---

## Asset changes — "the app was deployed while this tab was open"

**When:** long-lived sessions where stale JS against new HTML would break.

```razor
@* Inside <head>. Only <head> assets are tracked -- a marker anywhere else does nothing. *@
<meta name="app-version" content="@AssetVersion" up-asset />
```

```js
up.on('up:assets:changed', function (event) {
  // Notify ONCE. The event fires on every render pass whose assets differ, so an
  // unconditional append stacks one identical message per navigation.
  const flashes = document.querySelector('[up-flashes]')
  if (!flashes || flashes.querySelector('.assets-changed')) return

  flashes.appendChild(buildReloadPrompt())
})
```

**There is no default behaviour** — the app decides what a new version means. Do not
auto-reload by hijacking `up:link:follow`: it turns every subsequent navigation into a full
page load and breaks overlays. Offer the reload; let the user pick the moment.

---

## Quick decision table

| Symptom / want | Reach for |
|---|---|
| "Only this part should change" | `up-target` |
| "It should feel instant" | `up-preload`, `up-instant` |
| "Show errors in the form, not the page" | `up-fail-target` + answer 422 |
| "Check the field as they type" | `up-validate` + `up-watch-event="input"` |
| "Filter without a Submit button" | `up-autosubmit` on a `method="get"` form |
| "Load more as they scroll" | `up-target=".list:after, .more"` + `up-defer="reveal"` |
| "Keep this in sync everywhere" | `up-hungry` + `[id]` |
| "Refresh this every few seconds" | `up-poll` + `up-etag` |
| "Don't lose what they typed" | `up-keep` |
| "Ask something, then come back" | `up-layer="new modal"` + `up-accept` |
| "Don't fill the history" | `up-history="false"` |
| "This widget dies after a swap" | `up.compiler` + destructor |
| "Show a spinner" | CSS on `.up-active` / `.up-loading` |
