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
| `n/a` | nothing a sample could show — a JS-only API, or a structural row |
| `todo` | demonstrable, not built yet — the work is listed in `TASKS.md` |

"No C# side" (➖) does **not** imply `n/a`. The sample is a web app: most client-side
concepts are demonstrable in its markup, and treating ➖ as "nothing to show" is how seven
target-syntax variants went unexercised until a playground was added.

---

## `up.link`

### [/handling-everything](https://unpoly.com/handling-everything) — 6/6 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Following all links | ✅ `HandleAllLinksAndForms`, default **on** | `Program.cs:8` · `AddUnpoly` |
| 2 | Following all links on mousedown | ✅ `InstantAllLinks`, default **off** | `Home.razor` · `up-instant` |
| 3 | Preloading all links | ✅ `PreloadAllLinks`, default **off** | `Home.razor` · `up-preload` |
| 4 | Handling all forms | ✅ same option | `Login.razor:22` · `EditForm` |
| 5 | Fixing legacy JavaScript code | ➖ Phase G territory | — |
| 6 | Customizing navigation defaults | ➖ `ExtraScript` carries `navigateOptions` | — |

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
| 6 | Targeting an element object | ➖ JS API | n/a |
| 7 | Appending or prepending children | ✅ `:after` / `:before` stripped; checked | todo — infinite scroll, Phase E |
| 8 | Replacing all children | ✅ `:content` stripped; checked | `Home.razor` · playground |
| 9 | Targeting nothing | ✅ `WantsNothing()` → 204, 0 bytes | `ProductDetail.razor` · `up-target=":none"` |
| 10 | Resolving ambiguous selectors | ➖ client-side matching | n/a |
| 11 | Targeting an ancestor element | ➖ selector syntax | n/a |
| 12 | Targeting a sibling element | ➖ selector syntax | n/a |
| 13 | Disabling region-aware matching | ➖ client config | n/a |
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
| 1 | Rendering failed responses differently | ✅ `UpFailTarget()`, `UpFailTargets()` | `Login.razor:23` · `up-fail-target` |
| 2 | Ignoring HTTP error codes | ➖ `{ fail: false }` client-side | — |
| 3 | **Customizing failure detection** | ⬜ **server-relevant** — `up.network.config.fail` is a function the app can widen, e.g. treat a response header as failure. Reachable today through `ExtraScript`; no C# helper yet | — |
| 4 | Local content cannot fail | ➖ no request involved | — |
| 5 | Handling unexpected content | ➖ `up:fragment:loaded` | — |
| 6 | Handling fatal network errors | ➖ client-side | — |
| 7 | Handling aborted requests | ➖ client-side | — |
| 8 | Detecting a failed response programmatically | ➖ JS API | — |

Failure is any status **outside 2xx and 304** — so Phase B's conditional 304 never trips a
fail target. `IsUpFragment()` keeps chrome when *either* branch names a whole-page target,
because the layout renders chrome before the page has picked a status.

§3 is the one with a server story: the server can emit a header and the client be
configured to treat it as failure:

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
| 5 | Rendering different content for overlays | ⬜ **Phase D** — needs `X-Up-Mode` | — |
| 6 | Rendering different content for Unpoly requests | ✅ `IsUnpoly()` | `Program.cs:28` |
| 7 | Example | — | — |
| 8 | Rendering content that depends on layer context | ⬜ **Phase D** — needs `X-Up-Context` | — |

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
| `methodParam` (`_method`) | ⬜ **Phase E**, with the `_up_method` cookie | — |
| `maxHeaderSize` | ➖ observed through `:unknown` | `Login.razor:64` |

### [/conditional-requests](https://unpoly.com/conditional-requests) — 7/7 sections

The URL is `/conditional-requests`; `/conditional-responses` (as `up.protocol` links it) 404s.

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Conditional requests | ✅ one code path serves reload, revalidation and polling | `Shop.razor:31` |
| 2 | Content changed from a known hash | ✅ `If-None-Match`, incl. `W/`, lists, `*` | `Catalog.cs:46` · `ETag` |
| 3 | Content newer than a known time | ✅ `If-Modified-Since`, truncated to whole seconds | `Catalog.cs:44` · `LastModified` |
| 4 | Modification time or content hash? | — both supported; the sample sends both | `Shop.razor:31` |
| 5 | Individual versions per fragment | ➖ `[up-etag]` / `[up-time]` attributes | — |
| 6 | Removing versions for a fragment | ➖ attribute set to `false` | — |
| 7 | Resources | — | — |

304 with an empty body, verified on the wire: `/shop` 9194 → **0** bytes.

---

## `up.network`

### [/caching](https://unpoly.com/caching) — 21/21 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Enabling caching | ➖ `cache: 'auto'` during navigation | — |
| 2–6 | Disabling caching (globally / per route / per link / per call) | ➖ client config and `[up-cache=false]` | — |
| 7 | **Revalidation** | ⬜ **not reproduced** — needs a browser | `Program.cs:28` · request log |
| 8 | When nothing changed | ✅ 304 path, now proven in **both** directions — after `Catalog.Touch()` the old ETag returns 200 again | `Shop.razor` · `UpNotModified` · `Refresh()` |
| 9 | Preventing rendering of revalidation responses | ➖ `up:fragment:loaded` | — |
| 10 | Detecting revalidation from a compiler | ➖ Phase G | — |
| 11–12 | Enabling / disabling revalidation | ➖ client config | — |
| 13 | Expiration | ✅ `UpExpireCache()` | `Shop.razor` · `UpExpireCache` |
| 14 | Expiring content after an interaction | ✅ `X-Up-Expire-Cache`, incl. `false` via `UpKeepCache()` | `Shop.razor` · `Refresh()` |
| 15 | Eviction | ✅ `UpEvictCache()` | — |
| 16 | Evicting content after an interaction | ✅ same header | — |
| 17 | Capping memory usage | ➖ `cacheSize` | — |
| 18 | **Caching optimized responses** | ✅ this is why `Vary` is mandatory | `Program.cs:36` · `UseUnpoly` |
| 19 | Example | — | — |
| 20 | **How cache entries are matched** | ✅ keyed by URL, then partitioned by every header named in `Vary` | same |
| 21 | Caching after redirects | ⬜ unread detail — revisit with `X-Up-Location` in Phase E | — |

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
| Bar shown after `lateDelay` | ➖ on by default | — |
| `progressBar = false` to replace it | ➖ via `ExtraScript` | — |
| `[up-background]`; preload and poll are background | ➖ | — |

### [/up:request:load](https://unpoly.com/up:request:load)

| Concept | Status | Sample |
|---|---|---|
| Mutating `event.request.headers` before send | 🚫 **declined** — it injected the CSRF token, which `up.protocol.config` already does. Nine lines deleted | — |

### /aborting-requests · /network-issues

Not read. Expected to be entirely client-side; confirm before ticking.

---

## `up.form`

### [/validation](https://unpoly.com/validation) — 8/8 sections

| § | Concept | Status | Sample |
|---|---|---|---|
| 1 | Validating forms | ✅ `IsUpValidating()` | `Login.razor:59,73,83` |
| 2 | Contents | — | — |
| 3 | Validating after submission | ⬜ not exercised — the sample only validates on change | — |
| 4 | Signaling a failed submission | ✅ answer **422** | `Login.razor:83` · `Invalid()` |
| 5 | Changing how validation errors are rendered | ➖ `[up-form-group]` picks the swapped region | `Login.razor:26,32` |
| 6 | HTML5 validations | ➖ browser-native, runs before any request | — |
| 7 | Validating after changing a field | ✅ `[up-validate]`; fields arrive **space** separated | `Login.razor:28,34` |
| 8 | Validating while typing | ➖ `[up-watch-event=input]` | — |

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
| 5 | Multiple submit buttons | ➖ `[up-submit]` per button | — |
| 6 | Per-button parameters | ➖ `[formmethod]`/`[formaction]` | — |
| 7 | Per-button actions | ➖ | — |
| 8 | Showing that the form is processing | ➖ `[up-disable]` | `Login.razor:23` · `app.css:274` |
| 9 | Overriding render options | ➖ attributes | — |
| 10 | Handling all forms automatically | ✅ `HandleAllLinksAndForms` | `Program.cs:8` |
| 11 | Opting into a full page load | ➖ `[up-submit=false]` | — |
| 12 | Submitting forms with JavaScript | ➖ `up.submit()` | — |

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
| Remaining sections of both guides | ⬜ **Phase F** | — |

---

## `up.script`

### [/handling-asset-changes](https://unpoly.com/handling-asset-changes) — partial

| Concept | Status | Sample |
|---|---|---|
| Unpoly diffs scripts and stylesheets in `<head>` | 🚫 **broken here on purpose** — cutting `<head>` leaves nothing to diff | `App.razor:11` |
| `up:assets:changed` has no default behaviour | ⬜ **Phase G** — decide whether to re-emit `[up-asset]` | — |
| Remaining sections | ⬜ **Phase G** | — |

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

`up.fragment` · `up.layer` · `up.radio` · `up.history` · `up.viewport` · `up.event`

Skipped for the whole project: `up.motion` · `up.element` · `up.util` · `up.log` ·
`up.framework`, plus the *Features* tier everywhere — that tier is a dictionary. Look
things up in it **before** writing glue; the CSRF listener above is what happens otherwise.
