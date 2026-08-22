# VERIFY.md

What must actually pass before a phase counts as done.

`TASKS.md` says *what to build*. This file says *how you know it works*. A phase is never
done on code review — every item below is a command to run or a thing to watch happen.

## Rules

- **Automated** items live in `tests/Unpoly.Blazor.Tests/Program.cs`. They cover logic that
  can go wrong silently. Run: `dotnet run --project tests/Unpoly.Blazor.Tests`
- **Server-observable** items are `curl` commands with an expected output. They cover the
  wire format, which unit tests cannot see.
- **Browser-observable** items need a human watching. They cover behaviour no assertion
  reaches: focus, scroll, timing, whether a thing *feels* broken.
- If an item cannot be verified, say so in the phase's notes rather than ticking it. An
  untested tick is worse than an open box.

Start the sample first for anything below the automated section:

```bash
dotnet run --project sample/Jubin --urls http://localhost:5199
```

If the build fails on a locked DLL, a previous run is alive: `taskkill //F //IM dotnet.exe`.

---

## Phase A · Fragments and targeting ✅

**Automated — 14 checks**

- [x] A request without `X-Up-Version` is not an Unpoly request
- [x] `.content` classifies as a fragment; `body`, `html`, `:main`, `:layer` do not
- [x] A target **list** containing a whole-page target is not a fragment (`"body, .flash"`)
- [x] Lists split and trim (`".content , .flash"` → 2 entries, second is `.flash`)
- [x] Modifiers are stripped before classifying: `.tasks:after` is a fragment, `body:after`
      is not, `:main:content` is not, `.flash:maybe` is
- [x] `:none` reports `WantsNothing()`
- [x] `UpRetarget` writes `X-Up-Target`

**Server-observable**

```bash
U=http://localhost:5199/shop/dam
curl -s $U | grep -c up-nav                                    # 1
curl -s -H "X-Up-Version: 3.10.2" -H "X-Up-Target: .content" $U | grep -c up-nav   # 0
curl -s -H "X-Up-Version: 3.10.2" -H "X-Up-Target: .content" $U | grep -c 'class="content"'  # 1
```

- [x] Chrome present on a plain GET, absent on a fragment GET, target always present

**Browser-observable**

- [x] Clicking a category swaps the grid with no white flash and no full reload
- [x] The clicked category gets `.up-current` styling; the nav itself never re-renders
- [ ] **Not verified:** visual appearance against jubinstudio.com — the Chrome extension
      would not connect, so no screenshot was taken. Open it and judge.

---

## Phase B · Network and cache ⏳ (Vary and head-cut done)

**Automated**

- [x] `UpVary` writes both header names
- [x] `UpVary` merges with an existing `Vary` instead of overwriting (`Accept-Encoding`)
- [x] `UpVary` dedupes case-insensitively
- [ ] A 304 path returns an empty body
- [ ] `UpReloadFromTime` parses the header into a `DateTimeOffset`
- [ ] Cache-control headers carry the pattern they were given

**Server-observable**

```bash
curl -s -D - -o /dev/null http://localhost:5199/p/dam-4 | grep -i '^vary'
# Vary: X-Up-Target, X-Up-Version
```

- [x] `Vary` present on **both** shapes of response
- [x] Fragment response carries no stylesheet, no `unpoly.min.js`, no config script,
      but still carries `<title>` and `.content` (1865 → 459 bytes on `/p/dam-4`)
- [ ] A second request with `If-None-Match` matching the previous `ETag` returns **304**
      with `Content-Length: 0`

**Browser-observable**

- [ ] **The central one:** log every request server-side, click one link, confirm the server
      is hit **twice** — cached render first, then revalidation. If you see one, you have not
      reproduced the behaviour the rest of the phase depends on
- [ ] After a cart mutation, the listing is not stale
- [ ] The progress bar appears on a slow response and not on a fast one

---

## Phase C · Forms

**Automated**

- [ ] `IsUpValidating` true only when `X-Up-Validate` is present
- [ ] `UpValidatingField` returns the field name; empty and `:unknown` mean the whole form
- [ ] `UpRetarget` targets the fail branch on a 4xx/5xx status

**Server-observable**

- [ ] An invalid login POST answers **422**, not 200
- [ ] A validating request answers 200 with the re-rendered form
- [ ] Antiforgery: a POST without the token is rejected; a POST from Unpoly is accepted
      (this is the first real exercise of `up.protocol.config.csrfToken`)

**Browser-observable**

- [ ] Leaving an invalid field shows its error without submitting the form
- [ ] **A validating request writes nothing to the database.** Check the data, not the UI
- [ ] Filter inputs autosubmit after the debounce, not on every keystroke
- [ ] The form is disabled while in flight and re-enabled after

---

## Phase D · Layers

**Automated**

- [ ] Accept and dismiss write different headers
- [ ] `X-Up-Context` round-trips: read an object in, write a changed one out
- [ ] Mode helpers distinguish root from overlay

**Browser-observable**

- [ ] A link with `[up-layer=new]` opens an overlay; the page behind keeps its scroll
      position and its form state
- [ ] Saving closes the overlay **and** refreshes the list behind it
- [ ] Cancelling closes it and changes nothing
- [ ] **Subinteraction:** start browsing a product, log in inside an overlay, land back on
      the same product — not on the home page
- [ ] Browser Back closes the overlay rather than leaving the site
- [ ] Two overlays can stack, and closing the top one reveals the one below intact

---

## Phase E · History, scrolling and focus

**Automated**

- [ ] `UpTitle` writes a **JSON-encoded** string, not a bare one
- [ ] `UpLocation` and `UpMethod` write their headers

**Browser-observable**

- [ ] Filtering changes the URL, and pasting that URL into a new tab reproduces the view
- [ ] Back and Forward restore both the fragment and the scroll position
- [ ] The document title follows navigation, including after a fragment-only response
- [ ] Keyboard focus lands in the new fragment, not back at the top of the document
- [ ] Infinite scroll appends without losing the scroll position

---

## Phase F · Status and passive updates

**Automated**

- [ ] Two `UpEmit` calls accumulate into one JSON array:
      `[{"id":1,"type":"a"},{"type":"b"}]`

**Browser-observable**

- [ ] The cart badge updates after adding an item **from any page**, without being targeted
- [ ] A flash message appears once and does not duplicate across subsequent swaps
- [ ] Skeletons show during a slow load and are replaced, not appended
- [ ] `.up-active` and `.up-loading` are visibly styled while a request is in flight

---

## Phase G · JavaScript layer

**Browser-observable**

- [ ] A third-party widget still works after **three** consecutive fragment swaps, not one
- [ ] Navigating away and back does not leave duplicate event listeners
      (check the listener count, not the appearance)
- [ ] reCAPTCHA re-initialises on a swapped login form
- [ ] Decide and record: whether fragment responses re-emit `[up-asset]`. If not,
      `up:assets:changed` can never fire — say so in `CONCEPTS.md` rather than leaving it
      looking unfinished

---

## Cross-cutting, re-check at every phase

- [ ] `dotnet build` — 0 errors, 0 warnings
- [ ] `dotnet run --project tests/Unpoly.Blazor.Tests` — prints OK
- [ ] `.claude/skills/unpoly-blazor/SKILL.md` moved this phase's methods out of
      "not available yet"
- [ ] No route stopped working with JavaScript disabled. This is the property that makes
      Unpoly worth choosing over htmx here, and it is the easiest one to lose without noticing
