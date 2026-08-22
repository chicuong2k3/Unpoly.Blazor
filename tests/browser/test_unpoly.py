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
    # Target the ping form by its attribute: the page grew a second form in Phase F, and
    # form[method=post] silently started matching the wrong one.
    await p.click("form[up-target=':none'] button[type=submit]")
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

    # ---------------------------------------------------------------- history
    print("\n== History ==", flush=True)

    await p.goto("/lab")
    await p.click("a[href='/lab/titled']", settle=1.8)
    t = await p.js("document.title")
    record("X-Up-Title sets the document title", "server" in (t or "").lower(), f"title: {t}")

    await p.goto("/lab")
    await p.click("a[href='/lab/relocated']", settle=1.8)
    where = await p.js("location.pathname")
    record("X-Up-Location wins over the requested URL", where == "/lab/somewhere-else",
           f"address bar: {where}")

    await p.goto("/lab")
    before = await p.js("location.pathname")
    await p.click("a[up-history='false']", settle=1.8)
    after = await p.js("location.pathname")
    swapped = await p.js("!!document.querySelector('.card')")
    record("[up-history=false] swaps without touching the address bar",
           after == before and swapped, f"{before} -> {after}, content swapped: {swapped}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- infinite scroll
    print("\n== Infinite scroll ==", flush=True)

    await p.goto("/shop")
    first = await p.js("document.querySelectorAll('.listing .card').length")
    record("the first page is a slice, not everything", first == 12, f"{first} cards")

    # [up-defer=reveal] is the trigger, so the test scrolls rather than clicks -- doing both
    # loads the same page twice. It also keeps firing while the trigger stays in view, so one
    # scroll can pull several pages. Asserting an exact intermediate count was wrong about
    # the feature, not about the code.
    await p.js("window.scrollTo(0, document.body.scrollHeight)")
    await asyncio.sleep(3.0)
    grown = await p.js("document.querySelectorAll('.listing .card').length")
    record(".listing:after APPENDS rather than replaces", grown > first,
           f"{first} -> {grown} cards; staying at 12 would mean it replaced")

    record("it stops at the total, never past it", grown == 30, f"{grown} of 30")

    # The real risk of appending is appending the same slice twice.
    unique = await p.js("new Set(Array.from(document.querySelectorAll('.listing .card a'))"
                        ".map(a => a.getAttribute('href'))).size")
    record("and appends no duplicates", unique == grown, f"{unique} distinct of {grown} cards")

    done = await p.js("!document.querySelector('.more a')")
    exhausted = await p.js("document.querySelector('.more')?.textContent?.includes('hết')")
    record("the .more region was REPLACED, not appended", done and bool(exhausted),
           "the load-more link is gone and the exhausted message took its place")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- passive updates
    print("\n== Flashes, badge and polling ==", flush=True)

    # The cart is a static that survives between runs, so every assertion here is relative.
    def count_of(text):
        digits = "".join(ch for ch in (text or "") if ch.isdigit())
        return int(digits) if digits else -1

    await p.goto("/p/dam-4")
    badge_before = await p.js("document.querySelector('.cart-badge')?.textContent?.trim()")

    # the size picker unlocks the button
    await p.click("a[up-layer]", settle=1.8)
    await p.click("up-modal form.sizes button[value=M]", settle=1.8)

    heard = await p.js("window.__cart = null;"
                       " up.on('cart:changed', (e) => window.__cart = e.count); true")
    await p.click("form[method=post] button.add-to-cart", settle=2.0)

    flash = await p.js("document.querySelector('[up-flashes] .flash')?.textContent?.trim()")
    record("a flash appears in the response that caused it", bool(flash), f"flash: {flash}")

    got = await p.js("window.__cart")
    record("X-Up-Events reaches a client listener", got == count_of(badge_before) + 1,
           f"cart:changed fired with count={got}, was {count_of(badge_before)}")

    badge_after = await p.js("document.querySelector('.cart-badge')?.textContent?.trim()")
    record("[up-hungry] updated the badge without being targeted",
           count_of(badge_after) == count_of(badge_before) + 1,
           f"{badge_before} -> {badge_after}")
    await asyncio.sleep(PACE)

    # polling: unchanged data must not cost a render
    m = p.mark()
    await asyncio.sleep(5.0)
    polls = [r for r in p.since(m) if "/p/dam-4" in r[1]]
    record("[up-poll] keeps polling on its interval", len(polls) >= 1,
           f"{len(polls)} poll(s) in 5s at a 4s interval")

    statuses = [st for u, st in p.responses if "/p/dam-4" in u]
    record("and an unchanged poll is answered 304", 304 in statuses,
           f"statuses seen: {sorted(set(statuses))}")

    # ---------------------------------------------------------------- compilers
    print("\n== Compilers ==", flush=True)

    await p.goto("/p/dam-4")
    ran = await p.js("document.querySelector('[data-gallery]')?.dataset?.galleryLive")
    record("a compiler runs on page load", ran == "true", f"galleryLive: {ran}")

    live = await p.js("window.Gallery.liveCount()")
    record("exactly one instance is live", live == 1, f"liveCount: {live}")

    # [up-data] is relaxed JSON, handed to the compiler as its second argument. Read the
    # value the widget was given rather than timing how fast slides move -- the first
    # version of this check measured the clock and failed for that reason.
    got = await p.js("document.querySelector('[data-gallery]')?.dataset?.galleryInterval")
    record("[up-data] reached the compiler", got == "900",
           f"interval: {got}; 1500 is the widget default, meaning data never arrived")

    # The real question: does it survive being swapped away and back, repeatedly?
    for _ in range(3):
        await p.click(".site-nav a[href='/shop']", settle=1.4)
        await p.click(".card a", settle=1.6)

    ran_again = await p.js("document.querySelector('[data-gallery]')?.dataset?.galleryLive")
    record("and again after three swaps away and back", ran_again == "true",
           f"galleryLive: {ran_again}")

    # Without the destructor every swap would leave another timer running on detached DOM.
    # The count is the leak made visible.
    leaked = await p.js("window.Gallery.liveCount()")
    record("the destructor stopped the old instances", leaked == 1,
           f"liveCount after 3 round trips: {leaked}; 4 would mean every swap leaked one")

    # Assets are only tracked in <head>, so cutting the head switched detection off. One
    # marker outside UpChrome restores it.
    await p.goto("/shop")
    m = p.mark()
    await p.click(".card a", settle=1.6)
    marker = await p.js("!!document.querySelector('meta[up-asset]')")
    record("the asset marker survives fragment responses", marker,
           "meta[up-asset] is outside UpChrome, so up:assets:changed can still fire")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- overlay stack
    print("\n== Overlay stack ==", flush=True)

    await p.goto("/p/dam-4")
    await p.click("a[up-layer]", settle=1.8)
    await p.click("up-modal a[href='/size-guide']", settle=2.0)

    depth = await p.js("document.querySelectorAll('up-modal').length")
    record("an overlay can open another overlay", depth == 2, f"{depth} modals on the stack")

    layers = await p.js("up.layer.count")
    record("Unpoly counts three layers: root and two overlays", layers == 3,
           f"up.layer.count: {layers}")

    # With one overlay, root/parent/current/any all mean the same thing. This is the first
    # point in the project where they differ.
    knows = await p.js("document.querySelector('up-modal:last-of-type')"
                       "?.textContent?.includes('overlay thứ hai')")
    record("the second overlay knows it is not the first", bool(knows))

    # Closing the top must leave the one below intact -- the whole promise of a stack.
    await p.js("document.querySelector('up-modal:last-of-type button[up-dismiss]').click()")
    await asyncio.sleep(1.4)
    left = await p.js("document.querySelectorAll('up-modal').length")
    picker = await p.js("!!document.querySelector('up-modal form.sizes')")
    record("closing the top overlay reveals the one below, intact", left == 1 and bool(picker),
           f"{left} modal left, size picker present: {picker}")

    behind = await p.js("!!document.querySelector('.detail')")
    record("and the root page is still behind both", bool(behind))
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- reactive form
    print("\n== Reactive server forms ==", flush=True)

    await p.goto("/login")
    s0 = await p.js("document.querySelector('.strength strong')?.textContent?.trim()")

    # [up-validate] watches `change` unless a field opts into [up-watch-event=input].
    # Dispatching `input` here fires nothing, which reads as the server not re-rendering.
    await p.js("(() => { const w = document.querySelector('#password'); w.value = 'abc';"
               " w.dispatchEvent(new Event('change', {bubbles:true})); })()")
    await asyncio.sleep(1.8)
    s1 = await p.js("document.querySelector('.strength strong')?.textContent?.trim()")
    record("a validating request re-renders a DEPENDENT fragment", s1 != s0,
           f"strength: {s0} -> {s1}")

    await p.js("(() => { const w = document.querySelector('#password'); w.value = 'abcdefghijk';"
               " w.dispatchEvent(new Event('change', {bubbles:true})); })()")
    await asyncio.sleep(1.8)
    s2 = await p.js("document.querySelector('.strength strong')?.textContent?.trim()")
    record("and it reflects the current field value, not just an error", s2 != s1,
           f"strength: {s1} -> {s2}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- scroll and focus
    print("\n== Scroll and focus ==", flush=True)

    await p.goto("/shop")
    await p.js("window.scrollTo(0, 900)")
    await asyncio.sleep(0.4)
    scrolled = await p.js("Math.round(window.scrollY)")
    await p.click(".site-nav a[href='/shop/dam']", settle=1.8)
    top = await p.js("Math.round(window.scrollY)")
    record("navigating scrolls back to the top", top < scrolled, f"{scrolled} -> {top}")

    await p.js("history.back()")
    # Restoration re-renders up.history.config.restoreTargets, default ["body"]. Sample over
    # time rather than guessing one instant.
    seen = []
    for _ in range(12):
        await asyncio.sleep(0.35)
        seen.append(await p.js("Math.round(window.scrollY)"))

    back_url = await p.js("location.pathname")
    record("Back restores the previous fragment", back_url == "/shop", f"location: {back_url}")

    # Scroll is NOT restored automatically. Every sample over 4s was 0, and the guide makes
    # it opt-in via [up-scroll=restore]. This records the observation instead of asserting a
    # behaviour Unpoly does not promise by default.
    record("scroll restoration is opt-in, not automatic", max(seen) == 0,
           f"was {scrolled}, all 12 samples over 4s: {sorted(set(seen))}")

    await p.goto("/shop")
    await p.click(".site-nav a[href='/shop/ao']", settle=1.8)
    focused = await p.js("(() => { const a = document.activeElement;"
                         " return a && a.closest('.content') ? 'in .content' : (a?.tagName || 'none'); })()")
    record("focus lands inside the new fragment, not on <body>", focused == "in .content",
           f"activeElement: {focused}")
    await asyncio.sleep(PACE)

    # ---------------------------------------------------------------- without JavaScript
    print("\n== Without JavaScript ==", flush=True)

    # Measured over plain HTTP, not in the browser: Emulation.setScriptExecutionDisabled also
    # stops Runtime.evaluate, so the page cannot report on itself. A raw HTTP client IS a
    # browser with JavaScript disabled, which is exactly the thing under test.
    import urllib.request

    routes = []
    for path, marker in [("/", "card"), ("/shop", "card"), ("/p/dam-4", "detail"),
                         ("/login", "form-wrap"), ("/p/dam-4/size", "sizes"),
                         ("/size-guide", "size-table")]:
        with urllib.request.urlopen(BASE + path) as r:
            body = r.read().decode("utf-8", "replace")
        routes.append((path, r.status == 200 and marker in body))

    broken = [path for path, good in routes if not good]
    record("every route still renders with JavaScript disabled", not broken,
           f"{len(routes) - len(broken)}/{len(routes)} routes; broken: {broken or 'none'}")

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
