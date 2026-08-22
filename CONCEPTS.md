# CONCEPTS.md

Per-guide tracking: which concepts from each Unpoly guide are handled, and where.

`TASKS.md` tracks *what to do next*. This file tracks *what has been understood and
covered*, guide by guide, so a page can be marked finished with confidence.

**Legend**

| | |
|---|---|
| ✅ | covered — the code exists and a check or a browser observation proves it |
| ⬜ | not covered yet — the phase that will cover it is named |
| ➖ | nothing to cover — pure client-side, the C# side has no part to play |
| 🚫 | deliberately declined — the reason is stated |

Rows are added as guides are read. An empty guide section means it has not been read yet;
do not pre-fill it.

---

## `up.link`

### [/handling-everything](https://unpoly.com/handling-everything)

| Concept | Config | Status |
|---|---|---|
| Following all links | `up.link.config.followSelectors.push('a[href]')` | ✅ `UnpolyOptions.HandleAllLinksAndForms`, default **on** |
| Following all links on mousedown | `up.link.config.instantSelectors.push('a[href]')` | ✅ `UnpolyOptions.InstantAllLinks`, default **off** |
| Preloading all links | `up.link.config.preloadSelectors.push('a[href]')` | ✅ `UnpolyOptions.PreloadAllLinks`, default **off** |
| Handling all forms | `up.form.config.submitSelectors.push(['form'])` | ✅ same option as "following all links" |
| Opt-outs `[up-follow=false]` `[up-instant=false]` `[up-preload=false]` `[up-submit=false]` | — | ➖ attributes only, no server side |
| Fixing legacy JavaScript code | — | ➖ Phase G territory |
| Customizing navigation defaults | `up.fragment.config.navigateOptions` | ➖ `UnpolyOptions.ExtraScript` already carries it — no C# API earns its place for a config string |

Navigation applies defaults that `up.render()` does not — `history: 'auto'`,
`scroll: 'auto'`, `fallback: ':main'`, `cache: 'auto'`, `revalidate: 'auto'`,
`focus: 'auto'`, `peel: 'dismiss'`. Override them through `ExtraScript`:

```csharp
o.ExtraScript = "up.fragment.config.navigateOptions.transition = 'cross-fade'";
```

Both new flags default **off** on purpose:

- `instant` changes click semantics. A link that needs a confirm, or that users drag rather
  than click, must opt out.
- `preload` multiplies server load. On a product grid, sweeping the mouse across the cards
  fires one request per card. It also fills the same cache that revalidation later refetches
  — enable it only after `/caching` is understood.

### [/targeting-fragments](https://unpoly.com/targeting-fragments)

| Concept | Status |
|---|---|
| `[up-target]` with a CSS selector | ✅ used throughout the sample |
| `mainTargets` fallback when no target is declared | ✅ `UnpolyOptions.MainTargets` |
| Target **lists** (`.a, .b`) | ✅ `UpTargets()` splits and trims; checked |
| Pseudo-target `:main` | ✅ whole-page in `IsUpFragment()`; checked |
| Pseudo-target `:layer` | ✅ whole-page in `IsUpFragment()`; checked |
| Pseudo-target `:none` | ✅ `WantsNothing()`; checked. Answering 204 is not wired up yet |
| Pseudo-target `:origin` | ➖ resolved to a derived selector before the header is sent |
| Modifier `:before` / `:after` (prepend/append) | ✅ stripped by `BaseTarget()` before classification; checked |
| Modifier `:maybe` (optional target) | ✅ stripped by `BaseTarget()`; checked |
| Modifier `:content` (replace children) | ✅ stripped by `BaseTarget()`; checked |
| Server retargeting | ✅ `UpRetarget()` |
| `[up-fallback]` | ➖ resolved entirely on the client; the server plays no part |

`BaseTarget()` exists because modifiers change **how** a match is applied, never **what** is
matched. Comparing raw strings made `body:after` look like a fragment, which dropped the
chrome from a request that wanted to append into `<body>`.

### [/failed-responses](https://unpoly.com/failed-responses)

| Concept | Status |
|---|---|
| `X-Up-Fail-Target` is read | ✅ `UpFailTarget()` |
| Making `UpRetarget` aware of the failure branch | ⬜ **Phase C** |
| Answering 422 so the fail target is used | ⬜ **Phase C** |

---

## `up.protocol`

### [/optimizing-responses](https://unpoly.com/optimizing-responses)

| Concept | Status |
|---|---|
| Shorten the response to what matches the target | ✅ `UpChrome` — used in `MainLayout` **and** inside `<head>` in `App.razor` |
| `Vary` on the headers that changed the body | ✅ `UpVary()` + `UseUnpoly()` middleware; 3 checks |
| Optimisation is optional; full documents are always valid | ✅ that is why no fragment-only endpoints exist here |

Measured on `/p/dam-4`: full page 1865 bytes, fragment 459.

### [/up.protocol.config](https://unpoly.com/up.protocol.config)

| Concept | Status |
|---|---|
| `csrfHeader` | ✅ set from `UnpolyOptions.AntiforgeryHeaderName` |
| `csrfToken` | ✅ fed from `IAntiforgery.GetAndStoreTokens` |
| `csrfParam` | ➖ Blazor's `EditForm` renders its own hidden input |
| `methodParam` (`_method`) | ⬜ **Phase E**, with the `_up_method` cookie |
| `maxHeaderSize` | ⬜ **Phase D** — matters once `X-Up-Context` carries real payloads |

### [/conditional-responses](https://unpoly.com/conditional-responses)

Not read yet — **Phase B**.

---

## `up.network`

### [/up:request:load](https://unpoly.com/up:request:load)

| Concept | Status |
|---|---|
| Mutating `event.request.headers` before send | 🚫 **declined.** It was used to inject the CSRF token, which `up.protocol.config` already does. Nine lines deleted. Keep the hook in mind for genuinely per-request logic |

### [/caching](https://unpoly.com/caching)

Not read yet — **Phase B**. This is the one that reveals two server requests per click.

---

## `up.script`

### [/handling-asset-changes](https://unpoly.com/handling-asset-changes)

| Concept | Status |
|---|---|
| Unpoly diffs scripts and stylesheets in `<head>` between renders | 🚫 **broken here on purpose.** Cutting `<head>` on fragment responses leaves nothing to diff, so `up:assets:changed` can never fire for a fragment |
| `up:assets:changed` has no default behaviour | ⬜ **Phase G** — decide whether to re-emit `[up-asset]` into fragment responses |

---

## Not yet opened

`up.fragment` · `up.form` · `up.layer` · `up.radio` · `up.status` · `up.history` ·
`up.viewport` · `up.event`

Skipped for the whole project: `up.motion` · `up.element` · `up.util` · `up.log` ·
`up.framework`, plus the *Features* tier everywhere — that tier is a dictionary. Look
things up in it **before** writing glue; the CSRF listener above is what happens otherwise.
