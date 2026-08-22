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
| `todo` | demonstrable, not built yet — the work is listed in `TASKS.md` |

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
| 5 | Fixing legacy JavaScript code | ➖ Phase G territory | todo — Phase G, needs a legacy script worth fixing |
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
| 5 | Targeting the entire layer | ✅ `:layer` is whole-page; checked | todo — Phase D |
| 6 | Targeting an element object | ➖ JS API | `Lab.razor` · `up.render({ target: element })` |
| 7 | Appending or prepending children | ✅ `:after` / `:before` stripped; checked | todo — infinite scroll, Phase E |
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
| 5 | Handling unexpected content | ➖ `up:fragment:loaded` | todo — needs a route that returns the wrong shape |
| 6 | Handling fatal network errors | ➖ client-side | todo — needs the server killed mid-request |
| 7 | Handling aborted requests | ➖ client-side | todo — needs two overlapping navigations |
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

## `up.protocol`

### [/optimizing-responses](https://unpoly.com/optimizing-responses) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Optimizing responses | ✅ optional; full documents stay valid | n/a |
| 2 | Omitting content that isn't targeted | ✅ `UpChrome` | `App.razor:11` head · `MainLayout.razor:10,34` |
| 3 | Example | — | `/p/dam-4`: 1865 → 459 bytes |
| 4 | Note (the `Vary` requirement) | ✅ `UpVary()` + `UseUnpoly()` | `Program.cs:36` |
| 5 | Rendering different content for overlays | ⬜ **Phase D** — needs `X-Up-Mode` | todo — Phase D, needs `X-Up-Mode` |
| 6 | Rendering different content for Unpoly requests | ✅ `IsUnpoly()` | `Program.cs:28` |
| 7 | Example | — | n/a — structural row |
| 8 | Rendering content that depends on layer context | ⬜ **Phase D** — needs `X-Up-Context` | todo — Phase D, needs `X-Up-Context` |

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
| `methodParam` (`_method`) | ⬜ **Phase E**, with the `_up_method` cookie | todo — Phase E, with the `_up_method` cookie |
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
| 9 | Preventing rendering of revalidation responses | ➖ `up:fragment:loaded` | todo — Phase G, needs `up:fragment:loaded` |
| 10 | Detecting revalidation from a compiler | ➖ Phase G | todo — Phase G |
| 11–12 | Enabling / disabling revalidation | ➖ client config | `Lab.razor` · `up-revalidate="false"` |
| 13 | Expiration | ✅ `UpExpireCache()` | `Shop.razor` · `UpExpireCache` |
| 14 | Expiring content after an interaction | ✅ `X-Up-Expire-Cache`, incl. `false` via `UpKeepCache()` | `Shop.razor` · `Refresh()` |
| 15 | Eviction | ✅ `UpEvictCache()` | `Shop.razor` · `UpEvictCache` |
| 16 | Evicting content after an interaction | ✅ same header | `Shop.razor` · `mode=evict` button |
| 17 | Capping memory usage | ➖ `cacheSize` | todo — `cacheSize`; a meaningful demo needs the cache overflowed on purpose |
| 18 | **Caching optimized responses** | ✅ this is why `Vary` is mandatory | `Program.cs:36` · `UseUnpoly` |
| 19 | Example | — | n/a — structural row |
| 20 | **How cache entries are matched** | ✅ keyed by URL, then partitioned by every header named in `Vary` | same |
| 21 | Caching after redirects | ⬜ unread detail — revisit with `X-Up-Location` in Phase E | todo — unread; revisit with `X-Up-Location` in Phase E |

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
| 3 | Validating after submission | ⬜ not exercised — the sample only validates on change | todo — needs a browser: change a field after a 422 and watch it revalidate |
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
| 9 | Overriding render options | ➖ attributes | todo — same `fail` prefix idea, on a form |
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

## `up.status`

### /feedback-classes · /navigation-bars — partial

Read only far enough to style three classes early, so Unpoly's feedback is visible in the
browser rather than only in the docs. Full coverage is **Phase F**.

| Concept | Status | Sample |
|---|---|---|
| `[up-nav]` keeps `.up-current` in sync without swapping the nav | ➖ | `MainLayout.razor:18` · `app.css:83` |
| `.up-active` on the link or form in flight | ➖ | `app.css:90` |
| `.up-loading` on the fragment awaiting replacement | ➖ | `app.css:93` |
| Remaining sections of both guides | ⬜ **Phase F** | todo — Phase F |

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
| 2 | Disabling layer isolation (`any`) | ➖ | todo — needs two overlays open at once |
| 3 | Matching the root layer | ➖ | todo — with a stacked overlay |
| 4 | Matching any overlay | ➖ | todo — same |
| 5 | Matching the current or frontmost layer | ➖ | todo — same |
| 6 | Matching relative to the current layer (`parent`, `closest`, `ancestor`, `child`, `descendant`, `subtree`) | ➖ | todo — same |
| 7 | Matching the layer of a given element | ➖ JS API | todo |
| 8 | Matching a layer by index | ➖ | todo |
| 9 | Using a layer reference | ➖ JS API | todo |
| 10 | Matching in multiple layers | ➖ | todo |
| 11 | Opening new layers (`new`, `swap`, `shatter`) | ✅ `new` used; `swap`/`shatter` untried | `ProductDetail.razor` · `up-layer="new modal"` |
| 12 | Targeting another layer for server errors | ⬜ pairs with `X-Up-Fail-Mode` | todo |

Rows 2—10 all need a **stack** of overlays to mean anything, and the sample opens only one
at a time. That is one exercise, not ten.

### [/opening-overlays](https://unpoly.com/opening-overlays) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Opening an overlay from a link | ✅ | `ProductDetail.razor` · `up-layer` |
| 2 | Choosing the overlay mode | ✅ | `Lab.razor` · modal/drawer/popup/cover |
| 3 | Opening an overlay from a form | ➖ same attribute on a form | todo |
| 4 | Opening overlays from local content | ➖ `[up-content]` | `Lab.razor` · `up-content` |
| 5 | Opening overlays from JavaScript | ➖ `up.layer.open()` | todo |
| 6 | **Opening overlays from the server** | ✅ `UpOpenLayer(options)` — relaxed JSON of render options | `SizePicker.razor` · `?serverOpens=1` |
| 7 | Close conditions | ✅ see /closing-overlays | `SizePicker.razor` |
| 8 | Replacing existing overlays (`swap`, `shatter`) | ⬜ | todo — with a stack |

### [/closing-overlays](https://unpoly.com/closing-overlays) — 13/13 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Distinguishing close intents | ✅ accept ≠ dismiss | `SizePicker.razor` |
| 2 | Running code when an overlay closes | ➖ `[up-on-accepted]` / `[up-on-dismissed]` | `ProductDetail.razor` |
| 3 | Overlay result values (acceptance values, dismissal reasons) | ✅ | `SizePicker.razor` · `UpAcceptLayer` |
| 4 | Close conditions (location, event, fragment, discarded flashes/response) | ➖ `[up-accept-location]` etc | todo |
| 5 | **Closing from the server** | ✅ `UpAcceptLayer` / `UpDismissLayer` | `SizePicker.razor` · `Pick()` |
| 6 | Closing from JavaScript | ➖ | todo |
| 7 | Closing when a button is clicked | ➖ `[up-dismiss]` | `SizePicker.razor` · `up-dismiss` |
| 8 | Closing when a link is followed | ➖ | todo |
| 9 | Closing when a form is submitted | ✅ this is the sample's accept path | `SizePicker.razor` |
| 10 | Closing by targeting a background layer (peeling) | ⬜ | todo — with a stack |
| 11 | Customizing dismiss controls | ➖ CSS/attributes | todo |
| 12 | Close animation | ➖ | todo |
| 13 | Using overlays as promises | ➖ JS API | todo |

Accept and dismiss are not interchangeable. Accept means the sub-task finished and the
parent should continue **with the result**; dismiss means the user backed out and carries a
*reason*, not a value.

### [/subinteractions](https://unpoly.com/subinteractions) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Example | ➖ | n/a — structural row |
| 2 | Starting a subinteraction | ✅ | `ProductDetail.razor` · size picker |
| 3 | Common acceptance callbacks | ➖ | `ProductDetail.razor` · `onSizeChosen` |
| 4 | Reloading on acceptance | ➖ `up.reload` in the callback | todo |
| 5 | Adding options to an existing select | ➖ | todo |
| 6 | Navigating away | ➖ | todo |
| 7 | Reusing existing screens | ✅ the picker is a real route, overlay or not | `SizePicker.razor` |
| 8 | Awaiting subinteractions from JavaScript | ➖ JS API | todo |

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
| 4 | Forcing a history change | ➖ `[up-history=true]` | todo |
| 5 | Preventing a history change | ➖ `[up-history=false]` | `Lab.razor` · `up-history="false"` |
| 6 | Only `GET` requests change history | ➖ | n/a — nothing to observe beyond the rule |
| 7 | Changing history after a form submission | ✅ `UpLocation()` | `Lab.razor` · `/lab/relocated` |
| 8 | Changing history during programmatic rendering | ➖ JS API | todo |
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
| 1 | Default restoration behaviour | ➖ Unpoly refetches or reuses cache | todo — Back after a filter |
| 2 | Custom restoration behaviour | ➖ `up:location:restore` | todo |
| 3 | Handled history entries | ➖ | n/a — internal bookkeeping |
| 4 | History restoration with overlays | ➖ | todo — needs a stack |

### [/history-in-overlays](https://unpoly.com/history-in-overlays) — 7/7 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | When overlays update history | ➖ | `SizePicker.razor` opened as modal |
| 2 | Configuring history visibility | ➖ `[up-history]` on the layer | todo |
| 3—4 | Behaviour with visible / invisible history | ➖ | todo |
| 5 | Navigation bars work with invisible history | ➖ | todo |
| 6 | Invisible history is inherited | ➖ | todo — needs a stack |
| 7 | History restoration | ➖ | todo |

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
| 5 | Flashes from closing overlays show on a parent layer | ➖ | todo — needs an overlay that flashes |
| 6 | Clearing flashes | ✅ read-once | `Cart.cs` · `TakeFlash` |
| 7 | Removing messages after a delay | ➖ CSS/JS | todo |
| 8 | Caching considerations | ➖ | todo |
| 9 | Suppressing cached flashes | ➖ | todo |
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
| 3 | Controlling the source URL | ➖ `[up-source]` | todo |
| 4 | The target selector is derived | ➖ needs a derivable selector | `ProductDetail.razor` |
| 5 | Handling failed responses | ➖ | todo |
| 6 | Polling is paused in the background | ➖ | n/a — a headless tab is never backgrounded |
| 7 | Skipping updates on the client | ➖ `up:fragment:loaded` | todo |
| 8 | **Saving bandwidth when nothing changed** | ✅ `UpNotModified` — 304 | `ProductDetail.razor` |
| 9 | Stopping polling | ➖ `[up-poll=false]` | todo |

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
| 7 | Feedback classes from JavaScript | ➖ | todo |
| 8 | Custom feedback classes | ➖ | todo |
| 9 | Disabling feedback classes | ➖ | todo |

### [/loading-state](https://unpoly.com/loading-state) — 8/8 sections

Index page for the rest of `up.status`: styling loading elements, placeholders, arbitrary
status effects, optimistic rendering, disabling forms, the global progress bar, severe
network problems. **Entirely client-side.** Each is covered under its own guide below or in
Phase B.

### [/placeholders](https://unpoly.com/placeholders) — 7/7 sections

| Concept | Status | Sample |
|---|---|---|
| `[up-placeholder]` shows instantly, without waiting for the server | ➖ | `Lab.razor` · `up-placeholder` |
| From JavaScript, templates, dynamic templates, arbitrary logic, overlays | ➖ | todo |

### [/previews](https://unpoly.com/previews) — 11/11 sections

A preview is a temporary DOM change made *before* the server answers, reverted when the
response arrives. **The server plays no part at all** — all eleven sections are client-side.

| Concept | Status | Sample |
|---|---|---|
| `[up-preview]` naming a preview function | ➖ | todo — Phase G, with `up.compiler` |
| Mutating the DOM, context, parameters, delaying, multiple previews | ➖ | todo |

### [/optimistic-rendering](https://unpoly.com/optimistic-rendering) — 5/5 sections

Client renders the expected result immediately; the server then confirms or replaces it.
The server's part is the ordinary one: it validates and returns the authoritative
state. Nothing new on the wire.

| Concept | Status | Sample |
|---|---|---|
| Suitable use cases, previewing submissions, templates, validation errors | ➖ | todo — Phase G |

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
| 7 | Alternative ways to register destructors | ➖ `up.destructor()` | todo |
| 8 | Passing parameters to a compiler | ➖ `[up-data]` | `ProductDetail.razor` |
| 9 | Accessing information about the render pass | ➖ | todo |

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
| 3 | Using arbitrary attributes | ➖ | todo |
| 4 | Using data in an event handler | ➖ | todo |
| 5 | Accessing data programmatically | ➖ | todo |
| 6 | Overriding data for a render pass | ➖ | todo |
| 7 | Mapping selectors to data | ➖ | todo |
| 8 | Preserving data through reloads | ➖ data does **not** survive a swap unless `[up-keep]` | todo |

### [/handling-asset-changes](https://unpoly.com/handling-asset-changes) — 7/7 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Tracking assets | ➖ remote scripts and stylesheets in `<head>` | `App.razor` · `meta[up-asset]` |
| 2 | Handling new asset versions | ➖ `up:assets:changed` | todo |
| 3 | Notifying the user of new app versions | ➖ | todo |
| 4 | Reloading the app at the next opportunity | ➖ | todo |
| 5 | Loading new assets | ➖ | todo |
| 6 | Detecting new versions without a user interaction | ➖ | todo |
| 7 | Detecting changes in backend code | ➖ | todo |

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
| 1—5 | Callbacks: what is allowed, strict CSP, nonces, replacing with listeners | ➖ | todo — needs a CSP |
| 6 | **Script elements** | ➖ a `<script>` in a swapped fragment **does** run | see the note below |
| 7—10 | Nonces on scripts, `strict-dynamic`, rewriting, the document nonce | ➖ | todo |
| 11 | How responses are altered | ➖ | todo |
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

## Not yet opened

`up.fragment` · `up.event`

Skipped for the whole project: `up.motion` · `up.element` · `up.util` · `up.log` ·
`up.framework`, plus the *Features* tier everywhere — that tier is a dictionary. Look
things up in it **before** writing glue; the CSRF listener above is what happens otherwise.
