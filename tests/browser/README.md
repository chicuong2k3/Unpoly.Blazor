# Browser checks

What unit checks cannot see: whether Unpoly actually swaps, what it puts on the wire, and
the behaviour `VERIFY.md` was carrying as open browser items.

Driven by [browser-use](https://github.com/browser-use/browser-use) over CDP. No LLM and no
API key: `BrowserSession` is used purely as a typed CDP client.

```bash
# terminal 1
dotnet run --no-build --project sample/Jubin --urls http://localhost:5199

# terminal 2
python tests/browser/test_unpoly.py             # visible Chrome, paced to watch
python tests/browser/test_unpoly.py --headless  # fast
```

Exit code is non-zero if any check fails.

## Why real mouse events

`[up-instant]` fires on **mousedown**, so `element.click()` cannot distinguish it from a
normal click. `[up-preload]` needs a hover held past `up.link.config.preloadDelay` (90ms).
Both go through `Input.dispatchMouseEvent`, with `press_only()` deliberately never sending
the release.

## Two findings from the first run

**Cache.** "One click, two requests" was wrong as a blanket claim. Within `cacheExpireAge`
(15s) a cached click makes **no request at all**; only an **expired** entry is rendered
stale and then refetched. That is the second render pass.

**Counting.** cdp-use delivers each `Network.requestWillBeSent` **twice**, with the same
`requestId`. Recording both doubled every count and read as "Unpoly fires two requests per
action" — a finding that survived two wrong explanations before the sample's own request
log settled it: three actions, three lines. `Probe` deduplicates by `requestId`; removing
that reintroduces the phantom.

With honest counts, every action costs exactly one request: preload, instant,
preload-on-insert, and the revalidation of an expired cache entry.
