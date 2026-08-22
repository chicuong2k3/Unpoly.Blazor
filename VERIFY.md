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

**Browser-observable** items are automated now, in `tests/browser/test_unpoly.py`
(browser-use over CDP, no LLM, no API key). Run them:

```bash
python tests/browser/test_unpoly.py             # visible Chrome, paced to watch
python tests/browser/test_unpoly.py --headless  # fast
```

27 checks, exit code non-zero on failure. A box below is ticked only if that suite covers
it; anything it cannot reach stays open with a note.

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

## Phase B · Network and cache ✅

**Automated**

- [x] `UpVary` writes both header names
- [x] `UpVary` merges with an existing `Vary` instead of overwriting (`Accept-Encoding`)
- [x] `UpVary` dedupes case-insensitively
- [x] `UpExpireCache` writes a URL glob, defaults to `*`
- [x] `UpKeepCache` writes `false` — the opt-out from Unpoly clearing everything after a non-GET
- [x] `UpEvictCache` writes its own header, not Expire's
- [x] A matching ETag returns true and sets 304; a stale one does not
- [x] `W/` prefix, comma lists and `*` all match
- [x] `If-Modified-Since` compares, and sub-second precision is truncated first
      (without truncating, the 304 path silently never fires)
- [x] ~~`UpReloadFromTime`~~ — deprecated by Unpoly, removed rather than implemented

**Server-observable**

```bash
curl -s -D - -o /dev/null http://localhost:5199/p/dam-4 | grep -i '^vary'
# Vary: X-Up-Target, X-Up-Version
```

- [x] `Vary` present on **both** shapes of response
- [x] Fragment response carries no stylesheet, no `unpoly.min.js`, no config script,
      but still carries `<title>` and `.content` (1865 → 459 bytes on `/p/dam-4`)
- [x] A second request with `If-None-Match` matching the previous `ETag` returns **304**
      with a 0-byte body — measured on `/shop`: 9194 → 0

```bash
U=http://localhost:5199/shop
ET=$(curl -s -D - -o /dev/null $U | grep -i '^etag' | tr -d '
' | cut -d' ' -f2)
curl -s -o /dev/null -w "%{http_code} %{size_download}
" -H "If-None-Match: $ET" $U   # 304 0
curl -s -o /dev/null -w "%{http_code} %{size_download}
" -H 'If-None-Match: "x"'  $U   # 200 9194
```

- [x] `Vary` survives on the 304 as well

**Browser-observable**

- [x] **ANSWERED by `tests/browser/test_unpoly.py`.** A click on a **fresh** cached URL
      (within `cacheExpireAge`, 15s) makes **no request at all**. A click on an **expired**
      one renders the stale copy and refetches -- that is the second render pass. So
      "one click, two requests" was wrong as a blanket claim: it depends on expiry.
- [x] ~~every navigation request reaches the server twice~~ — **it does not.** The harness
      was miscounting: cdp-use delivers each `Network.requestWillBeSent` twice with the
      *same* `requestId`. The sample's own request log settled it: three actions produced
      three lines. Deduplicating by `requestId` brought every count to exactly one The sample now prints a numbered request log. Click
      one link and count the lines. The docs describe two *render passes*; they never say
      whether the server sees two *HTTP requests*, and this was not confirmed here because
      the Chrome extension would not connect. Do not tick it on the strength of the docs.

      Note the cache expires after **15 seconds** (`cacheExpireAge`), so a slow click is not
      a revalidation — go back and forth quickly.
- [x] After a mutation the listing is not stale — the refresh form changes the catalog
      version and the page shows the new one, not the cached string
- [x] The progress bar appears on a slow response: `up-progress-bar` is in the DOM while the
      1.2s route loads, and gone once it settles
- [x] `[up-background]` shows no bar even on the same slow route
- [x] **Instant** — one request fires with the button still held down, before any release
- [x] **Preload** — one request fires from a hover held past the 90ms `preloadDelay`
- [x] **Preload on insert** — one request has already fired by the time /lab finishes rendering

Each of those links points at a 1.2s route with its own query string. That matters: the
cache lives 15s, so pointing them all at one fast URL makes the second hover fire nothing
and reads as "preload is broken"

---

## Phase C · Forms ✅

**Automated — 13 checks**

- [x] `IsUpValidating` true only when `X-Up-Validate` is present, including when empty
- [x] `UpValidatingFields` splits on **spaces**, not commas, and collapses repeats
- [x] `:unknown` is recognised and names no fields
- [x] `IsUpFragment` keeps chrome when the **fail** target is whole-page, and only then

**Server-observable** — reproduce with the sample running:

```bash
TOK=$(curl -s -c cj http://localhost:5199/login       | grep -o 'name="__RequestVerificationToken" value="[^"]*"' | sed 's/.*value="//;s/"//')
P=(--data-urlencode _handler=login --data-urlencode "__RequestVerificationToken=$TOK")

# invalid submit -> 422
curl -s -b cj -o /dev/null -w "%{http_code}
" -X POST http://localhost:5199/login "${P[@]}"   --data-urlencode "Model.Email=x" --data-urlencode "Model.Password=1"          # 422

# validation request -> 200, and NOT a success
curl -s -b cj -X POST http://localhost:5199/login -H "X-Up-Version: 3.10.2"   -H "X-Up-Validate: Model.Email Model.Password" "${P[@]}"   --data-urlencode "Model.Email=x" --data-urlencode "Model.Password=1" | grep -c 'thành công'   # 0

# no antiforgery token -> rejected
curl -s -o /dev/null -w "%{http_code}
" -X POST http://localhost:5199/login   --data-urlencode _handler=login                                              # 400
```

- [x] An invalid login POST answers **422**, not 200, and the body carries the messages
- [x] A validating request answers 200, re-renders the form, and does **not** succeed
- [x] A POST without the antiforgery token is rejected with **400**
- [x] `X-Up-Fail-Target: body` keeps the nav in a 422 response; `.form-wrap` drops it

**Browser-observable**

- [x] Typing an invalid email sends `X-Up-Validate: Model.Email` and the error appears
      without any submit
- [x] **A validating request does not act.** Filling *valid* credentials triggers validation
      and the success state never appears — the guard is proven by the absence of the effect,
      not by the UI
- [x] `[up-disable]` disables the form mid-submit and it recovers afterwards
- [x] Filters autosubmit on change: 30 cards → 10
- [x] `[up-watch-delay]` collapses three rapid changes into **one** request, carrying the
      last value. Zero would have proven nothing — a cache hit looks identical — so the
      burst deliberately lands on a value never requested before

---

## Phase D · Layers ✅

**Automated — 20 checks**

- [x] `IsUpOverlay()` is false for no mode and for `root`, true for `modal` and `drawer`
- [x] `UpOriginMode` reads the layer that *issued* the request, which differs from `UpMode`
      exactly while an overlay is being opened
- [x] `X-Up-Context` parses as JSON; absent and `{}` both mean no context
- [x] **Malformed context degrades to null instead of throwing** — it is client-controlled
      data, so a bad value is a bad request, not a 500 in a page that merely read it
- [x] Accept and dismiss write different headers, and accepting never also dismisses
- [x] `UpOpenLayer()` sends `{}`; with options it passes render options through
- [x] `Vary` covers `X-Up-Context`, or two layers with different context share a cache entry

**Browser-observable — 10 checks**

- [x] `[up-layer=new]` opens `up-modal`
- [x] **The opener stays intact behind it** — the product page is not replaced. This is what
      makes it a subinteraction rather than a navigation
- [x] Accepting closes the overlay and the value reaches the opener (`chosen-size` → `M`)
- [x] `[up-on-accepted]` runs on the opener
- [x] Dismissing closes it and changes nothing
- [x] `X-Up-Open-Layer` opens a drawer from a link with no `[up-layer]` at all
- [x] The overlay request carries `X-Up-Mode` and the `[up-context]` the link set, and the
      server echoes the context back

**Open**

- [ ] Two overlays stacked, and closing the top one revealing the one below intact — the
      sample opens only one, which is also why ten `/layer-option` rows sit at `todo`

---

## Phase E · History, scrolling and focus ✅ (two items open)

**Automated — 8 checks**

- [x] `UpTitle` keeps the quotes: `"Playlist browser"`, not a bare string
- [x] and escapes non-ASCII, so a Vietnamese title stays ASCII-safe in a header while still
      decoding to the original
- [x] `UpLocation` is a plain URL, `UpMethod` is upper-cased
- [x] `_up_method` defaults to the request method, takes an explicit override, and is set for
      the whole site

**Browser-observable — 8 checks**

- [x] `X-Up-Title` sets the document title
- [x] `X-Up-Location` wins over the URL that was actually requested
- [x] `[up-history=false]` swaps content without touching the address bar
- [x] Infinite scroll: `.listing:after` **appends**, the first page stays put
- [x] It stops at the total and never goes past it
- [x] **No duplicates** — 30 distinct of 30. Appending the same slice twice is the real
      risk, and it is what the first version of this sample did
- [x] The `.more` region is **replaced** in the same response that appended — one target
      list doing two different jobs

**Open**

- [ ] Back and Forward restore the scroll position
- [ ] Keyboard focus lands in the new fragment rather than the top of the document

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
