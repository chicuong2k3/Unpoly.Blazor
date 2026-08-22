"""Browser checks for the Unpoly.Blazor sample, driven by browser-use over CDP.

Counting note: cdp-use delivers each Network.requestWillBeSent twice, with the SAME
requestId. Recording both makes every count double, which read as "Unpoly fires two
requests" until the sample's own request log showed one line per action. Requests are
deduplicated by requestId below; do not remove that.

These cover what unit checks cannot see: whether Unpoly actually swaps, what it puts on
the wire, and the behaviour VERIFY.md has been carrying as open browser items.

Run the sample first, in another terminal:
    dotnet run --no-build --project sample/Jubin --urls http://localhost:5199

Then:
    python tests/browser/test_unpoly.py            # visible Chrome, paced to watch
    python tests/browser/test_unpoly.py --headless # fast, for CI
"""
import asyncio, sys, json

BASE = "http://localhost:5199"
HEADLESS = "--headless" in sys.argv
PACE = 0.2 if HEADLESS else 1.2       # slow enough to follow along

results = []


def record(name, ok, detail=""):
    results.append((name, ok, detail))
    mark = "PASS" if ok else "FAIL"
    print(f"  [{mark}] {name}" + (f"  -- {detail}" if detail else ""), flush=True)


class Probe:
    """Thin CDP wrapper: evaluate JS, send real mouse events, record traffic."""

    def __init__(self, session):
        self.session = session
        self.requests = []          # (method, url, headers)
        self.responses = []         # (url, status)
        self._seen_ids = set()      # cdp-use delivers each event twice, same requestId
        self._registered = False

    async def attach(self):
        self.cdp = await self.session.get_or_create_cdp_session()
        self.client = self.cdp.cdp_client
        self.sid = self.cdp.session_id

        # Register the handlers exactly once. attach() runs after every navigation, and
        # registering again would record each event twice -- which silently doubled every
        # count in the first run of this suite.
        if not self._registered:
            def on_request(event, session_id=None):
                rid = event.get("requestId")
                if rid in self._seen_ids:
                    return
                self._seen_ids.add(rid)
                r = event.get("request", {})
                self.requests.append((r.get("method"), r.get("url", ""), r.get("headers", {})))

            def on_response(event, session_id=None):
                r = event.get("response", {})
                self.responses.append((r.get("url", ""), r.get("status")))

            self.client.register.Network.requestWillBeSent(on_request)
            self.client.register.Network.responseReceived(on_response)
            self._registered = True

        await self.client.send.Network.enable(session_id=self.sid)
        await self.client.send.Runtime.enable(session_id=self.sid)

    async def js(self, expr):
        res = await self.client.send.Runtime.evaluate(
            params={"expression": expr, "returnByValue": True, "awaitPromise": True},
            session_id=self.sid,
        )
        return res.get("result", {}).get("value")

    async def goto(self, path):
        await self.session.navigate_to(BASE + path)
        await asyncio.sleep(1.5)
        await self.attach()          # navigation can hand us a new target

    async def center(self, selector):
        return await self.js(
            f"(() => {{ const e = document.querySelector({json.dumps(selector)});"
            f" if (!e) return null; e.scrollIntoView({{block:'center'}});"
            f" const r = e.getBoundingClientRect();"
            f" return [Math.round(r.left + r.width/2), Math.round(r.top + r.height/2)]; }})()")

    async def _mouse(self, kind, x, y, buttons=1):
        await self.client.send.Input.dispatchMouseEvent(
            params={"type": kind, "x": x, "y": y, "button": "left",
                    "buttons": buttons, "clickCount": 1},
            session_id=self.sid)

    async def hover(self, selector, hold=0.4):
        c = await self.center(selector)
        if not c: return False
        await self._mouse("mouseMoved", c[0], c[1], buttons=0)
        await asyncio.sleep(hold)
        return True

    async def press_only(self, selector, hold=0.6):
        """mousedown without mouseup -- the only way to see [up-instant]."""
        c = await self.center(selector)
        if not c: return False
        await self._mouse("mouseMoved", c[0], c[1], buttons=0)
        await self._mouse("mousePressed", c[0], c[1])
        await asyncio.sleep(hold)
        return True

    async def release(self, selector):
        c = await self.center(selector)
        if c: await self._mouse("mouseReleased", c[0], c[1])

    async def click(self, selector, settle=1.4):
        c = await self.center(selector)
        if not c: return False
        await self._mouse("mouseMoved", c[0], c[1], buttons=0)
        await self._mouse("mousePressed", c[0], c[1])
        await self._mouse("mouseReleased", c[0], c[1])
        await asyncio.sleep(settle)
        return True

    async def poll(self, expr, timeout=3.0, step=0.05):
        """True if `expr` becomes truthy at any point. Needed for states that exist only
        while a request is in flight -- a settled page never shows them."""
        waited = 0.0
        while waited < timeout:
            if await self.js(expr):
                return True
            await asyncio.sleep(step)
            waited += step
        return False

    async def mouse_at(self, selector):
        c = await self.center(selector)
        if c:
            await self._mouse("mouseMoved", c[0], c[1], buttons=0)
        return c

    def since(self, mark, path_contains=None, exact=None):
        out = self.requests[mark:]
        if exact:
            out = [r for r in out if r[1] == BASE + exact]
        elif path_contains:
            out = [r for r in out if path_contains in r[1]]
        return out

    def mark(self):
        return len(self.requests)

    def status_of(self, path_contains):
        hits = [s for u, s in self.responses if path_contains in u]
        return hits[-1] if hits else None


async def main():
    from browser_use.browser.session import BrowserSession
    from browser_use.browser.profile import BrowserProfile

    session = BrowserSession(browser_profile=BrowserProfile(
        headless=HEADLESS, viewport={"width": 1360, "height": 900}))
    await session.start()
    p = Probe(session)
    await p.goto("/")

    # ---------------------------------------------------------------- fragments
    print("\n== Fragment swaps ==", flush=True)

    await p.goto("/shop")
    node_before = await p.js("document.querySelector('.site-nav').dataset.probe = 'kept'")
    m = p.mark()
    await p.click(".site-nav a[href='/shop/dam']")

    reqs = p.since(m, "/shop/dam")
    hdr = reqs[0][2] if reqs else {}
    up_target = {k.lower(): v for k, v in hdr.items()}.get("x-up-target")
    record("a link sends X-Up-Target", up_target == ".content", f"X-Up-Target: {up_target}")

    heading = await p.js("document.querySelector('.page-head h2')?.textContent?.trim()")
    record("the fragment was swapped", heading == "Đầm", f"heading: {heading}")

    kept = await p.js("document.querySelector('.site-nav')?.dataset?.probe")
    record("the nav element survived (no full reload)", kept == "kept", f"data-probe: {kept}")

    cur = await p.js("document.querySelector('.site-nav a.up-current')?.getAttribute('href')")
    record("[up-nav] moved .up-current", cur == "/shop/dam", f".up-current: {cur}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- caching
    print("\n== Caching and revalidation ==", flush=True)

    await p.goto("/shop")
    await p.click(".site-nav a[href='/shop/ao']")
    await asyncio.sleep(0.5)

    # (a) cached less than cacheExpireAge (15s) ago: the entry is FRESH.
    m = p.mark()
    await p.click(".site-nav a[href='/shop/dam']")
    await asyncio.sleep(0.5)
    await p.click(".site-nav a[href='/shop/ao']")
    fresh = len(p.since(m, "/shop/ao"))
    record("a fresh cache hit makes NO request", fresh == 0,
           f"{fresh} request(s) within cacheExpireAge")

    # (b) let the entry expire, then revisit: Unpoly renders the stale copy and refetches.
    print("     (waiting 17s for cacheExpireAge to pass...)", flush=True)
    await asyncio.sleep(17)
    m = p.mark()
    await p.click(".site-nav a[href='/shop/dam']")
    await asyncio.sleep(1.5)
    stale = len(p.since(m, exact="/shop/dam"))
    record("an EXPIRED cache hit revalidates exactly once", stale == 1,
           f"{stale} request(s) after expiry -- the second render pass")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- preload
    print("\n== Preload and instant ==", flush=True)

    await p.goto("/lab")
    m = p.mark()
    await p.hover("a[href='/lab/slow?case=preload']", hold=0.8)
    hits = p.since(m, exact="/lab/slow?case=preload")
    n = len(hits)
    record("[up-preload] fires on hover past 90ms", n == 1, f"{n} request(s) while hovering")

    fired = len([r for r in p.requests if r[1] == BASE + "/lab/slow?case=preload-insert"])
    record("[up-preload=insert] fires without interaction", fired == 1,
           f"{fired} request(s) since the page rendered")
    await asyncio.sleep(PACE)

    await p.goto("/lab")
    m = p.mark()
    ok = await p.press_only("a[href='/lab/slow?case=instant']", hold=0.8)
    n = len(p.since(m, exact="/lab/slow?case=instant"))
    record("[up-instant] fires on mousedown, before release", ok and n == 1,
           f"{n} request(s) with the button still held")
    await p.release("a[href='/lab/slow?case=instant']")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- forms
    print("\n== Forms ==", flush=True)

    await p.goto("/login")
    m = p.mark()
    await p.js("(() => { const f = document.querySelector('#email');"
               " f.value = 'khong-phai-email';"
               " f.dispatchEvent(new Event('input', {bubbles:true})); })()")
    await asyncio.sleep(1.6)

    val = [r for r in p.since(m, "/login")
           if any(k.lower() == "x-up-validate" for k in r[2])]
    field = ""
    if val:
        field = {k.lower(): v for k, v in val[0][2].items()}["x-up-validate"]
    record("[up-validate] sends X-Up-Validate on input", len(val) >= 1,
           f"X-Up-Validate: {field or 'absent'}")

    msg = await p.js("document.querySelector('.validation-message')?.textContent?.trim()")
    record("the error rendered without a submit", bool(msg), f"message: {msg}")
    await asyncio.sleep(PACE)

    await p.js("(() => { const p = document.querySelector('#password');"
               " p.value = '1'; p.dispatchEvent(new Event('input', {bubbles:true})); })()")
    await asyncio.sleep(0.8)
    await p.click("form button[type=submit]")
    st = p.status_of("/login")
    record("an invalid submit answers 422", st == 422, f"status: {st}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- :none
    print("\n== Targeting nothing ==", flush=True)

    await p.goto("/p/dam-4")
    m = p.mark()
    await p.click("form[method=post] button[type=submit]")
    st = p.status_of("/p/dam-4")
    record("[up-target=:none] answers 204", st == 204, f"status: {st}")
    still = await p.js("!!document.querySelector('.detail')")
    record("and the page was left alone", bool(still), f".detail present: {still}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- chrome
    print("\n== Chrome and Provides ==", flush=True)

    await p.goto("/lab")
    m = p.mark()
    await p.click("a[up-target='.content, .site-nav']")
    reqs = p.since(m, "/shop")
    t = {k.lower(): v for k, v in reqs[0][2].items()}.get("x-up-target") if reqs else None
    record("a chrome selector reaches the server", t == ".content, .site-nav",
           f"X-Up-Target: {t}")
    nav = await p.js("!!document.querySelector('.site-nav')")
    record("UpChrome Provides kept the nav alive", bool(nav), f".site-nav present: {nav}")

    # ---------------------------------------------------------------- progress bar
    print("\n== Progress bar ==", flush=True)

    await p.goto("/lab")
    c = await p.center("a[href='/lab/slow?case=progress']")
    await p._mouse("mouseMoved", c[0], c[1], buttons=0)
    await p._mouse("mousePressed", c[0], c[1])
    await p._mouse("mouseReleased", c[0], c[1])
    shown = await p.poll("!!document.querySelector('up-progress-bar')", timeout=2.5)
    record("the progress bar appears on a slow response", shown,
           "up-progress-bar seen while the 1.2s route was loading")
    await asyncio.sleep(1.5)
    gone = await p.js("!document.querySelector('up-progress-bar')")
    record("and disappears once it settles", bool(gone))

    await p.goto("/lab")
    c = await p.center("a[href='/lab/slow?case=background']")
    await p._mouse("mouseMoved", c[0], c[1], buttons=0)
    await p._mouse("mousePressed", c[0], c[1])
    await p._mouse("mouseReleased", c[0], c[1])
    bg = await p.poll("!!document.querySelector('up-progress-bar')", timeout=2.0)
    record("[up-background] shows no bar even when slow", not bg,
           "no up-progress-bar for a background request")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- staleness
    print("\n== Cache staleness after a mutation ==", flush=True)

    await p.goto("/shop")
    before = await p.js("document.querySelector('.meta')?.textContent?.trim()")
    await p.click("form.refresh button[type=submit]", settle=1.8)
    after = await p.js("document.querySelector('.meta')?.textContent?.trim()")
    record("a mutation is reflected, not served stale", before != after,
           f"version {before} -> {after}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- form state
    print("\n== Form state while in flight ==", flush=True)

    await p.goto("/login")
    await p.js("(() => { const e=document.querySelector('#email'); e.value='a@b.com';"
               " e.dispatchEvent(new Event('input',{bubbles:true}));"
               " const w=document.querySelector('#password'); w.value='secret123';"
               " w.dispatchEvent(new Event('input',{bubbles:true})); })()")
    await asyncio.sleep(1.2)

    c = await p.center("form button[type=submit]")
    await p._mouse("mouseMoved", c[0], c[1], buttons=0)
    await p._mouse("mousePressed", c[0], c[1])
    await p._mouse("mouseReleased", c[0], c[1])
    busy = await p.poll("(() => { const f=document.querySelector('form');"
                        " return f && (f.matches('[aria-busy=true]')"
                        " || !!f.querySelector('input:disabled, button:disabled')); })()",
                        timeout=2.5)
    record("[up-disable] disables the form while in flight", busy,
           "form was aria-busy or had disabled controls mid-submit")
    await asyncio.sleep(1.5)
    recovered = await p.poll("!document.querySelector('form input:disabled')", timeout=2.0)
    record("and it recovers afterwards", recovered)
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- validation writes nothing
    print("\n== A validating request must not act ==", flush=True)

    await p.goto("/login")
    await p.js("(() => { const e=document.querySelector('#email'); e.value='a@b.com';"
               " e.dispatchEvent(new Event('input',{bubbles:true}));"
               " const w=document.querySelector('#password'); w.value='secret123';"
               " w.dispatchEvent(new Event('input',{bubbles:true})); })()")
    await asyncio.sleep(1.8)
    succeeded = await p.js("document.body.textContent.includes('thành công')")
    record("a VALID field passing validation does not log the user in", not succeeded,
           "the success state never appeared, though the values were valid")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- autosubmit
    print("\n== Autosubmit filter ==", flush=True)

    await p.goto("/shop")
    n_before = await p.js("document.querySelectorAll('.card').length")
    m = p.mark()
    await p.js("(() => { const s=document.querySelector(\'select[name=maxPrice]\');"
               " s.value='400000'; s.dispatchEvent(new Event('change',{bubbles:true})); })()")
    await asyncio.sleep(2.0)
    n_after = await p.js("document.querySelectorAll('.card').length")
    fired = len(p.since(m, "/shop"))
    record("[up-autosubmit] submits on change", fired >= 1, f"{fired} request(s)")
    record("and the grid actually filtered", n_after < n_before,
           f"{n_before} cards -> {n_after}")

    # Three rapid changes landing on a value not requested before, so a cache hit cannot
    # masquerade as debouncing. Without [up-watch-delay] this would be three requests.
    m = p.mark()
    await p.js("(() => { const s=document.querySelector('select[name=maxPrice]');"
               " for (const v of ['600000','400000','900000']) {"
               "   s.value=v; s.dispatchEvent(new Event('change',{bubbles:true})); } })()")
    await asyncio.sleep(2.5)
    burst = len(p.since(m, "/shop"))
    record("[up-watch-delay] collapses a burst into one request", burst == 1,
           f"3 rapid changes -> {burst} request(s); 0 proves nothing (cache), 3 means no debounce")
    landed = await p.js("document.querySelector('select[name=maxPrice]')?.value")
    record("and the value that was sent is the last one", landed == "900000", f"value: {landed}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- layers
    print("\n== Layers ==", flush=True)

    await p.goto("/p/dam-4")
    before = await p.js("document.querySelector('.chosen-size')?.textContent?.trim()")
    await p.click("a[up-layer]", settle=1.8)

    opened = await p.js("!!document.querySelector('up-modal')")
    record("[up-layer=new] opens a modal", opened, "up-modal is in the DOM")

    behind = await p.js("!!document.querySelector('.detail')")
    record("the opener stays intact behind it", behind,
           "the product page was not replaced -- that is what makes it a subinteraction")

    m = p.mark()
    hdr = None
    reqs = p.since(m)
    await p.click("up-modal form.sizes button[value=M]", settle=1.8)

    closed = await p.js("!document.querySelector('up-modal')")
    record("accepting closes the overlay", closed)

    after = await p.js("document.querySelector('.chosen-size')?.textContent?.trim()")
    record("and the value reaches the opener", after == "M", f"chosen-size: {before} -> {after}")

    enabled = await p.js("!document.querySelector('.add-to-cart')?.disabled")
    record("[up-on-accepted] ran on the opener", bool(enabled), "add-to-cart became enabled")
    await asyncio.sleep(PACE)

    # dismissal is not acceptance
    await p.goto("/p/dam-4")
    await p.click("a[up-layer]", settle=1.8)
    await p.js("document.querySelector('up-modal button[up-dismiss]').click()")
    await asyncio.sleep(1.2)
    gone = await p.js("!document.querySelector('up-modal')")
    unchanged = await p.js("document.querySelector('.chosen-size')?.textContent?.trim()")
    record("dismissing closes without a value", gone and unchanged == "chưa chọn",
           f"chosen-size still: {unchanged}")
    await asyncio.sleep(PACE)

    # the server decides
    await p.goto("/p/dam-4")
    await p.click("a[href*='serverOpens=1']", settle=2.0)
    drawer = await p.js("!!document.querySelector('up-drawer')")
    record("X-Up-Open-Layer opens a drawer the link never asked for", drawer,
           "up-drawer is in the DOM though the link had no [up-layer]")
    await asyncio.sleep(PACE)

    # mode and context on the wire
    await p.goto("/lab")
    m = p.mark()
    await p.click("a[up-context]", settle=1.8)
    reqs = [r for r in p.since(m) if "/size" in r[1]]
    h = {k.lower(): v for k, v in reqs[0][2].items()} if reqs else {}
    record("the overlay request carries X-Up-Mode", h.get("x-up-mode") == "modal",
           f"X-Up-Mode: {h.get('x-up-mode')}")
    record("and X-Up-Context set by the link", "flavour" in (h.get("x-up-context") or ""),
           f"X-Up-Context: {h.get('x-up-context')}")
    shown = await p.js("document.querySelector('up-modal')?.textContent?.includes('flavour')")
    record("the server read that context and echoed it", bool(shown))

    # ---------------------------------------------------------------- summary
    ok = sum(1 for _, o, _ in results if o)
    print(f"\n{'=' * 58}\n{ok}/{len(results)} browser checks passed\n{'=' * 58}")
    for n, o, d in results:
        if not o:
            print(f"  FAILED: {n}  ({d})")

    if not HEADLESS:
        print("\nChrome stays open 20s so you can look around...", flush=True)
        await asyncio.sleep(20)

    await session.kill()
    return 0 if ok == len(results) else 1


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
