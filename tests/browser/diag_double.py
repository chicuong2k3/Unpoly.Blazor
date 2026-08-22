"""Diagnose: why does one navigation reach the server twice?"""
import asyncio, sys, json

BASE = "http://localhost:5199"
sys.path.insert(0, __file__.rsplit("\\", 1)[0] if "\\" in __file__ else ".")


async def main():
    from browser_use.browser.session import BrowserSession
    from browser_use.browser.profile import BrowserProfile

    session = BrowserSession(browser_profile=BrowserProfile(headless=True))
    await session.start()
    cdp = await session.get_or_create_cdp_session()
    client, sid = cdp.cdp_client, cdp.session_id

    sent = []
    client.register.Network.requestWillBeSent(
        lambda e, session_id=None: sent.append((
            e.get("requestId"), e.get("request", {}).get("url", ""),
            e.get("initiator", {}).get("type"), e.get("loaderId"))))
    await client.send.Network.enable(session_id=sid)
    await client.send.Runtime.enable(session_id=sid)

    async def js(expr):
        r = await client.send.Runtime.evaluate(
            params={"expression": expr, "returnByValue": True, "awaitPromise": True},
            session_id=sid)
        return r.get("result", {}).get("value")

    await session.navigate_to(BASE + "/lab")
    await asyncio.sleep(2.5)

    print("=" * 62)
    print("1. Is Unpoly loaded once?")
    print("   <script src=unpoly>:", await js("document.querySelectorAll('script[src*=\\\"unpoly\\\"]').length"))
    print("   up.version          :", await js("up.version"))
    print("   inline config script:", await js(
        "Array.from(document.querySelectorAll('script:not([src])'))"
        ".filter(s => s.textContent.includes('up.protocol.config')).length"))

    print("\n2. Are the config arrays duplicated?")
    for name in ["up.link.config.followSelectors", "up.link.config.preloadSelectors",
                 "up.link.config.instantSelectors", "up.form.config.submitSelectors",
                 "up.fragment.config.mainTargets"]:
        print(f"   {name:38} = {await js(f'JSON.stringify({name})')}")

    print("\n3. One hover -- what exactly is sent?")
    sent.clear()
    c = await js("(() => { const e = document.querySelector(\"a[href='/lab/slow?case=preload']\");"
                 " e.scrollIntoView({block:'center'}); const r = e.getBoundingClientRect();"
                 " return [Math.round(r.left+r.width/2), Math.round(r.top+r.height/2)]; })()")
    await client.send.Input.dispatchMouseEvent(
        params={"type": "mouseMoved", "x": c[0], "y": c[1], "button": "none", "buttons": 0},
        session_id=sid)
    await asyncio.sleep(1.5)

    hits = [s for s in sent if "case=preload" in s[1] and "insert" not in s[1]]
    print(f"   requests: {len(hits)}")
    for rid, url, init, loader in hits:
        print(f"     requestId={rid}  initiator={init}  loaderId={loader}")
    print("   distinct requestIds:", len({h[0] for h in hits}))

    print("\n4. Does Unpoly itself think it made one request?")
    print("   up.network.queue count :", await js(
        "typeof up.network !== 'undefined' ? String(up.network.isBusy?.()) : 'n/a'"))
    print("   cache entries for URL  :", await js(
        "(() => { try { return String(up.cache?.get?.({url:'/lab/slow?case=preload'}) !== undefined) }"
        " catch(e) { return 'err: ' + e.message } })()"))

    print("\n5. up:request:load events seen by the page itself")
    await js("window.__loads = []; up.on('up:request:load', e => window.__loads.push(e.request.url));")
    sent.clear()
    c = await js("(() => { const e = document.querySelector(\"a[href='/lab/slow?case=instant']\");"
                 " e.scrollIntoView({block:'center'}); const r = e.getBoundingClientRect();"
                 " return [Math.round(r.left+r.width/2), Math.round(r.top+r.height/2)]; })()")
    await client.send.Input.dispatchMouseEvent(
        params={"type": "mouseMoved", "x": c[0], "y": c[1], "button": "none", "buttons": 0},
        session_id=sid)
    await client.send.Input.dispatchMouseEvent(
        params={"type": "mousePressed", "x": c[0], "y": c[1], "button": "left", "buttons": 1,
                "clickCount": 1}, session_id=sid)
    await asyncio.sleep(1.5)

    loads = await js("JSON.stringify(window.__loads)")
    net = [s for s in sent if "case=instant" in s[1]]
    print(f"   up:request:load fired : {loads}")
    print(f"   network requests      : {len(net)}")
    print("   -> if Unpoly fired 1 but the network shows 2, the duplicate is BELOW Unpoly")

    await session.kill()


asyncio.run(main())
