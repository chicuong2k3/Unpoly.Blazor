"""Browser checks for the Unpoly.Blazor sample, driven by browser-use over CDP.

OPEN: every navigation-triggered request arrives at the server TWICE, with byte-identical
X-Up-* headers. Confirmed from both sides -- CDP capture and the sample's own request log,
which shows consecutive pairs. Not a duplicate CDP listener (registering once changed
nothing) and not a substring filter (exact URL matching changed nothing). Unexplained;
the assertions below check that a feature fires, not how many times.

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
    record("an EXPIRED cache hit revalidates", stale >= 1,
           f"{stale} request(s) after expiry -- this is the second render pass")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- preload
    print("\n== Preload and instant ==", flush=True)

    await p.goto("/lab")
    m = p.mark()
    await p.hover("a[href='/lab/slow?case=preload']", hold=0.8)
    hits = p.since(m, exact="/lab/slow?case=preload")
    n = len(hits)
    record("[up-preload] fires on hover past 90ms", n >= 1,
           f"{n} request(s) while hovering (see OPEN: duplicate requests)")

    fired = len([r for r in p.requests if r[1] == BASE + "/lab/slow?case=preload-insert"])
    record("[up-preload=insert] fires without interaction", fired >= 1,
           f"{fired} request(s) since the page rendered")
    await asyncio.sleep(PACE)

    await p.goto("/lab")
    m = p.mark()
    ok = await p.press_only("a[href='/lab/slow?case=instant']", hold=0.8)
    n = len(p.since(m, exact="/lab/slow?case=instant"))
    record("[up-instant] fires on mousedown, before release", ok and n >= 1,
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
