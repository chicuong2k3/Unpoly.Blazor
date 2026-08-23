# TASKS — Unpoly.Blazor

Resumable work log. Any agent (Claude Code, opencode, Codex…) or human picks this up cold.
Read `AGENTS.md` first for the rules, then start at **Next action** below.

---

## Status

| | |
|---|---|
| Phase | **all 7 complete** |
| Protocol coverage | **24 / 24** — complete |
| Build | 4 projects, 0 errors |
| Checks | 77 tests under one `dotnet test` |
| Unpoly version vendored | 3.x (`src/Unpoly.Blazor/wwwroot/unpoly.min.js`) |

## Verify current state

```bash
dotnet build                                       # expect 0 errors
dotnet test                                       # 77 tests: 9 header + 68 browser
dotnet run --project sample/Jubin                  # then the curl proof below
```

Fragment vs full-page proof (server must be running):

```bash
curl -s http://localhost:5199/shop/dam | grep -c up-nav          # 1  (chrome present)
curl -s -H "X-Up-Version: 3.10.2" -H "X-Up-Target: .content" \
        http://localhost:5199/shop/dam | grep -c up-nav          # 0  (chrome skipped)
```

If the build fails with a file lock, a previous `dotnet run` is still alive: `taskkill //F //IM dotnet.exe`.

---

## Next action

**Everything is closed.** 24/24 protocol headers, 97 unit checks, 68 browser checks, and no
open item left in `VERIFY.md` that was ever in reach.

The overlay stack is built, so `/layer-option` and `/history-in-overlays` have something to
point at. Two things are recorded as **not** true rather than left open:

- Scroll is not restored on Back by default — opt in with `[up-scroll=restore]`
- reCAPTCHA needs a real site key, so the compiler story is demonstrated with a stand-in
  widget instead. The mechanism is identical; only the vendor is missing

**All 24 protocol headers are implemented.** Phase G adds no C# at all — it is about
`up.compiler` and re-initialising third-party widgets after a swap.

Two exercises are still carried forward, both blocked on the same missing feature — the
sample opens only **one** overlay at a time:

- ten `/layer-option` rows (root / parent / closest / ancestor / child / any …)
- `/history-in-overlays` rows 2—7

That is one feature to build, not seventeen checks to write.

---

## Definition of done, per phase

**Every box in that phase's section of [`VERIFY.md`](VERIFY.md) passes.** That file is the
authority — it lists the exact command or browser observation behind each claim.

Summarised, a phase is done when:

1. Its guides are read.
2. Its methods no longer `throw new NotImplementedException`.
3. `VERIFY.md` for that phase is fully ticked, including the browser-observable items.
4. A new `[Fact]` exists — header logic in `Unpoly.Blazor.Tests`, browser behaviour in
   `Unpoly.Blazor.BrowserTests` — and `dotnet test` is green.
5. `CONCEPTS.md` marks its guides, and `.claude/skills/unpoly-blazor/SKILL.md` is updated
   **both ways**: the phase's methods move out of "typed helpers that do not exist yet",
   and anything newly marked ➖ is added to "reach for HTML first".

An item you could not verify stays **unticked**, with a note. An untested tick is worse than
an open box — it is the only way this checklist can lie to you.

---

## Phase A · Fragments and targeting ✅

- [x] Read `/navigation`, `/render-lifecycle`, `/target-derivation`, `/providing-html`, `/preserving-elements`, `/skipping-rendering`, `/templates`
- [x] Read the 7 `up.link` guides
- [x] `IsUnpoly` `UpVersion` `UpTarget` `UpFailTarget` `UpTargets` `IsUpFragment` `WantsNothing` `UpRetarget`
- [x] `UpChrome` component
- [x] Jubin: category nav, product grid, product detail
- [x] 14 checks

Lesson banked #2: Unpoly already had CSRF support. Nine lines of hand-rolled
`up:request:load` listener were reinventing `up.protocol.config`. Check the *Features*
tier of a module before writing glue — that tier is a dictionary, and this is what it is for.

Lesson banked #1: `X-Up-Target` is a **list**, not one selector. `"body, .flash"` is still a
full-page request. Missing that made chrome vanish silently — check kept at
`tests/.../Program.cs`.

## Phase B · Network and cache ⬅️ NEXT

- [x] Read `/caching` **twice**
- [x] Read `/progress-bar` · [ ] `/aborting-requests`, `/network-issues` (nothing server-side expected)
- [x] Read `/optimizing-responses`
- [x] Read `/conditional-requests` (the `/conditional-responses` URL 404s)
- [x] `UpExpireCache` `UpEvictCache` `UpClearCache` (UpResponse.cs)
- [x] `UpReloadFromTime` (UpRequest.cs)
- [x] ETag / `If-None-Match` → return **304** with empty body
- [x] `Vary` via `UpVary` + `UseUnpoly()` middleware — **required**, not an optimisation:
      the body changes with `X-Up-Target`, so without it a shared cache can serve a
      fragment to a full page load
- [x] Cut `<head>` on fragment responses by reusing `UpChrome` inside `App.razor`.
      `<HeadOutlet />` stays outside the wrapper so `<PageTitle>` keeps working — which is
      why `X-Up-Title` is not needed yet. `/p/dam-4`: 1865 -> 459 bytes
- [x] Jubin: expire the listing cache after a cart mutation; enable the progress bar
- [x] Checks: cache pattern header written; 304 path returns no body
- [x] Write the double-request finding into README so library users hit it in docs, not in prod

> Cache revalidation means **one click produces two server requests**. Handlers must be
> idempotent. This invalidates assumptions in every later phase — do not reorder.

## Phase C · Forms

- [x] Read `/validation`, `/failed-responses`, `/X-Up-Validate`, `/submitting-forms`
- [x] `IsUpValidating` `UpValidatingFields` `IsUpValidatingUnknown` `UpFailTargets`
- [x] `IsUpFragment` now honours the **failure** branch — a `[up-fail-target=body]` form was
      getting a 422 body swap with no nav in it
- [x] Jubin: login form, 422 on invalid, no persistence while validating
- [x] 13 checks
- [x] Jubin: price/collection filter via `[up-autosubmit]` + `[up-watch-delay=300]`
- [x] Read `/reactive-server-forms`

## Phase D · Layers

- [x] Read all 7 guides — 62 sections enumerated in `CONCEPTS.md`
- [x] `UpMode` `UpFailMode` `UpOriginMode` `IsUpOverlay`
- [x] `UpContext<T>` `UpFailContext<T>` `UpSetContext` (`X-Up-Context` travels both ways)
- [x] `UpOpenLayer` `UpAcceptLayer` `UpDismissLayer`
- [x] `UseUnpoly()` now varies on `X-Up-Mode` and `X-Up-Context` too — the /context guide
      names this trap directly
- [x] Jubin: **subinteraction** — the size picker is a real route that becomes an overlay,
      and the product page keeps its state behind it
- [x] Jubin: the server opening a drawer the link never asked for
- [x] 20 unit checks + 10 browser checks
- [x] Open a **second** overlay so `/layer-option` rows 2–10 mean something

## Phase E · History, scrolling and focus

- [x] Read `/updating-history`, `/restoring-history`, `/history-in-overlays`, `/analytics`
- [x] Read `/scrolling`, `/scroll-tuning`, `/focus`, `/focus-visibility`, `/infinite-scrolling`
- [x] `UpTitle` (JSON-encoded, quotes included) `UpLocation` `UpMethod`
- [x] `UpMethodCookie` — the cookie exists because a full page load carries no Unpoly
      request to put a header on; Unpoly pops it during boot
- [x] Jubin: infinite scroll on `/shop` via `.listing:after` — closes the `:after` row open
      since Phase A
- [x] 8 unit + 8 browser checks
- [x] ~~Scroll position restored on Back~~ — **it is not, by default.** Observed: every
      sample over 4s was 0. The guide makes it opt-in via `[up-scroll=restore]`
- [x] Read `/scrolling`, `/focus`, `/analytics`

## Phase F · Status and passive updates

- [x] Read `/navigation-bars`, `/loading-state`, `/feedback-classes`, `/placeholders`, `/previews`, `/optimistic-rendering`
- [x] Read `/polling`, `/flashes`
- [x] `UpEmit` — the last header. Accumulates, and escapes non-ASCII because Unpoly states
      plainly that headers may only carry US-ASCII
- [x] Jubin: cart badge via `[up-hungry]`, flash message, `[up-placeholder]`, `[up-poll]`
- [x] `[up-poll]` + `[up-etag]` — an unchanged poll costs a 304, which is Phase B paying off
- [x] 7 unit + 5 browser checks

## Phase G · JavaScript layer

- [x] Read `/enhancing-elements`, `/data`, `/handling-asset-changes`, `/script-security`, `/legacy-scripts`
- [x] A stand-in third-party widget with an imperative init/destroy API and a timer
- [x] `up.compiler` re-inits it; verified across three swaps away and back
- [x] The returned destructor stops the old instances — `liveCount()` stays at 1, not 4
- [x] `[up-data]` reaches the compiler as its second argument
- [x] **Asset tracking resolved.** Assets are only tracked in `<head>`, so cutting it had
      silently disabled `up:assets:changed` for five phases. One `meta[up-asset]` outside
      `UpChrome` buys it back
- [x] Application scripts moved out of `<body>` — they re-executed on every swap
- [ ] reCAPTCHA on the login form (needs a real key)
- [x] No C# in this phase

---

## Protocol coverage — 20 / 24

Grep `NotImplementedException` in `src/` for the live list.

**Request (11)** — all covered: `X-Up-Version` `X-Up-Target` `X-Up-Fail-Target`
`If-None-Match` `If-Modified-Since` `X-Up-Validate` `X-Up-Mode` `X-Up-Fail-Mode`
`X-Up-Origin-Mode` `X-Up-Context` `X-Up-Fail-Context`

**Response (13)** — ✅ `X-Up-Target` `Vary` `X-Up-Expire-Cache` `X-Up-Evict-Cache` `ETag`
`Last-Modified` `X-Up-Open-Layer` `X-Up-Accept-Layer` `X-Up-Dismiss-Layer` `X-Up-Context`
⬜ `X-Up-Title` `X-Up-Location` `X-Up-Method` `X-Up-Events` + cookie `_up_method`

All 24 are implemented; nothing in the library throws.

**Dropped from the target (was 26):** `X-Up-Clear-Cache` appears in no current guide, and
`X-Up-Reload-From-Time` is deprecated in favour of `Last-Modified`. Implementing either
would have been work that made the library worse.

Spec: <https://unpoly.com/up.protocol>

---

## Exercise gaps

One method is covered by a unit test but has never run in the sample:

- `UpKeepCache()` — needs a POST that changes nothing the user can see, such as recording
  a "recently viewed" ping. Everything else in the library runs in the sample.

`CONCEPTS.md` has **305 rows and none open**: every one points at the sample or says in
words why it cannot.

## Open questions

- **`GetAndStoreTokens` vs streaming rendering** — the antiforgery token is minted while
  `<head>` renders, which is safe today. Re-verify if streaming rendering is ever enabled.
- **reCAPTCHA on the login form** — needs a real site key. The mechanism it would exercise
  is already covered by the stand-in widget: a third-party `init`/`destroy` API,
  re-initialised by a compiler, cleaned up by a destructor.

Resolved, kept here because the reasoning outlived the question:

- ~~`<AntiforgeryToken />` outside a `<form>`~~ — Unpoly ships CSRF support
  (`up.protocol.config.csrfHeader` / `csrfToken`), so the hand-rolled `up:request:load`
  listener and the hidden token div are gone.
- ~~`X-Up-Clear-Cache`~~ — in no current guide. Stub deleted, and the protocol target
  dropped from 26 headers to 24.
- ~~Asset tracking is impossible on fragment responses~~ — it is not. Assets are only
  tracked in `<head>`, so one `meta[up-asset]` placed **outside** `UpChrome` restores
  detection for about a hundred bytes. This was carried as a trade-off for five phases and
  was avoidable the whole time.
- ~~The vendored Unpoly version is not recorded~~ — `wwwroot/UNPOLY-VERSION.txt` names it
  (3.14.3, read from `up.version` rather than the floating CDN URL) and says what to re-run
  when it changes.

---

## Reading

**All 17 modules and 52 guides are read and enumerated in `CONCEPTS.md`**, with 305 concept
rows and none left open.

An earlier version of this file told you to skip five modules — `up.motion`, `up.element`,
`up.util`, `up.log`, `up.framework` — on the grounds that they have no server side. That was
wrong twice over. `up.util` documents **relaxed JSON** and **URL patterns**, which are the
formats this library writes: `UpExpireCache` takes a URL pattern, and several headers are
specified as relaxed JSON. And "no C# to write" never meant "nothing to demonstrate" —
`up.motion` is now four links in the lab.

The **Features** tier is still a dictionary rather than reading material, but it is
enumerated for the four modules that have no guides at all (`up.element`, `up.event`,
`up.framework`, `up.log`). Look things up in it **before** writing glue: the CSRF listener
that got deleted was nine lines reimplementing `up.protocol.config`.

If only three pages ever get read: `/render-lifecycle`, `/caching`, `/subinteractions`.
