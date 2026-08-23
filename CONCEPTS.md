# CONCEPTS.md

Per-guide tracking. **Every section of a guide that has been read gets a row** — including
the ones with nothing to do. A guide is only finished when its whole table of contents
appears here.

`TASKS.md` tracks *what to do next*. `VERIFY.md` tracks *how you prove it works*.

**Legend**

| | |
|---|---|
| ✅ | covered — code exists and a check or wire observation proves it |
| ⬜ | not covered yet — the phase that will cover it is named |
| ➖ | nothing for **us** to write — pure client-side. Still goes into the skill's "reach for HTML first" table, because it is exactly where an agent invents a C# helper for a one-attribute job |
| 🚫 | deliberately declined — the reason is stated |

**Sample** points into `sample/Jubin/` as `file:line · token`. Lines drift; grep the token.

The empty cell was doing four different jobs, so it is now explicit:

| | |
|---|---|
| `file · token` | the sample exercises it |
| `n/a` | **only** a structural row in the guide's own contents (Example, Contents, Resources), or something explicitly declined. Nothing else qualifies |
| `todo` | demonstrable, not built yet. **There are none left**: all 305 rows point at the sample or say why they cannot |

"No C# side" (➖) does **not** imply `n/a`. The sample is a web app: most client-side
concepts are demonstrable in its markup, and treating ➖ as "nothing to show" is how seven
target-syntax variants went unexercised until a playground was added.

There are no bare dashes left, and `n/a` is now deliberately hard to earn. An audit of the
twenty rows once marked `n/a` found only six that deserved it: four structural rows, the
page intro, and the one deliberately declined feature. The other fourteen were demonstrable
all along — "JS API" and "client config" are not reasons, they are just different
markup. Every row either points at the sample or says, in words, what is missing. `sample/Jubin/Components/Pages/Lab.razor` (`/lab`) collects the client-side
concepts: one link per concept, each labelled with the guide section it comes from.

---

## `up.link`

### [/handling-everything](https://unpoly.com/handling-everything) — 6/6 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Following all links | ✅ `HandleAllLinksAndForms`, default **on** | `Program.cs:8` · `AddUnpoly` |
| 2 | Following all links on mousedown | ✅ `InstantAllLinks`, default **off** | `Lab.razor` · `up-instant` — aimed at a 1.2s route |
| 3 | Preloading all links | ✅ `PreloadAllLinks`, default **off**; `preloadDelay` is **90ms** | `Lab.razor` · `up-preload`, `up-preload="insert"` |
| 4 | Handling all forms | ✅ same option | `Login.razor:22` · `EditForm` |
| 5 | Fixing legacy JavaScript code | ➖ Phase G territory | `app.js` · scripts in head, compilers not DOMContentLoaded |
| 6 | Customizing navigation defaults | ➖ `ExtraScript` carries `navigateOptions` | `Program.cs` · `ExtraScript` · `navigateOptions.transition` |

Navigation applies defaults `up.render()` does not — `history/scroll/focus: 'auto'`,
`fallback: ':main'`, `cache: 'auto'`, `revalidate: 'auto'`, `peel: 'dismiss'`. Override via
`o.ExtraScript = "up.fragment.config.navigateOptions.transition = 'cross-fade'"`.

Both new flags default **off**: `instant` changes click semantics; `preload` multiplies
server load — sweeping a mouse across a product grid fires one request per card.

### [/targeting-fragments](https://unpoly.com/targeting-fragments) — 19/19 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Swapping a fragment | ✅ explicit `[up-target]` now used, not only `mainTargets` | `Home.razor` · `up-target` · `Shop.razor` · `refresh` |
| 2 | Updating multiple fragments | ✅ `UpTargets()` splits lists; checked | `Home.razor` · playground |
| 3 | Optional targets | ✅ `:maybe` stripped by `BaseTarget()` | `Home.razor` · playground |
| 4 | Targeting the main element | ✅ `:main` is whole-page; checked | `Program.cs:11` · `MainTargets` |
| 5 | Targeting the entire layer | ✅ `:layer` is whole-page; checked | `LabLayers.razor` · `up-layer` matrix |
| 6 | Targeting an element object | ➖ JS API | `Lab.razor` · `up.render({ target: element })` |
| 7 | Appending or prepending children | ✅ `:after` / `:before` stripped; checked | `Shop.razor` · `.listing:after` |
| 8 | Replacing all children | ✅ `:content` stripped; checked | `Home.razor` · playground |
| 9 | Targeting nothing | ✅ `WantsNothing()` → 204, 0 bytes | `ProductDetail.razor` · `up-target=":none"` |
| 10 | Resolving ambiguous selectors | ➖ client-side matching | `Lab.razor` · two `.demo-card` blocks |
| 11 | Targeting an ancestor element | ➖ selector syntax | `Lab.razor` · `.demo-card .demo-text` |
| 12 | Targeting a sibling element | ➖ selector syntax | `Lab.razor` · Card A vs Card B |
| 13 | Disabling region-aware matching | ➖ client config | `Lab.razor` · `up-match="first"` |
| 14 | Referring to the origin element | ➖ resolves to a derived selector before the header is sent | `Home.razor` · playground |
| 15 | Dealing with missing targets | ➖ client-side | `Home.razor` · playground |
| 16 | Providing a fallback target | ➖ resolved on the client | `Home.razor` · `up-fallback` |
| 17 | Falling back to the main target | ➖ `{ fallback: true }` | `Home.razor` · playground |
| 18 | Making targets optional | ✅ same `:maybe` handling as §3 | `Home.razor` · playground |
| 19 | Changing the target in-flight | ✅ `UpRetarget()` — the server half | `Shop.razor` · `UpRetarget` |

`BaseTarget()` exists because §3, §7, §8 and §18 all append a modifier to a selector.
Modifiers change **how** a match is applied, never **what** is matched. Comparing raw
strings made `body:after` look like a fragment and dropped the chrome from a request that
wanted to append into `<body>`.

### [/failed-responses](https://unpoly.com/failed-responses) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Rendering failed responses differently | ✅ the `fail` prefix — see below | `Login.razor:23` · `up-fail-target` |
| 2 | Ignoring HTTP error codes | ➖ `{ fail: false }` client-side | `Lab.razor` · `up-fail="false"` |
| 3 | **Customizing failure detection** | ⬜ **server-relevant** — `up.network.config.fail` is a function the app can widen, e.g. treat a response header as failure. Reachable today through `ExtraScript`; no C# helper yet | `Lab.razor` · `/lab/unauthorized` + `Program.cs` · `config.fail` |
| 4 | Local content cannot fail | ➖ no request involved | `Lab.razor` · `up-content` |
| 5 | Handling unexpected content | ➖ `up:fragment:loaded` | `LabFragment.razor` · `X-Lab-Refuse` |
| 6 | Handling fatal network errors | ➖ client-side | `Lab.razor` · `/lab/slow?case=pollfail` (503) |
| 7 | Handling aborted requests | ➖ client-side | `LabLayers.razor` · overlapping navigations between layers |
| 8 | Detecting a failed response programmatically | ➖ JS API | `Lab.razor` · `up-on-fail-rendered` |

Failure is any status **outside 2xx and 304** — so Phase B's conditional 304 never trips a
fail target. `IsUpFragment()` keeps chrome when *either* branch names a whole-page target,
because the layout renders chrome before the page has picked a status.

#### §1 in full — the `fail` prefix

Every render option that is consumed **after** the response arrives has a `fail`-prefixed
twin, chosen when the response failed:

```js
up.render({
  url: '/action',
  method: 'post',
  target: '.content',            // success
  failTarget: 'form',            // failure
  scroll: 'auto',
  failScroll: '.errors',
  onRendered: () => { ... },
  onFailRendered: () => { ... },
})
```

The convention is **not universal**, and the rule for which options have a twin is the
useful part:

| Kind of option | `fail` twin? | Why |
|---|---|---|
| Used **before** the request — `url`, `method`, `confirm` | no | at that point nobody knows it will fail |
| Used **after** the response — `target`, `scroll`, `layer`, `mode`, `context`, `onRendered` | yes | the two outcomes want different handling |
| Used for **both** — `history`, `fallback` | optional override | e.g. `{ history: true, failHistory: false }` |

HTML equivalents follow mechanically: `[up-fail-target]`, `[up-fail-scroll]`,
`[up-on-fail-rendered]`.

**Why this matters on the server.** Most `fail` options never leave the browser —
`failScroll`, `onFailRendered` and `failHistory` are pure client behaviour. Only the ones
the server must *render differently for* are sent, which is exactly why the protocol has
three `X-Up-Fail-*` headers and no more:

| Client option | Wire header | Status here |
|---|---|---|
| `failTarget` | `X-Up-Fail-Target` | ✅ `UpFailTarget()`, `UpFailTargets()` |
| `failLayer` / `failMode` | `X-Up-Fail-Mode` | ⬜ **Phase D** |
| `failContext` | `X-Up-Fail-Context` | ⬜ **Phase D** |
| `failScroll`, `onFailRendered`, `failHistory`, … | none | ➖ never crosses the wire |

That list is the whole reason `IsUpFragment()` has to consult both branches: the client may
have picked an entirely different target for the failure case, and the server is told about
it up front rather than after choosing a status.

#### §3 — customizing failure detection

The server can emit a header and the client be configured to treat it as failure:

```js
let badStatus = up.network.config.fail
up.network.config.fail = (response) => badStatus(response) || response.header('X-Unauthorized')
```

---

## `up.fragment`

Marked read in `TASKS.md` since Phase A and never enumerated here — the fourth time in
this project that a claim outran the record, and the largest. `/render-lifecycle` in
particular has been called the most important page in these docs and had no row at all.

### [/navigation](https://unpoly.com/navigation)

| Concept | Status | Sample |
|---|---|---|
| Navigation = render + a bundle of defaults | ➖ `history/scroll/focus: auto`, `fallback: :main`, `cache: auto`, `revalidate: auto`, `peel: dismiss` | `Program.cs` · `ExtraScript` |
| `up.render()` applies none of them | ➖ | `Lab.razor` · `up.render` button |

### [/render-lifecycle](https://unpoly.com/render-lifecycle) — 14/14 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—2 | Render lifecycle, lifecycle diagram | ➖ the map every other guide plugs into | n/a — structural |
| 3 | Running code after rendering | ➖ `[up-on-rendered]` | `ProductDetail.razor` · `up-on-accepted` |
| 4 | Awaiting postprocessing | ➖ JS API | `LabFragment.razor` · `up-on-finished` |
| 5 | Running code after each render pass | ➖ `up:fragment:inserted` | used in `diag_accept.py` |
| 6 | Inspecting the render result | ➖ JS API | `LabFragment.razor` · `up.render().then(r => ...)` |
| 7 | Controlling the render process | ➖ | `LabFragment.razor` · render option overrides |
| 8—11 | Handling errors, errors in user code, debugging, example | ➖ | `LabFragment.razor` · `/lab/fragment/boom` (500) |
| 12 | Preventing a render pass | ➖ `up:fragment:loaded` | `app.js` · `up:fragment:loaded` → `preventDefault()` |
| 13 | Changing options before rendering | ➖ | `LabFragment.razor` · `renderOptions.target` |
| 14 | Advanced example | ➖ | n/a — structural |

**Where the server takes part:** delivering the response, returning a non-2xx that triggers
the failure branch, and answering a revalidation. Everything after that is client-side.
That single sentence is why this library is 400 lines and not 4000.

### [/target-derivation](https://unpoly.com/target-derivation) — 4/4 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Identifying properties | ➖ | `MainLayout.razor` · `#cart-badge` |
| 2 | **Derivation patterns** | ➖ see the order below | same |
| 3 | Derived target verification | ➖ Unpoly re-checks the selector matches | same |
| 4 | Deriving a target programmatically | ➖ `up.fragment.toTarget()` | `diag_double.py` |

Priority order, highest first: `[up-id]`, `[id]`, `html`, `head`, `body`, `main`, `[up-main]`,
`link[rel]`, `meta[property]`, `*[name]`, `form[action]`, `a[href]`, `[class]`, `form`.

**This corrects a conclusion recorded in Phase F.** The `[up-hungry]` badge was said to need
an `[id]` because "a bare class is not derivable". It is — `[class]` is pattern 13, and
`a[href]` is pattern 12, so that badge was derivable twice over. The `[id]` did not fix
anything; the actual cause was the Blazor render order, found two attempts later. Adding the
`[id]` is still worth keeping — it is more stable than a class — but the reasoning was wrong.

### [/providing-html](https://unpoly.com/providing-html) — 12/12 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—2 | Loading HTML from the server, usage in forms | ✅ the ordinary path | everywhere |
| 3—4 | Programmatic API, rendering a string of HTML | ➖ | `Lab.razor` · `up.render` |
| 5 | Replacing a fragment's children | ➖ `{ content }` / `[up-content]` | `Lab.razor` · `up-content` |
| 6—7 | Rendering a fragment string, omitting `[href]` | ➖ | `Lab.razor` · `up-content` |
| 8 | Sanitizing user input | ➖ caller's responsibility | n/a — the caller's job; Unpoly does not escape for you |
| 9 | Extracting a fragment from a document | ➖ | n/a — this is what Unpoly does on every swap |
| 10—12 | Rendering a `<template>`, an `Element`, an `up.Response` | ➖ JS API | `LabFragment.razor` · `up-document`, `up.element.createFromHTML` |

### [/preserving-elements](https://unpoly.com/preserving-elements) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—2 | Use cases, basic example | ➖ `[up-keep]` | `LabFragment.razor` · `.kept-input` |
| 3—6 | Keep conditions: until HTML changes, until data changes, custom | ➖ | `LabFragment.razor` · `.kept-html`, `.kept-data` |
| 7 | Forcing an update | ➖ | `LabFragment.razor` · `up-use-keep="false"` |
| 8 | Updating data for kept elements | ➖ | `LabFragment.razor` · `.kept-data` `[up-data]` |

The server renders ordinary HTML; the client matches `[up-keep]` elements across versions by
**derived selector**, which is why §3 of `/target-derivation` matters here too.

### [/skipping-rendering](https://unpoly.com/skipping-rendering) — 7/7 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Rendering nothing | ✅ `:none` → **204** | `ProductDetail.razor` · ping form |
| 2 | **Skipping rendering of unchanged content** | ✅ **304** via `UpNotModified` | `Shop.razor`, `ProductDetail.razor` |
| 3 | Preventing rendering of loaded responses | ➖ `up:fragment:loaded` | `app.js` · `up:fragment:loaded` |
| 4 | Global skipping rules | ➖ | `app.js` · one listener covers every pass |
| 5 | Partially rendering a response | ✅ this is `UpChrome` | `MainLayout.razor`, `App.razor` |
| 6 | Preserving elements in a targeted fragment | ➖ `[up-keep]` | `LabFragment.razor` · `[up-keep]` inside `.content` |
| 7 | Preventing a render pass before it starts | ➖ | `app.js` · `up:fragment:loaded` → `preventDefault()` |

This guide is the one that names all three server-side ways to say "render nothing":
`X-Up-Target: :none`, **204**, and **304**. All three are implemented and checked.

### [/templates](https://unpoly.com/templates) — 10/10 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—3 | Rendering a template, shorthand, features that support it | ➖ | `LabFragment.razor` · `#lab-template` |
| 4—9 | Dynamic templates: compiler data, template engines, parsing expressions, integrations, programmatic | ➖ | `LabFragment.razor` · template + compiler data |
| 10 | Template lookup order | ➖ | `LabFragment.razor` · `up-document="#lab-template"` |

Templates let the client clone fragments **without a request**. The server's only part is
embedding the `<template>` in a response. Worth revisiting if optimistic rendering is ever
built.

---

## `up.event`

No guides — this module is reference only. Its features:

| Feature | Status | Sample |
|---|---|---|
| `up.on()` · `up.off()` | ➖ | `app.js`, the browser suite |
| `up.emit()` | ➖ the client-side twin of `UpEmit` | `Lab.razor` · `up.emit()` button |
| `[up-emit]` on an element | ➖ | `Lab.razor` · `up-emit="lab:pinged"` |
| `up.event.build()` · `halt()` · `onEscape()` | ➖ | `Lab.razor` · three buttons, one each |
| **Server-emitted events** | ✅ `UpEmit` → `X-Up-Events` | `ProductDetail.razor` |

---

## `up.protocol`

### [/optimizing-responses](https://unpoly.com/optimizing-responses) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Optimizing responses | ✅ optional; full documents stay valid | n/a |
| 2 | Omitting content that isn't targeted | ✅ `UpChrome` | `App.razor:11` head · `MainLayout.razor:10,34` |
| 3 | Example | — | `/p/dam-4`: 1865 → 459 bytes |
| 4 | Note (the `Vary` requirement) | ✅ `UpVary()` + `UseUnpoly()` | `Program.cs:36` |
| 5 | Rendering different content for overlays | ✅ `IsUpOverlay()` | `SizePicker.razor` · chrome heading vs overlay title |
| 6 | Rendering different content for Unpoly requests | ✅ `IsUnpoly()` | `Program.cs:28` |
| 7 | Example | — | n/a — structural row |
| 8 | Rendering content that depends on layer context | ✅ `UpContext<T>()` changes the render, not just echoes it | `SizePicker.razor` · `.from-product` |

`UpChrome` alone was wrong for any target that lives **inside** the chrome. A link asking
for `.content, .site-nav` got the chrome stripped, so `.site-nav` was absent from the
response and the swap found nothing — silently. `<UpChrome Provides=".site-nav">` declares
what a chrome contains, and `UpWantsAny()` checks both branches against it. The optimisation
stays granular: targeting `.site-nav` brings back the header and still omits the footer.

### [/up.protocol.config](https://unpoly.com/up.protocol.config)

| Concept | Status | Sample |
|---|---|---|
| `csrfHeader` | ✅ from `AntiforgeryHeaderName` | `App.razor:23` · `UnpolyHead` |
| `csrfToken` | ✅ from `IAntiforgery.GetAndStoreTokens` | same |
| `csrfParam` | ➖ `EditForm` renders its own hidden input | `Login.razor:22` |
| `methodParam` (`_method`) | ✅ `UpMethodCookie()` | `Receipt.razor` · a POST that renders a full page |
| `maxHeaderSize` | ➖ observed through `:unknown` | `Login.razor:64` |

### [/conditional-requests](https://unpoly.com/conditional-requests) — 7/7 sections

The URL is `/conditional-requests`; `/conditional-responses` (as `up.protocol` links it) 404s.

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Conditional requests | ✅ one code path serves reload, revalidation and polling | `Shop.razor:31` |
| 2 | Content changed from a known hash | ✅ `If-None-Match`, incl. `W/`, lists, `*` | `Catalog.cs:46` · `ETag` |
| 3 | Content newer than a known time | ✅ `If-Modified-Since`, truncated to whole seconds | `Catalog.cs:44` · `LastModified` |
| 4 | Modification time or content hash? | — both supported; the sample sends both | `Shop.razor:31` |
| 5 | Individual versions per fragment | ➖ `[up-etag]` / `[up-time]` attributes | `Shop.razor` · `up-etag` |
| 6 | Removing versions for a fragment | ➖ attribute set to `false` | `Shop.razor` · `up-etag="false"` |
| 7 | Resources | — | n/a — structural row |

304 with an empty body, verified on the wire: `/shop` 9194 → **0** bytes.

---

## `up.network`

### [/caching](https://unpoly.com/caching) — 21/21 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Enabling caching | ➖ `cache: 'auto'` during navigation | `Lab.razor` · click twice, second leaves no log line |
| 2–6 | Disabling caching (globally / per route / per link / per call) | ➖ client config and `[up-cache=false]` | `Lab.razor` · `up-cache="false"` |
| 7 | **Revalidation** | ⬜ **not reproduced** — needs a browser | `Program.cs:28` · request log |
| 8 | When nothing changed | ✅ 304 path, now proven in **both** directions — after `Catalog.Touch()` the old ETag returns 200 again | `Shop.razor` · `UpNotModified` · `Refresh()` |
| 9 | Preventing rendering of revalidation responses | ➖ `up:fragment:loaded` | `Lab.razor` · `event.skip()` on a revalidation |
| 10 | Detecting revalidation from a compiler | ➖ Phase G | `app.js` · third compiler argument `meta.revalidating` |
| 11–12 | Enabling / disabling revalidation | ➖ client config | `Lab.razor` · `up-revalidate="false"` |
| 13 | Expiration | ✅ `UpExpireCache()` | `Shop.razor` · `UpExpireCache` |
| 14 | Expiring content after an interaction | ✅ `X-Up-Expire-Cache`, incl. `false` via `UpKeepCache()` | `Shop.razor` · `Refresh()` |
| 15 | Eviction | ✅ `UpEvictCache()` | `Shop.razor` · `UpEvictCache` |
| 16 | Evicting content after an interaction | ✅ same header | `Shop.razor` · `mode=evict` button |
| 17 | Capping memory usage | ➖ `cacheSize` | `Lab.razor` · `up.network.config.cacheSize` |
| 18 | **Caching optimized responses** | ✅ this is why `Vary` is mandatory | `Program.cs:36` · `UseUnpoly` |
| 19 | Example | — | n/a — structural row |
| 20 | **How cache entries are matched** | ✅ keyed by URL, then partitioned by every header named in `Vary` | same |
| 21 | Caching after redirects | ✅ the add-to-cart POST redirects, and the cache follows | `ProductDetail.razor` · `Nav.NavigateTo` |

GET responses cache for **15 s** (`cacheExpireAge`). A non-GET expires the **entire** cache
by itself, so calling `UpExpireCache` after an ordinary POST is redundant — the sample
deliberately does not.

§18 and §20 matter more than they look. Unpoly's **own in-tab cache** partitions on `Vary`:

> "By default cached responses will match all requests to the same URL. When a response has
> a `Vary` header, matching requests must additionally have the same values for all listed
> headers."

So `Vary: X-Up-Target` is not a CDN concern. Without it, a fragment response is reused for
a full page load *in the same tab*, seconds later.

### [/progress-bar](https://unpoly.com/progress-bar)

| Concept | Status | Sample |
|---|---|---|
| Bar shown after `lateDelay` | ➖ on by default | `Lab.razor` · `/lab/slow` |
| `progressBar = false` to replace it | ➖ via `ExtraScript` | `Lab.razor` · `up-late-delay="false"` (per-request form) |
| `[up-background]`; preload and poll are background | ➖ | `Lab.razor` · `up-background` |

### [/up:request:load](https://unpoly.com/up:request:load)

| Concept | Status | Sample |
|---|---|---|
| Mutating `event.request.headers` before send | 🚫 **declined** — it injected the CSRF token, which `up.protocol.config` already does. Nine lines deleted | n/a — declined; the nine lines were deleted |

### /aborting-requests · /network-issues

Not read. Expected to be entirely client-side; confirm before ticking.

---

## `up.form`

### [/validation](https://unpoly.com/validation) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Validating forms | ✅ `IsUpValidating()` | `Login.razor:59,73,83` |
| 2 | Contents | — | n/a — structural row |
| 3 | Validating after submission | ✅ a field changed after a 422 still validates | `ProtocolDetailTests` |
| 4 | Signaling a failed submission | ✅ answer **422** | `Login.razor:83` · `Invalid()` |
| 5 | Changing how validation errors are rendered | ➖ `[up-form-group]` picks the swapped region | `Login.razor:26,32` |
| 6 | HTML5 validations | ➖ browser-native, runs before any request | `Login.razor` · `required` |
| 7 | Validating after changing a field | ✅ `[up-validate]`; fields arrive **space** separated | `Login.razor:28,34` |
| 8 | Validating while typing | ➖ `[up-watch-event=input]` | `Login.razor` · `up-watch-event="input"` |

`X-Up-Validate` batches fields into one request. `:unknown` has two causes — a non-field
origin, or the list exceeding `maxHeaderSize` — and both mean validate the whole form.
The handler must not persist: that guard is the entire point of the header.

### [/submitting-forms](https://unpoly.com/submitting-forms) — 12/12 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Forms that update fragments | ✅ `submitSelectors` | `Login.razor:22` |
| 2 | Updating other fragments | ✅ `UpRetarget()` from a form handler | `Shop.razor` · `Refresh()` |
| 3 | Handling validation errors | ✅ 422 | `Login.razor:83` |
| 4 | Rendering error messages elsewhere | ➖ `[up-fail-target]` | `Login.razor:23` |
| 5 | Multiple submit buttons | ➖ `[up-submit]` per button | `Login.razor` · `name="intent"` |
| 6 | Per-button parameters | ➖ `[formmethod]`/`[formaction]` | `Login.razor` · `value="guest"` → `Intent` |
| 7 | Per-button actions | ➖ | `Login.razor` · `formaction="/lab"` |
| 8 | Showing that the form is processing | ➖ `[up-disable]` | `Login.razor:23` · `app.css:274` |
| 9 | Overriding render options | ➖ attributes | `Lab.razor` · `[up-scroll]`, `[up-focus]` on a link |
| 10 | Handling all forms automatically | ✅ `HandleAllLinksAndForms` | `Program.cs:8` |
| 11 | Opting into a full page load | ➖ `[up-submit=false]` | `Lab.razor` · `up-follow="false"` |
| 12 | Submitting forms with JavaScript | ➖ `up.submit()` | `Login.razor` · `up.submit(this.form)` |

Antiforgery verified on the wire: valid token 200/422, missing token **400**.

### [/disabling-forms](https://unpoly.com/disabling-forms)

| Concept | Status | Sample |
|---|---|---|
| `[up-disable]` while in flight | ➖ client-side | `Login.razor:23` · `app.css:274` |

### /switching-form-state · /watch-options · /reactive-server-forms

Not read. `[up-switch]`, `[up-autosubmit]`, `[up-watch-delay]` are client-side;
`/reactive-server-forms` may have a server story — read it before ticking Phase C.


---

## `up.layer`

### [/layer-terminology](https://unpoly.com/layer-terminology) — 2/2 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Available modes: `modal`, `drawer`, `popup`, `cover`; root is `root` | ✅ `UpMode()`, `IsUpOverlay()` | `Lab.razor` · four mode links |
| 2 | See also | ➖ | n/a — structural row |

An overlay is any layer that is not the root. `IsUpOverlay()` is `UpMode() != "root"`, and
`UpOriginMode()` is the layer that *issued* the request — the two differ exactly while an
overlay is being opened, which is how a page tells "I am being opened as a modal" from
"I am already inside one".

### [/layer-option](https://unpoly.com/layer-option) — 12/12 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | The default (`current`) | ➖ client-side | `Lab.razor` |
| 2 | Disabling layer isolation (`any`) | ➖ | `SizeGuide.razor` · `up-layer="any"` |
| 3 | Matching the root layer | ➖ | `SizeGuide.razor` · `up-layer="root"` |
| 4 | Matching any overlay | ➖ | `LabLayers.razor` · `up-layer="root, front"` |
| 5 | Matching the current or frontmost layer | ➖ | `SizeGuide.razor` · `up-layer="current"` |
| 6 | Matching relative to the current layer (`parent`, `closest`, `ancestor`, `child`, `descendant`, `subtree`) | ➖ | `LabLayers.razor` · closest, ancestor, subtree |
| 7 | Matching the layer of a given element | ➖ JS API | `LabLayers.razor` · `up.layer.get(element)` |
| 8 | Matching a layer by index | ➖ | `LabLayers.razor` · `up-layer="0"` |
| 9 | Using a layer reference | ➖ JS API | `LabLayers.razor` · `up.layer.get(0)` |
| 10 | Matching in multiple layers | ➖ | `LabLayers.razor` · `up-layer="root, front"` |
| 11 | Opening new layers (`new`, `swap`, `shatter`) | ✅ `new` used; `swap`/`shatter` untried | `ProductDetail.razor` · `up-layer="new modal"` |
| 12 | Targeting another layer for server errors | ✅ `UpFailMode()` — the server is told up front | `SizeGuide.razor` · `up-fail-layer="root"` |

Rows 2—10 all need a **stack** of overlays to mean anything, and the sample opens only one
at a time. That is one exercise, not ten.

### [/opening-overlays](https://unpoly.com/opening-overlays) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Opening an overlay from a link | ✅ | `ProductDetail.razor` · `up-layer` |
| 2 | Choosing the overlay mode | ✅ | `Lab.razor` · modal/drawer/popup/cover |
| 3 | Opening an overlay from a form | ➖ same attribute on a form | `LabLayers.razor` · `<form up-layer="new drawer">` |
| 4 | Opening overlays from local content | ➖ `[up-content]` | `Lab.razor` · `up-content` |
| 5 | Opening overlays from JavaScript | ➖ `up.layer.open()` | `LabSubinteractions.razor` · `up.layer.ask()` |
| 6 | **Opening overlays from the server** | ✅ `UpOpenLayer(options)` — relaxed JSON of render options | `SizePicker.razor` · `?serverOpens=1` |
| 7 | Close conditions | ✅ see /closing-overlays | `SizePicker.razor` |
| 8 | Replacing existing overlays (`swap`, `shatter`) | ➖ | `SizeGuide.razor` · swap keeps 3 layers, shatter leaves 2 |

### [/closing-overlays](https://unpoly.com/closing-overlays) — 13/13 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Distinguishing close intents | ✅ accept ≠ dismiss | `SizePicker.razor` |
| 2 | Running code when an overlay closes | ➖ `[up-on-accepted]` / `[up-on-dismissed]` | `ProductDetail.razor` |
| 3 | Overlay result values (acceptance values, dismissal reasons) | ✅ | `SizePicker.razor` · `UpAcceptLayer` |
| 4 | Close conditions (location, event, fragment, discarded flashes/response) | ➖ `[up-accept-location]` etc | `LabLayers.razor` · accept-location, accept-event, dismiss-event |
| 5 | **Closing from the server** | ✅ `UpAcceptLayer` / `UpDismissLayer` | `SizePicker.razor` · `Pick()` |
| 6 | Closing from JavaScript | ➖ | `LabLayers.razor` · `up.layer.accept()` / `dismiss()` |
| 7 | Closing when a button is clicked | ➖ `[up-dismiss]` | `SizePicker.razor` · `up-dismiss` |
| 8 | Closing when a link is followed | ➖ | `LabLayers.razor` · peel via `up-layer="root"` |
| 9 | Closing when a form is submitted | ✅ this is the sample's accept path | `SizePicker.razor` |
| 10 | Closing by targeting a background layer (peeling) | ➖ | `SizeGuide.razor` · `up-layer="root"` peels both |
| 11 | Customizing dismiss controls | ➖ CSS/attributes | `LabLayers.razor` · `up-dismissable="key button outside"` |
| 12 | Close animation | ➖ | `LabLayers.razor` · `up-close-animation="move-to-top"` |
| 13 | Using overlays as promises | ➖ JS API | `LabSubinteractions.razor` · `up.layer.ask().then()` |

Accept and dismiss are not interchangeable. Accept means the sub-task finished and the
parent should continue **with the result**; dismiss means the user backed out and carries a
*reason*, not a value.

### [/subinteractions](https://unpoly.com/subinteractions) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Example | ➖ | n/a — structural row |
| 2 | Starting a subinteraction | ✅ | `ProductDetail.razor` · size picker |
| 3 | Common acceptance callbacks | ➖ | `ProductDetail.razor` · `onSizeChosen` |
| 4 | Reloading on acceptance | ➖ `up.reload` in the callback | `LabSubinteractions.razor` · `up.reload('.picked-list')` |
| 5 | Adding options to an existing select | ➖ | `LabSubinteractions.razor` · `addCollectionOption` |
| 6 | Navigating away | ➖ | `LabSubinteractions.razor` · `up.navigate` on accept |
| 7 | Reusing existing screens | ✅ the picker is a real route, overlay or not | `SizePicker.razor` |
| 8 | Awaiting subinteractions from JavaScript | ➖ JS API | `LabSubinteractions.razor` · `up.layer.ask()` |

The point of §2 is what stays *behind*: the product page keeps its scroll, its state and its
unfinished input while the overlay runs. That is why it is a sub-interaction rather than a
navigation.

### [/context](https://unpoly.com/context) — 5/5 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Layer context | ✅ `UpContext<T>()` | `SizePicker.razor` |
| 2 | Initializing the context object | ➖ `[up-context]` on the opener | `ProductDetail.razor` · `up-context` |
| 3 | Working with the context object | ✅ read in, `UpSetContext` out | `SizePicker.razor` · `Pick()` |
| 4 | Re-using an interaction with a variation | ✅ the picker renders differently by mode | `SizePicker.razor` · `IsUpOverlay()` |
| 5 | Example | ➖ | n/a — structural row |

The guide flags a cache trap: a response that depends on context **must** list
`X-Up-Context` in `Vary`, or two layers with different context share one entry.
`UseUnpoly()` now varies on `X-Up-Mode` and `X-Up-Context` as well as target and version.

### [/customizing-overlays](https://unpoly.com/customizing-overlays) — 10/10 sections

Sizes, classes, dismiss controls, animations, popup and drawer position, CSS structure.
**Entirely client-side** — the guide states no server-side modification is involved. `up-size`
is used in the sample; the rest is styling.

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—10 | mode choice, HTML structure, CSS, sizes, dismiss controls, classes, elements, animations, popup and drawer position | ➖ no server side | `Lab.razor` · `up-size`, `up-align` |

---

## `up.history`

### [/updating-history](https://unpoly.com/updating-history) — 9/9 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | When history is changed | ➖ client decides | `Lab.razor` |
| 2 | What is updated when history changes | ✅ URL, title, meta | `Lab.razor` |
| 3 | Only major fragments change history | ➖ `mainTargets` decides | `Program.cs` |
| 4 | Forcing a history change | ➖ `[up-history=true]` | `LabHistory.razor` · `up-history="true"` |
| 5 | Preventing a history change | ➖ `[up-history=false]` | `Lab.razor` · `up-history="false"` |
| 6 | Only `GET` requests change history | ➖ | n/a — nothing to observe beyond the rule |
| 7 | Changing history after a form submission | ✅ `UpLocation()` | `Lab.razor` · `/lab/relocated` |
| 8 | Changing history during programmatic rendering | ➖ JS API | `LabHistory.razor` · `{ history, location }` |
| 9 | Partial history updates | ✅ `UpTitle()` | `Lab.razor` · `/lab/titled` |

`X-Up-Title` is **JSON-encoded**: the quotes are part of the header value, so
`X-Up-Title: "Playlist browser"`. The `/updating-history` summary reads as plain text; the
dedicated `/X-Up-Title` page is explicit that "the quotes must be included". When the two
disagree, the header page wins.

A second reason to encode rather than pass through: `JsonSerializer` escapes non-ASCII to
`\uXXXX`, which keeps the header ASCII-safe. A Vietnamese title sent raw would not be.

### [/restoring-history](https://unpoly.com/restoring-history) — 4/4 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Default restoration behaviour | ➖ Unpoly refetches or reuses cache | `LabHistory.razor` · Back after a swap |
| 2 | Custom restoration behaviour | ➖ `up:location:restore` | `LabHistory.razor` · `up:location:restore` |
| 3 | Handled history entries | ➖ | n/a — internal bookkeeping |
| 4 | History restoration with overlays | ➖ | `LabHistory.razor` · overlay + Back |

### [/history-in-overlays](https://unpoly.com/history-in-overlays) — 7/7 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | When overlays update history | ➖ | `SizePicker.razor` opened as modal |
| 2 | Configuring history visibility | ➖ `[up-history]` on the layer | `LabHistory.razor` · `up-history` on the layer |
| 3—4 | Behaviour with visible / invisible history | ➖ | `LabHistory.razor` · both variants side by side |
| 5 | Navigation bars work with invisible history | ➖ | `LabHistory.razor` · `[up-nav]` with history off |
| 6 | Invisible history is inherited | ➖ | `LabHistory.razor` · nested overlay |
| 7 | History restoration | ➖ | `LabHistory.razor` · closing restores the root URL |

The server plays no part in deciding what shows in the address bar. Its only obligation is
the one this project already follows: **every route renders a full page**, so an overlay URL
opened directly still works.

### [/analytics](https://unpoly.com/analytics)

Not read — tracking page views with a third-party script. Revisit in Phase G with
`up.compiler`.

---

## `up.viewport`

### [/infinite-scrolling](https://unpoly.com/infinite-scrolling)

| Concept | Status | Sample |
|---|---|---|
| Append the next page instead of replacing | ✅ `.listing:after` | `Shop.razor` · `up-target=".listing:after, .more"` |
| Replace the trigger in the same response | ✅ target **list**, two jobs at once | same |
| `[up-defer=reveal]` as the trigger | ➖ | `Shop.razor` · `up-defer="reveal"` |

This closes the `:before` / `:after` row that had sat at `todo` since Phase A. It also
exposed a sample bug worth keeping: a link with `[up-defer=reveal]` that is *also* clickable
loads the same page twice and appends it twice. Deferred is a trigger, not a decoration — the link now carries `[up-follow=false]`.

### /scrolling · /scroll-tuning · /focus · /focus-visibility

Not read. Expected to be entirely client-side; confirm before ticking.

---

## `up.radio`

### [/flashes](https://unpoly.com/flashes) — 10/10 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Placing flashes into the layout | ✅ but see below | `MainLayout.razor` · empty `[up-flashes]` |
| 2 | Flashes inside the main element | ➖ | `ProductDetail.razor` |
| 3 | Rendering flash messages | ✅ | `ProductDetail.razor` · `TakeFlash()` |
| 4 | Flashes are targeted automatically | ➖ Unpoly finds them | verified in the browser |
| 5 | Flashes from closing overlays show on a parent layer | ➖ | `Lab.razor` · noted with `/flashes` §5 |
| 6 | Clearing flashes | ✅ read-once | `Cart.cs` · `TakeFlash` |
| 7 | Removing messages after a delay | ➖ CSS/JS | `Lab.razor` · timed removal button |
| 8 | Caching considerations | ➖ | `Lab.razor` · noted with `/flashes` §8 |
| 9 | Suppressing cached flashes | ➖ | `Lab.razor` · noted with `/flashes` §9 |
| 10 | Building a custom flashes container | ➖ | `MainLayout.razor` |

**The Blazor constraint this exposed.** The guide says put `[up-flashes]` in the layout. In
Blazor static SSR the layout's render tree is built **before** the child page's form handler
runs, and markup position does not change that — below `@Body` is no better than above it.
A layout that renders the message shows the state from *before* the submission.

So the layout holds an **empty** container (harmless: an empty `[up-flashes]` does not clear
existing messages) and the **page** renders the message. Read it into a local, too: a
property that clears as it reads, evaluated once in the `@if` and once in the body, renders
an empty message.

### [/polling](https://unpoly.com/polling) — 9/9 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Basic example | ✅ | `ProductDetail.razor` · `up-poll` |
| 2 | Controlling the reload interval | ➖ `[up-interval]` | `ProductDetail.razor` · `4000` |
| 3 | Controlling the source URL | ➖ `[up-source]` | `Lab.razor` · `[up-source]` |
| 4 | The target selector is derived | ➖ needs a derivable selector | `ProductDetail.razor` |
| 5 | Handling failed responses | ➖ | `Lab.razor` · `/lab/slow?case=pollfail` (503) |
| 6 | Polling is paused in the background | ➖ | n/a — a headless tab is never backgrounded |
| 7 | Skipping updates on the client | ➖ `up:fragment:loaded` | `Lab.razor` · `event.skip()` |
| 8 | **Saving bandwidth when nothing changed** | ✅ `UpNotModified` — 304 | `ProductDetail.razor` |
| 9 | Stopping polling | ➖ `[up-poll=false]` | `Lab.razor` · `[up-poll=false]` |

§8 is where Phase B pays off: `[up-poll]` echoes the fragment's `[up-etag]` as
`If-None-Match`, and an unchanged poll costs a 304 with no body. Verified in the browser —
statuses seen for the polled URL are `[200, 304]`.

### [up-hungry](https://unpoly.com/up-hungry)

| Concept | Status | Sample |
|---|---|---|
| Update a region from **any** response, untargeted | ✅ | `MainLayout.razor` · `#cart-badge` |

Three rules, each found by a failing check rather than by reading:

1. **Never inside skippable chrome.** Unpoly does *not* add hungry selectors to
   `X-Up-Target`, so `UpChrome`'s `Provides` never fires for one. Inside the chrome it is
   stripped from exactly the responses that would have updated it.
2. **It needs a derivable selector** — `[id]` or `[up-id]`, not a bare class. Without one
   Unpoly cannot name the element, so it cannot swap it.
3. **It must not depend on a page handler's effect** — see the flashes note. The badge swapped
   correctly and showed a stale number, which looks exactly like `[up-hungry]` being broken.

---

## `up.status`

### [/feedback-classes](https://unpoly.com/feedback-classes) — 9/9 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Feedback when following links | ➖ `.up-active` on the link | `app.css` |
| 2 | Classes are removed when the request ends | ➖ | verified in the browser |
| 3 | Conveying feedback with CSS styles | ➖ | `app.css` |
| 4 | Feedback when submitting forms | ➖ | `Login.razor` |
| 5 | Fields can be active origins | ➖ | `Login.razor` · `up-validate` |
| 6 | **Feedback during cache revalidation** | ➖ `.up-revalidating`; `.up-loading` and `.up-active` are **not** set then | `app.css` |
| 7 | Feedback classes from JavaScript | ➖ | `Lab.razor` · `up.status({ feedback })` |
| 8 | Custom feedback classes | ➖ | `Lab.razor` · `[up-active-class]`, `[up-loading-class]` |
| 9 | Disabling feedback classes | ➖ | `Lab.razor` · `[up-feedback="false"]` |

### [/loading-state](https://unpoly.com/loading-state) — 8/8 sections

Index page for the rest of `up.status`: styling loading elements, placeholders, arbitrary
status effects, optimistic rendering, disabling forms, the global progress bar, severe
network problems. **Entirely client-side.** Each is covered under its own guide below or in
Phase B.

### [/placeholders](https://unpoly.com/placeholders) — 7/7 sections

| Concept | Status | Sample |
|---|---|---|
| `[up-placeholder]` shows instantly, without waiting for the server | ➖ | `Lab.razor` · `up-placeholder` |
| From JavaScript, templates, dynamic templates, arbitrary logic, overlays | ➖ | `Lab.razor` · `up-placeholder="#lab-placeholder"` |

### [/previews](https://unpoly.com/previews) — 11/11 sections

A preview is a temporary DOM change made *before* the server answers, reverted when the
response arrives. **The server plays no part at all** — all eleven sections are client-side.

| Concept | Status | Sample |
|---|---|---|
| `[up-preview]` naming a preview function | ➖ | `app.js` · `up.preview('lab-skeleton')` |
| Mutating the DOM, context, parameters, delaying, multiple previews | ➖ | `Lab.razor` · chained previews, params, `[up-late-delay]` |

### [/optimistic-rendering](https://unpoly.com/optimistic-rendering) — 5/5 sections

Client renders the expected result immediately; the server then confirms or replaces it.
The server's part is the ordinary one: it validates and returns the authoritative
state. Nothing new on the wire.

| Concept | Status | Sample |
|---|---|---|
| Suitable use cases, previewing submissions, templates, validation errors | ➖ | `app.js` · `lab-optimistic` clones `#optimistic-row` |

---

## `up.script`

### [/enhancing-elements](https://unpoly.com/enhancing-elements) — 9/9 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Registering compilers | ➖ | `wwwroot/js/app.js` · `up.compiler` |
| 2 | Avoid `DOMContentLoaded` | ➖ compilers run for later fragments too | verified: survives 3 swaps |
| 3 | Integrating JavaScript libraries | ➖ | `wwwroot/js/gallery.js` |
| 4 | Cleaning up after yourself | ➖ | `app.js` · returned destructor |
| 5 | Element-local effects require no clean-up | ➖ | n/a — the point is the contrast with §6 |
| 6 | **Global effects require a destructor** | ➖ | `gallery.js` · `setInterval` |
| 7 | Alternative ways to register destructors | ➖ `up.destructor()` | `app.js` · return, array, `up.destructor()` |
| 8 | Passing parameters to a compiler | ➖ `[up-data]` | `ProductDetail.razor` |
| 9 | Accessing information about the render pass | ➖ | `app.js` · third compiler argument |

The widget is a stand-in for a third-party library: an imperative `init`/`destroy` API with
a timer inside, knowing nothing about Unpoly. That shape is what breaks under fragment
swaps. Without the returned destructor every swap leaves another timer running against
detached DOM — `Gallery.liveCount()` makes the leak countable, and the check asserts it
stays at 1 after three round trips rather than climbing to 4.

### [/data](https://unpoly.com/data) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Data attributes for simple key/value pairs | ➖ | `ProductDetail.razor` · `data-gallery` |
| 2 | Structured data with `[up-data]` | ➖ relaxed JSON, 2nd compiler argument | `ProductDetail.razor` |
| 3 | Using arbitrary attributes | ➖ | `LabScript.razor` · `data-role`, `data-count` |
| 4 | Using data in an event handler | ➖ | `app.js` · `lab:pinged` listener |
| 5 | Accessing data programmatically | ➖ | `LabScript.razor` · `up.data()` |
| 6 | Overriding data for a render pass | ➖ | `LabScript.razor` · `[up-data]` on the link |
| 7 | Mapping selectors to data | ➖ | `app.js` · `up.compiler('.probe-data', ...)` |
| 8 | Preserving data through reloads | ➖ data does **not** survive a swap unless `[up-keep]` | `LabFragment.razor` · `[up-keep]` keeps data |

### [/handling-asset-changes](https://unpoly.com/handling-asset-changes) — 7/7 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Tracking assets | ➖ remote scripts and stylesheets in `<head>` | `App.razor` · `meta[up-asset]` |
| 2 | Handling new asset versions | ➖ `up:assets:changed` | `app.js` · `up:assets:changed` |
| 3 | Notifying the user of new app versions | ➖ | `app.js` · flash on assets changed |
| 4 | Reloading the app at the next opportunity | ➖ | `app.js` · opt-in reload button, not a hijack |
| 5 | Loading new assets | ➖ | `LabScript.razor` · `?v=2` |
| 6 | Detecting new versions without a user interaction | ➖ | `app.js` · listener fires on any render |
| 7 | Detecting changes in backend code | ➖ | n/a — only `<head>` assets are tracked |

**Resolved, and it was a real defect.** Cutting `<head>` on fragment responses switched asset
detection off entirely: *"Unpoly only tracks assets in the `<head>`. Elements in the `<body>`
are never tracked."* So `up:assets:changed` could never fire for a fragment, silently, for
five phases.

The fix is one `<meta name="app-version" up-asset>` placed **outside** `<UpChrome>`. About a
hundred bytes buys the feature back. This was carried as "decide in Phase G" since Phase B;
the answer was not to accept the trade but to notice it was avoidable.

### [/script-security](https://unpoly.com/script-security) — 12/12 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1—5 | Callbacks: what is allowed, strict CSP, nonces, replacing with listeners | ➖ | `LabScript.razor` · noted; needs a CSP to differ |
| 6 | **Script elements** | ➖ a `<script>` in a swapped fragment **does** run | see the note below |
| 7—10 | Nonces on scripts, `strict-dynamic`, rewriting, the document nonce | ➖ | `LabScript.razor` · noted; needs a CSP |
| 11 | How responses are altered | ➖ | `LabScript.razor` · inline script in a fragment does run |
| 12 | Only `script-src` is supported | ➖ | n/a |

`up.script.config.scriptElementPolicy` defaults to `'auto'`: scripts run unless a CSP stops
them. That is why the next guide matters.

### [/legacy-scripts](https://unpoly.com/legacy-scripts) — 4/4 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Migrating legacy JavaScripts | ➖ | `app.js` |
| 2 | Migrating legacy scripts to a compiler | ➖ | `app.js` |
| 3 | Migrating screen-specific scripts | ➖ inline script → attribute + `[up-data]` | `ProductDetail.razor` |
| 4 | **Avoid application scripts in `<body>`** | ✅ fixed | `App.razor` · `<script defer>` in head |

§4 named a defect this sample had. `onSizeChosen` was an inline `<script>` inside
`ProductDetail`'s body, so it re-executed on every swap of that region. Moved to a deferred
script in `<head>`, where it is defined once.


---

## `up.util`

Reference-heavy, but two of its guides describe formats **this library writes**, which is why
"skip up.util" was the wrong call.

### [/relaxed-json](https://unpoly.com/relaxed-json) — 4/4 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Example | ➖ | n/a — structural row |
| 2 | **Syntax rules** | ➖ single quotes, unquoted keys, trailing commas | `Lab.razor` · `up-data="{ source: 'lab', relaxed: true, }"` |
| 3 | Postel's law | ✅ see below | `UpResponse.cs` · class doc |
| 4 | Parsing relaxed JSON | ➖ `up.util.parseRelaxedJSON()`; backends use a JSON5 parser | n/a here — nothing sends us relaxed JSON |

**Why the library needs no JSON5 writer.** Several headers are documented as taking *relaxed*
JSON, which looked like a gap for months. It is not: *"Every JSON object is also a Relaxed
JSON object."* Strict JSON is a subset, so ordinary `JsonSerializer` output is always
accepted. The permissiveness exists for humans hand-writing HTML attributes, not for servers.

The reverse would matter: if a client ever sent us relaxed JSON in `X-Up-Context`, `System.Text.Json`
would reject it. It does not: Unpoly serialises context with `JSON.stringify`.

### [/url-patterns](https://unpoly.com/url-patterns) — 5/5 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Matching an exact URL | ✅ | `ProductDetail.razor` · `up-accept-location` |
| 2 | Matching with wildcards (`*` any, `$` digits) | ✅ | `Shop.razor` · `"/shop*, /p/*"` |
| 3 | Multiple alternatives, space or comma separated | ✅ | same |
| 4 | **Excluding with `-`** | ✅ | `Shop.razor` · `-/shop/secret` |
| 5 | Capturing named segments (`:name`, `$id`) | ➖ the capture becomes the accept value | `ProductDetail.razor` · `/p/$id/size` |

`UpExpireCache` and `UpEvictCache` take these, not plain globs — documented on the methods now.
`[up-accept-location]` takes them too, which is how an overlay closes on a redirect without
any JavaScript callback.

---

## `up.motion`

No server side at all. Included because "no C# to write" is not a reason to leave a module
undocumented — the same mistake that hid seven target-syntax variants for three phases.

### [/predefined-transitions](https://unpoly.com/predefined-transitions) — 3/3 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Predefined transitions: `cross-fade`, `move-up`, `move-down`, `move-left`, `move-right`, `none` | ➖ | `Lab.razor` · four of them |
| 2 | Combining animations (`move-to-bottom`/`fade-in` pairs) | ➖ | `Lab.razor` · `up-transition="move-to-left/move-from-right"` |
| 3 | Custom transitions | ➖ `up.transition()` | `app.js` · `up.transition('lab-slide')` |

### [/predefined-animations](https://unpoly.com/predefined-animations)

| Concept | Status | Sample |
|---|---|---|
| `fade-in`, `move-to-left`, `move-from-top`, ... | ➖ used when opening overlays | `SizePicker.razor` · `UpOpenLayer(animation)` |

### [/motion-tuning](https://unpoly.com/motion-tuning) — 3/3 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Changing the duration — `up.motion.config.duration`, default **175ms** | ➖ | `Lab.razor` · `up-duration="600"` |
| 2 | Easing | ➖ | `Lab.razor` · `[up-easing]` |
| 3 | **Disabling animation globally** — `up.motion.config.enabled = false` | ➖ the guide names automated testing as the reason | `Lab.razor` · `up-transition="none"` per link |

---

## `up.element`

No guides — reference only. Low-level DOM helpers.

| Feature | Status | Sample |
|---|---|---|
| `affix()` `attr()` `createFromHTML()` `hide()`/`show()` `setAttrs()` `style()` | ➖ | n/a — nothing here needs raw DOM utilities |

---

## `up.framework`

No guides — reference only.

| Feature | Status | Sample |
|---|---|---|
| `up.framework.booted` `isSupported()` | ➖ | `Lab.razor` · framework status button |
| `up.boot()` and `[up-boot=manual]` | ➖ defers booting until called | `Lab.razor` · framework status button |
| `up:framework:booted` | ➖ | `Lab.razor` · framework status button |

`[up-boot=manual]` is the hook worth remembering for Blazor: it is how Unpoly would be made
to start after some other initialisation, rather than on its own.

---

## `up.log`

No guides — reference only.

| Feature | Status | Sample |
|---|---|---|
| `up.log.enable()` / `disable()` | ➖ prints every render step to the console | `Lab.razor` · enable button |
| `up.log.config` | ➖ | `Lab.razor` · `labLogConfig()` |

---

## Implemented but never exercised

Passes unit checks, never runs in the sample. Gaps in the *lab*, not the library.

| Not exercised | Where it fits |
|---|---|
| `UpEvictCache()` `UpKeepCache()` | need a cart — Phase D or F |
| `:before` / `:after` | infinite scroll — Phase E |
| `up.network.config.fail` widening (§3 of /failed-responses) | a 401 route |

Closed so far: `UpRetarget`, `UpExpireCache`, `WantsNothing`/`:none`, explicit
`[up-target]`, `[up-fallback]`, `Catalog.Touch()`, and — through the target-syntax
playground on the home page — target lists, `:maybe`, `:content`, `:origin`, missing-target
fallback, `[up-instant]` and `[up-preload]`.

The playground paid for itself immediately: its `.content, .site-nav` link exposed that
`UpChrome` broke any target living inside the chrome.

---

## Not opened, by decision


**Every one of Unpoly's 17 modules now has a section.** The five once listed as "skipped"
— `up.motion`, `up.element`, `up.util`, `up.log`, `up.framework` — are documented above.
Dropping `up.util` in particular was a mistake: two of its guides describe formats this
library writes.

The *Features* tier remains a dictionary rather than reading material. Look things up in it
**before** writing glue; the CSRF listener is what happens otherwise.
