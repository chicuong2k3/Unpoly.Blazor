# VERIFY.md

What must actually pass before a phase counts as done.

`TASKS.md` says *what to build*. This file says *how you know it works*. A phase is never
done on code review — every item below is a command to run or a thing to watch happen.

## Rules

- **Automated** items live in `tests/Unpoly.Blazor.Tests/UnpolyTests.cs` (xunit). They cover
  logic that can go wrong silently. Run: `dotnet test`
- **Server-observable** items are `curl` commands with an expected output. They cover the
  wire format, which unit tests cannot see.
- **Browser-observable** items need a human watching. They cover behaviour no assertion
  reaches: focus, scroll, timing, whether a thing *feels* broken.
- If an item cannot be verified, say so in the phase's notes rather than ticking it. An
  untested tick is worse than an open box.

**Browser-observable** items are automated in `tests/Unpoly.Blazor.BrowserTests`
(Playwright for .NET), and run in the same command as everything else:

```bash
dotnet test          # 58 tests
HEADED=1 dotnet test # watch the browser do it
```

A box below is ticked only if a test covers it; anything out of reach stays open with a note.

The browser tests start the sample themselves. To poke at it by hand:

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
- [x] Screenshots are a `page.ScreenshotAsync` call away in any browser test. The visual
      judgement is the user's;
      the tooling no longer blocks it

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

- [x] **ANSWERED by `CachingTests`.** A click on a **fresh** cached URL
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

- [x] Two overlays stacked (`up.layer.count` is 3: root plus two), and closing the top one
      reveals the one below intact, with the root page still behind both. This is what makes
      `[up-layer=root|parent|current|any]` mean four different things

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

- [x] Back restores the previous fragment
- [x] Keyboard focus lands **inside** the new fragment, not on `<body>`

**Resolved as NOT true**

- [x] ~~Back restores the scroll position~~ — it does not, by default. All twelve samples
      over four seconds were 0. The guide makes it opt-in via `[up-scroll=restore]`, and
      nothing here opts in. Recorded rather than asserted

---

## Phase F · Status and passive updates ✅

**Automated — 7 checks**

- [x] `UpEmit` writes a JSON array with a `type`, and a second call **accumulates** rather
      than replacing — two things can happen in one response
- [x] An event needs nothing but a `type`
- [x] Non-ASCII is escaped: Unpoly states that headers may only carry US-ASCII, so a
      Vietnamese message would otherwise be an invalid header
- [x] `layer: "current"` passes through so the event lands on the overlay

**Browser-observable — 5 checks**

- [x] A flash appears in the response that caused it
- [x] `X-Up-Events` reaches a client listener with the new count
- [x] `[up-hungry]` updates the cart badge without ever being targeted
- [x] `[up-poll]` keeps polling on its interval
- [x] **An unchanged poll is answered 304** — statuses seen are `[200, 304]`. This is the
      Phase B conditional-request work paying off

**Three rules for `[up-hungry]`, each found by a failing check**

1. Never inside skippable chrome — Unpoly does not add hungry selectors to `X-Up-Target`,
   so `UpChrome`'s `Provides` never fires for one
2. It needs a derivable selector: `[id]`, not a bare class
3. It must not depend on a page handler's effect — in Blazor SSR the layout renders before
   the page's handler runs, so the badge swaps correctly and shows a **stale** number, which
   looks exactly like `[up-hungry]` being broken. POST-redirect-GET is the fix

---

## Phase G · JavaScript layer ✅

**Browser-observable — 6 checks**

- [x] A compiler runs on page load
- [x] Exactly one widget instance is live
- [x] `[up-data]` reaches the compiler — the check reads the value the widget was handed,
      not how fast slides move. The first version timed the clock and failed for that reason
- [x] The compiler runs again after **three** swaps away and back, not just one
- [x] **The destructor stopped the old instances** — `liveCount()` is 1, not 4. Without it
      every swap leaves a timer running against detached DOM, which nothing visible reveals
- [x] The asset marker survives fragment responses

**Resolved**

- [x] Whether fragment responses can support asset tracking. They can: assets are only
      tracked in `<head>`, so one `meta[up-asset]` outside `UpChrome` restores it. Cutting
      the head had silently disabled `up:assets:changed` since Phase B

**Not reachable here**

- [ ] reCAPTCHA re-initialises on a swapped login form — needs a real site key. The
      mechanism it would exercise is already covered by the stand-in widget: a third-party
      `init`/`destroy` API, re-initialised by a compiler, cleaned up by a destructor

---

## Cross-cutting, re-check at every phase

- [x] `dotnet build` — 0 errors, 0 warnings
- [x] `dotnet test` — 58 tests, all green
- [x] `SKILL.md` carries no "not available yet" list any more — nothing throws
- [x] **No route stopped working with JavaScript disabled** — 6/6 routes render over plain
      HTTP. Measured with `urllib`, not the browser: `setScriptExecutionDisabled` also stops
      `Runtime.evaluate`, so the page cannot report on itself. A raw HTTP client *is* a
      browser with JavaScript off
