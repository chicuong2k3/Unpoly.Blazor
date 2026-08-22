# TASKS — Unpoly.Blazor

Resumable work log. Any agent (Claude Code, opencode, Codex…) or human picks this up cold.
Read `AGENTS.md` first for the rules, then start at **Next action** below.

---

## Status

| | |
|---|---|
| Phase | **A, B, C and D complete** |
| Protocol coverage | **20 / 24** headers |
| Build | 4 projects, 0 errors |
| Checks | 82 unit + 37 browser |
| Unpoly version vendored | 3.x (`src/Unpoly.Blazor/wwwroot/unpoly.min.js`) |

## Verify current state

```bash
dotnet build                                       # expect 0 errors
dotnet run --project tests/Unpoly.Blazor.Tests     # expect "OK — 14 checks passed (Phase A)"
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

**Phase E · History, scrolling and focus.** Read `/updating-history` first.

Nothing is open in A–D. Every browser-observable claim is automated in
`tests/browser/test_unpoly.py`.

One exercise is worth carrying into E: the sample only ever opens **one** overlay. Ten rows
of `/layer-option` (root / parent / closest / ancestor / child / any …) need a *stack* to
mean anything. That is one feature to build, not ten checks to write.

---

## Definition of done, per phase

**Every box in that phase's section of [`VERIFY.md`](VERIFY.md) passes.** That file is the
authority — it lists the exact command or browser observation behind each claim.

Summarised, a phase is done when:

1. Its guides are read.
2. Its methods no longer `throw new NotImplementedException`.
3. `VERIFY.md` for that phase is fully ticked, including the browser-observable items.
4. `tests/Unpoly.Blazor.Tests/Program.cs` has a new `── PHASE X ──` block and still prints OK.
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
- [ ] `UpExpireCache` `UpEvictCache` `UpClearCache` (UpResponse.cs)
- [ ] `UpReloadFromTime` (UpRequest.cs)
- [ ] ETag / `If-None-Match` → return **304** with empty body
- [x] `Vary` via `UpVary` + `UseUnpoly()` middleware — **required**, not an optimisation:
      the body changes with `X-Up-Target`, so without it a shared cache can serve a
      fragment to a full page load
- [x] Cut `<head>` on fragment responses by reusing `UpChrome` inside `App.razor`.
      `<HeadOutlet />` stays outside the wrapper so `<PageTitle>` keeps working — which is
      why `X-Up-Title` is not needed yet. `/p/dam-4`: 1865 -> 459 bytes
- [ ] Jubin: expire the listing cache after a cart mutation; enable the progress bar
- [ ] Checks: cache pattern header written; 304 path returns no body
- [ ] Write the double-request finding into README so library users hit it in docs, not in prod

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
- [ ] Read `/reactive-server-forms`

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
- [ ] Open a **second** overlay so `/layer-option` rows 2–10 mean something

## Phase E · History, scrolling and focus

- [ ] Read `/updating-history`, `/restoring-history`, `/history-in-overlays`, `/analytics`
- [ ] Read `/scrolling`, `/scroll-tuning`, `/focus`, `/focus-visibility`, `/infinite-scrolling`
- [ ] `UpTitle` (value is a **JSON-encoded** string) `UpLocation` `UpMethod`
- [ ] Investigate the `_up_method` cookie
- [ ] Jubin: filters push to the URL; infinite scroll on `/shop`; scroll restored on Back
- [ ] Checks: `UpTitle` writes valid JSON, not a bare string

## Phase F · Status and passive updates

- [ ] Read `/navigation-bars`, `/loading-state`, `/feedback-classes`, `/placeholders`, `/previews`, `/optimistic-rendering`
- [ ] Read `/polling`, `/flashes`
- [ ] `UpEmit` — repeated calls accumulate into one `X-Up-Events` JSON array
- [ ] Jubin: cart badge via `[up-hungry]`; flash message; skeleton via `[up-placeholder]`
- [ ] Checks: two `UpEmit` calls produce `[{...,"type":"a"},{"type":"b"}]`

## Phase G · JavaScript layer

- [ ] Read `/enhancing-elements`, `/data`, `/handling-asset-changes`, `/script-security`, `/legacy-scripts`
- [ ] Add one third-party JS widget to the sample (carousel or a date picker) so there is
      something real that needs re-initialising after a swap
- [ ] `up.compiler(...)` to re-init it; verify it survives several fragment swaps
- [ ] Jubin: reCAPTCHA re-init on the login form
- [ ] Decide the asset-tracking question below (fragment responses have no `<head>`)
- [ ] No C# in this phase

---

## Protocol coverage — 20 / 24

Grep `NotImplementedException` in `src/` for the live list.

**Request (11)** — all covered: `X-Up-Version` `X-Up-Target` `X-Up-Fail-Target`
`If-None-Match` `If-Modified-Since` `X-Up-Validate` `X-Up-Mode` `X-Up-Fail-Mode`
`X-Up-Origin-Mode` `X-Up-Context` `X-Up-Fail-Context`

**Response (13)** — ✅ `X-Up-Target` `Vary` `X-Up-Expire-Cache` `X-Up-Evict-Cache` `ETag`
`Last-Modified` `X-Up-Open-Layer` `X-Up-Accept-Layer` `X-Up-Dismiss-Layer` `X-Up-Context`
⬜ `X-Up-Title` `X-Up-Location` `X-Up-Method` `X-Up-Events` + cookie `_up_method`

The four left are Phase E (history, `_up_method`) and Phase F (`X-Up-Events`).

**Dropped from the target (was 26):** `X-Up-Clear-Cache` appears in no current guide, and
`X-Up-Reload-From-Time` is deprecated in favour of `Last-Modified`. Implementing either
would have been work that made the library worse.

Spec: <https://unpoly.com/up.protocol>

---

## Exercise gaps

`CONCEPTS.md` ends with a list of methods that pass unit checks but have never run in the
sample. They are candidate work, not bugs — pick them up alongside the phase that fits:

- `UpKeepCache()` — needs a POST that changes nothing visible, so Phase D or F
- `:before` / `:after` — infinite scroll in Phase E

Everything else is exercised. `CONCEPTS.md` has no bare dashes left: 110 rows, 73 pointing
at the sample, 20 marked `n/a` with a reason, 17 marked `todo` with what is missing.

## Open questions

- ~~`<AntiforgeryToken />` outside a `<form>`~~ — **resolved.** Unpoly ships CSRF support
  (`up.protocol.config.csrfHeader` / `csrfToken`), so the hand-rolled `up:request:load`
  listener and the hidden token div are gone. `UnpolyHead` now feeds the ASP.NET token
  straight into `up.protocol.config.csrfToken`. 📖 https://unpoly.com/up.protocol.config
- **`GetAndStoreTokens` vs streaming rendering** — the token is minted while `<head>` renders,
  which is safe today. Re-verify if streaming rendering is ever enabled.
- ~~`X-Up-Clear-Cache`~~ — **resolved:** in no current guide. Stub deleted.
- **Asset tracking is now impossible on fragment responses** — cutting `<head>` means no
  scripts or stylesheets are present to diff, so `up:assets:changed` can never fire for a
  fragment. Decide in Phase G whether to re-emit `[up-asset]` elements into fragment
  responses. 📖 https://unpoly.com/handling-asset-changes
- **Vendored Unpoly version** — pinned by download, not by a version file. Consider
  recording the exact version so `/handling-asset-changes` in Phase G has something to test.

---

## Reading budget

~60 guide pages total across 17 modules. At 6–8 min each that is ~7 hours.
Skip entirely: `up.motion`, `up.element`, `up.util`, `up.log`, `up.framework`,
and the whole **Features** tier (reference, not prose).

If only three pages ever get read: `/render-lifecycle`, `/caching`, `/subinteractions`.
