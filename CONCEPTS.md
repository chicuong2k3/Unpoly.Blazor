# CONCEPTS.md

Per-guide tracking: which concepts from each Unpoly guide are handled, where the code lives,
and **where the sample exercises them**.

`TASKS.md` tracks *what to do next*. `VERIFY.md` tracks *how you prove it works*. This file
tracks *what has been understood and covered*, guide by guide.

**Legend**

| | |
|---|---|
| ✅ | covered — the code exists and a check or a wire observation proves it |
| ⬜ | not covered yet — the phase that will cover it is named |
| ➖ | nothing for **us** to write — pure client-side. Still goes into the skill's "reach for HTML first" table, because it is exactly where an agent invents a C# helper for a one-attribute job |
| 🚫 | deliberately declined — the reason is stated |

The **Sample** column points into `sample/Jubin/`. Line numbers drift; the token after the
dot is what to grep for. A dash means *the sample does not exercise this yet* — see
[Implemented but never exercised](#implemented-but-never-exercised).

Rows are added as guides are read. An empty guide section means it has not been read yet.

---

## `up.link`

### [/handling-everything](https://unpoly.com/handling-everything)

| Concept | Config | Status | Sample |
|---|---|---|---|
| Following all links | `followSelectors.push('a[href]')` | ✅ `HandleAllLinksAndForms`, default **on** | `Program.cs:8` · `AddUnpoly` |
| Following on mousedown | `instantSelectors.push('a[href]')` | ✅ `InstantAllLinks`, default **off** | — |
| Preloading all links | `preloadSelectors.push('a[href]')` | ✅ `PreloadAllLinks`, default **off** | — |
| Handling all forms | `submitSelectors.push(['form'])` | ✅ same option | `Login.razor:22` · `EditForm` |
| Opt-outs `[up-follow=false]` etc. | — | ➖ attributes only | — |
| Fixing legacy JavaScript | — | ➖ Phase G territory | — |
| Customizing navigation defaults | `navigateOptions` | ➖ `ExtraScript` already carries it | — |

Navigation applies defaults `up.render()` does not — `history: 'auto'`, `scroll: 'auto'`,
`fallback: ':main'`, `cache: 'auto'`, `revalidate: 'auto'`, `focus: 'auto'`,
`peel: 'dismiss'`. Override through `ExtraScript`:

```csharp
o.ExtraScript = "up.fragment.config.navigateOptions.transition = 'cross-fade'";
```

Both new flags default **off** on purpose:

- `instant` changes click semantics. A link that needs a confirm, or that users drag rather
  than click, must opt out.
- `preload` multiplies server load. On a product grid, sweeping the mouse across the cards
  fires one request per card, into the same cache that revalidation later refetches.

### [/targeting-fragments](https://unpoly.com/targeting-fragments)

| Concept | Status | Sample |
|---|---|---|
| `[up-target]` with a CSS selector | ✅ | — *(every link relies on `mainTargets` instead)* |
| `mainTargets` fallback | ✅ `UnpolyOptions.MainTargets` | `Program.cs:11` · `MainTargets` |
| The target element itself | ✅ never wrapped in chrome | `MainLayout.razor:30` · `class="content"` |
| Target **lists** (`.a, .b`) | ✅ `UpTargets()`; checked | — |
| Pseudo-target `:main` | ✅ whole-page; checked | — |
| Pseudo-target `:layer` | ✅ whole-page; checked | — |
| Pseudo-target `:none` | ✅ `WantsNothing()`; checked | — |
| Pseudo-target `:origin` | ➖ resolved to a derived selector before the header is sent | — |
| Modifier `:before` / `:after` | ✅ stripped by `BaseTarget()`; checked | — |
| Modifier `:maybe` | ✅ stripped by `BaseTarget()`; checked | — |
| Modifier `:content` | ✅ stripped by `BaseTarget()`; checked | — |
| Server retargeting | ✅ `UpRetarget()` | — |
| `[up-fallback]` | ➖ resolved entirely on the client | — |

`BaseTarget()` exists because modifiers change **how** a match is applied, never **what** is
matched. Comparing raw strings made `body:after` look like a fragment, which dropped the
chrome from a request that wanted to append into `<body>`.

### [/failed-responses](https://unpoly.com/failed-responses)

| Concept | Status | Sample |
|---|---|---|
| Failure is any status **outside 2xx and 304** | ✅ so Phase B's 304 never trips a fail target | `Login.razor:83` · `Status422` |
| `X-Up-Fail-Target` read and split | ✅ `UpFailTarget()`, `UpFailTargets()` | `Login.razor:23` · `up-fail-target` |
| `IsUpFragment()` accounts for the failure branch | ✅ chrome kept if **either** branch is whole-page | `MainLayout.razor:10` · `UpChrome` |
| Answering 422 so the fail target is used | ✅ | `Login.razor:83` · `Invalid()` |
| A separate response header to retarget on failure | ➖ none exists — one `X-Up-Target` overrides both | — |
| `[up-fail-target]` on the element | ➖ client-side | `Login.razor:23` |

---

## `up.protocol`

### [/optimizing-responses](https://unpoly.com/optimizing-responses)

| Concept | Status | Sample |
|---|---|---|
| Shorten the response to what matches the target | ✅ `UpChrome` | `App.razor:11` head · `MainLayout.razor:10,34` chrome |
| `Vary` on the headers that changed the body | ✅ `UpVary()` + middleware; 3 checks | `Program.cs:36` · `UseUnpoly` |
| Optimisation is optional; full documents stay valid | ✅ no fragment-only endpoints exist here | — |

Measured on `/p/dam-4`: full page 1865 bytes, fragment 459.

### [/up.protocol.config](https://unpoly.com/up.protocol.config)

| Concept | Status | Sample |
|---|---|---|
| `csrfHeader` | ✅ from `AntiforgeryHeaderName` | `App.razor:23` · `UnpolyHead` |
| `csrfToken` | ✅ from `IAntiforgery.GetAndStoreTokens` | same |
| `csrfParam` | ➖ `EditForm` renders its own hidden input | `Login.razor:22` |
| `methodParam` (`_method`) | ⬜ **Phase E**, with the `_up_method` cookie | — |
| `maxHeaderSize` | ➖ observed through `:unknown` | `Login.razor:64` · `IsUpValidatingUnknown` |

### [/conditional-requests](https://unpoly.com/conditional-requests)

The URL is `/conditional-requests`; `/conditional-responses` (as `up.protocol` links it) 404s.

| Concept | Status | Sample |
|---|---|---|
| `ETag` published by the server | ✅ `UpNotModified(etag:)` | `Shop.razor:31` · `Catalog.cs:46` |
| `Last-Modified` published | ✅ truncated to whole seconds | `Shop.razor:31` · `Catalog.cs:44` |
| `If-None-Match` compared (`W/`, lists, `*`) | ✅ checked | same |
| `If-Modified-Since` compared | ✅ checked | same |
| 304 with an empty body | ✅ on the wire: `/shop` 9194 to **0** bytes | same |
| Serves reload, revalidation and polling alike | ✅ one code path | same |
| Fragment-level `[up-etag]` / `[up-time]` | ➖ HTML attributes | — |

---

## `up.network`

### [/caching](https://unpoly.com/caching)

| Concept | Status | Sample |
|---|---|---|
| GET cached, expiring after **15 s** (`cacheExpireAge`) | ➖ client-side default | — |
| Revalidation: render cached, refetch, render again | ⬜ **not reproduced** — needs a browser | `Program.cs:28` · request log |
| A non-GET expires the **entire** cache automatically | ➖ Unpoly does it unprompted | — |
| `X-Up-Expire-Cache` with a URL glob or `*` | ✅ `UpExpireCache()` | — |
| `X-Up-Expire-Cache: false` | ✅ `UpKeepCache()` | — |
| `X-Up-Evict-Cache` | ✅ `UpEvictCache()` | — |
| `X-Up-Clear-Cache` | 🚫 in no current guide. Use expire or evict | — |

Because a non-GET already clears everything, `UpExpireCache` is for the *narrower* case —
expiring a subset, or expiring from a GET. Calling it after an ordinary POST is redundant,
and the sample deliberately does not.

### [/progress-bar](https://unpoly.com/progress-bar)

| Concept | Status | Sample |
|---|---|---|
| Bar shown after `lateDelay` | ➖ on by default | — |
| `progressBar = false` to replace it | ➖ via `ExtraScript` | — |
| `[up-background]`; preload and poll are background | ➖ | — |

### [/up:request:load](https://unpoly.com/up:request:load)

| Concept | Status | Sample |
|---|---|---|
| Mutating `event.request.headers` before send | 🚫 **declined** — it injected the CSRF token, which `up.protocol.config` already does. Nine lines deleted | — |

---

## `up.form`

### [/validation](https://unpoly.com/validation)

| Concept | Status | Sample |
|---|---|---|
| `X-Up-Validate` marks a validation-only request | ✅ `IsUpValidating()` | `Login.razor:59,73,83` |
| Fields **space** separated, batched into one request | ✅ `UpValidatingFields()`; checked | `Login.razor:66` |
| `:unknown` — non-field origin **or** `maxHeaderSize` overflow | ✅ `IsUpValidatingUnknown()` | `Login.razor:64` |
| The handler must not persist while validating | ✅ the guard is the whole point of the header | `Login.razor:73` · `Submit()` |
| Server renders a fresh form state; the form group is swapped | ➖ `[up-validate]` + `[up-form-group]` | `Login.razor:26,28,32,34` |

### [/submitting-forms](https://unpoly.com/submitting-forms)

| Concept | Status | Sample |
|---|---|---|
| Forms submit through Unpoly | ✅ `submitSelectors` | `Login.razor:22` |
| Antiforgery on a real POST | ✅ valid token 200/422, missing token **400** | `Login.razor:22` · `FormName` |

### [/disabling-forms](https://unpoly.com/disabling-forms)

| Concept | Status | Sample |
|---|---|---|
| `[up-disable]` while in flight | ➖ client-side | `Login.razor:23` · `app.css:274` |

### [/switching-form-state](https://unpoly.com/switching-form-state) · [/watch-options](https://unpoly.com/watch-options)

Not read — `[up-switch]`, `[up-autosubmit]`, `[up-watch-delay]` are client-side. Nothing to
implement; revisit only if a server-side need appears.

### [/reactive-server-forms](https://unpoly.com/reactive-server-forms)

Not read yet.

---

## `up.status`

### [/feedback-classes](https://unpoly.com/feedback-classes) · [/navigation-bars](https://unpoly.com/navigation-bars)

| Concept | Status | Sample |
|---|---|---|
| `[up-nav]` keeps `.up-current` in sync without swapping the nav | ➖ client-side | `MainLayout.razor:18` · `app.css:83` |
| `.up-active` on the link or form in flight | ➖ | `app.css:90` |
| `.up-loading` on the fragment awaiting replacement | ➖ | `app.css:93` |

Full `up.status` coverage is Phase F; these three are styled early so Unpoly's feedback is
visible in the browser rather than only in the docs.

---

## `up.script`

### [/handling-asset-changes](https://unpoly.com/handling-asset-changes)

| Concept | Status | Sample |
|---|---|---|
| Unpoly diffs scripts and stylesheets in `<head>` | 🚫 **broken here on purpose** — cutting `<head>` leaves nothing to diff | `App.razor:11` |
| `up:assets:changed` has no default behaviour | ⬜ **Phase G** — decide whether to re-emit `[up-asset]` | — |

---

## Implemented but never exercised

Covered by unit checks, never seen on the wire. Each is a gap in the *lab*, not in the
library — and a candidate exercise when the matching sample feature is built.

| Not exercised | Where it would fit |
|---|---|
| `UpRetarget()` | login success: retarget `.site-nav` to swap in a greeting link |
| `UpExpireCache()` `UpEvictCache()` `UpKeepCache()` | needs a cart, so Phase D or F |
| `WantsNothing()` / `:none` | a fire-and-forget POST such as a "recently viewed" ping |
| Explicit `[up-target]` on a link | every link currently falls through to `mainTargets` |
| Target lists, `:before` `:after` `:maybe` `:content` | `.tasks:after` fits an infinite-scroll list — Phase E |
| `InstantAllLinks` `PreloadAllLinks` | both off; turn on and watch the request log |
| `Catalog.Touch()` | nothing mutates the catalog yet |

That last one matters: the conditional-request path has only been proven in its *fresh*
direction. Nothing has yet changed the data and confirmed the 304 stops.

---

## Not yet opened

`up.fragment` · `up.layer` · `up.radio` · `up.history` · `up.viewport` · `up.event`

Skipped for the whole project: `up.motion` · `up.element` · `up.util` · `up.log` ·
`up.framework`, plus the *Features* tier everywhere — that tier is a dictionary. Look
things up in it **before** writing glue; the CSRF listener above is what happens otherwise.
