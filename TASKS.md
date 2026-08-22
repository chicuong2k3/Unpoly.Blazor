# TASKS — Unpoly.Blazor

Resumable work log. Any agent (Claude Code, opencode, Codex…) or human picks this up cold.
Read `AGENTS.md` first for the rules, then start at **Next action** below.

---

## Status

| | |
|---|---|
| Phase | **A complete**, B not started |
| Protocol coverage | **6 / 26** headers |
| Build | 4 projects, 0 errors |
| Checks | 14 passing |
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

**Phase B.** Start by reading <https://unpoly.com/caching> twice, then add a request
counter to `sample/Jubin/Program.cs` and click around. You should observe **two** server
requests per click. Understand why before writing any code.

---

## Definition of done, per phase

A phase is done when all four are true:

1. Its guides are read.
2. Its methods no longer `throw new NotImplementedException`.
3. Its Jubin feature works in the browser.
4. `tests/Unpoly.Blazor.Tests/Program.cs` has a new `── PHASE X ──` block and still prints OK.

---

## Phase A · Fragments and targeting ✅

- [x] Read `/navigation`, `/render-lifecycle`, `/target-derivation`, `/providing-html`, `/preserving-elements`, `/skipping-rendering`, `/templates`
- [x] Read the 7 `up.link` guides
- [x] `IsUnpoly` `UpVersion` `UpTarget` `UpFailTarget` `UpTargets` `IsUpFragment` `WantsNothing` `UpRetarget`
- [x] `UpChrome` component
- [x] Jubin: category nav, product grid, product detail
- [x] 14 checks

Lesson banked: `X-Up-Target` is a **list**, not one selector. `"body, .flash"` is still a
full-page request. Missing that made chrome vanish silently — check kept at
`tests/.../Program.cs`.

## Phase B · Network and cache ⬅️ NEXT

- [ ] Read `/caching` **twice**
- [ ] Read `/aborting-requests`, `/network-issues`, `/progress-bar`
- [ ] Read `/optimizing-responses`, `/conditional-responses`
- [ ] `UpExpireCache` `UpEvictCache` `UpClearCache` (UpResponse.cs)
- [ ] `UpReloadFromTime` (UpRequest.cs)
- [ ] ETag / `If-None-Match` → return **304** with empty body
- [ ] Jubin: expire the listing cache after a cart mutation; enable the progress bar
- [ ] Checks: cache pattern header written; 304 path returns no body
- [ ] Write the double-request finding into README so library users hit it in docs, not in prod

> Cache revalidation means **one click produces two server requests**. Handlers must be
> idempotent. This invalidates assumptions in every later phase — do not reorder.

## Phase C · Forms

- [ ] Read `/submitting-forms`, `/validation`, `/switching-form-state`, `/reactive-server-forms`, `/disabling-forms`, `/watch-options`
- [ ] `IsUpValidating` `UpValidatingField`
- [ ] Make `UpRetarget` fail-aware (4xx/5xx uses `X-Up-Fail-Target`)
- [ ] Jubin: price/collection filter via `[up-autosubmit]` + `[up-watch-delay=300]`
- [ ] Jubin: login form returning **422** on invalid input, not 200
- [ ] Checks: validating request must not persist; 422 path selects the fail target
- [ ] ⚠️ Verify `<AntiforgeryToken />` renders correctly **outside** a `<form>` (see Open questions)

## Phase D · Layers

- [ ] Read `/layer-terminology`, `/layer-option`, `/opening-overlays`, `/closing-overlays`, `/subinteractions`, `/context`, `/customizing-overlays`
- [ ] `UpMode` `UpFailMode` `UpOriginMode` `IsUpOverlay`
- [ ] `UpContext<T>` `UpFailContext<T>` (`X-Up-Context` travels both ways)
- [ ] `UpOpenLayer` `UpAcceptLayer` `UpDismissLayer`
- [ ] Jubin: login modal as a layer; **subinteraction** — log in mid-browse, return to the exact spot
- [ ] Jubin: cart drawer, product quick-view
- [ ] Checks: accept vs dismiss produce different headers; context round-trips

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
- [ ] Swap DAUB classless → full build + `daub.js` in `sample/Jubin/Components/App.razor`
- [ ] First check whether `daub.js` uses event delegation; if it does, no compiler is needed
- [ ] Otherwise `up.compiler(...)` to re-init DAUB components after each swap
- [ ] Jubin: image carousel; reCAPTCHA re-init on the login form
- [ ] No C# in this phase

---

## Protocol coverage — 6 / 26

Grep `NotImplementedException` in `src/` for the live list.

**Request (12)** — ✅ `X-Up-Version` `X-Up-Target` `X-Up-Fail-Target`
⬜ `X-Up-Mode` `X-Up-Fail-Mode` `X-Up-Origin-Mode` `X-Up-Validate` `X-Up-Context`
`X-Up-Fail-Context` `X-Up-Reload-From-Time` `If-Modified-Since` `If-None-Match`

**Response (14)** — ✅ `X-Up-Target`
⬜ `X-Up-Title` `X-Up-Location` `X-Up-Method` `X-Up-Open-Layer` `X-Up-Accept-Layer`
`X-Up-Dismiss-Layer` `X-Up-Events` `X-Up-Clear-Cache` `X-Up-Evict-Cache`
`X-Up-Expire-Cache` `ETag` `Last-Modified` `Vary` + cookie `_up_method`

Spec: <https://unpoly.com/up.protocol>

---

## Open questions

- **`<AntiforgeryToken />` outside a `<form>`** — used in `UnpolyHead.razor`, never exercised
  because the sample has no POST yet. If it fails in Phase C, replace it with
  `IAntiforgery.GetAndStoreTokens(ctx).RequestToken` rendered into a `<meta>` tag.
- **`X-Up-Clear-Cache`** — may be legacy/superseded by expire+evict. Confirm against the
  spec in Phase B before implementing; delete the stub if it is obsolete.
- **`daub.js` init model** — delegation or per-element? Decides whether Phase G needs
  `up.compiler` at all. Cheap to check early.
- **Vendored Unpoly version** — pinned by download, not by a version file. Consider
  recording the exact version so `/handling-asset-changes` in Phase G has something to test.

---

## Reading budget

~60 guide pages total across 17 modules. At 6–8 min each that is ~7 hours.
Skip entirely: `up.motion`, `up.element`, `up.util`, `up.log`, `up.framework`,
and the whole **Features** tier (reference, not prose).

If only three pages ever get read: `/render-lifecycle`, `/caching`, `/subinteractions`.
