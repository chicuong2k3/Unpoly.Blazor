"""Why does the accept value not reach the opener?"""
import asyncio, json

BASE = "http://localhost:5199"


async def main():
    from browser_use.browser.session import BrowserSession
    from browser_use.browser.profile import BrowserProfile

    session = BrowserSession(browser_profile=BrowserProfile(headless=True))
    await session.start()
    cdp = await session.get_or_create_cdp_session()
    client, sid = cdp.cdp_client, cdp.session_id

    seen = {}
    extra = []

    def on_resp(e, session_id=None):
        r = e.get("response", {})
        if "/size" in r.get("url", ""):
            seen[r.get("url")] = (r.get("status"), r.get("headers", {}))

    client.register.Network.responseReceived(on_resp)
    await client.send.Network.enable(session_id=sid)
    await client.send.Runtime.enable(session_id=sid)

    async def js(e):
        r = await client.send.Runtime.evaluate(
            params={"expression": e, "returnByValue": True, "awaitPromise": True},
            session_id=sid)
        if "exceptionDetails" in r:
            return "JS ERROR: " + json.dumps(r["exceptionDetails"].get("text", ""))
        return r.get("result", {}).get("value")

    await session.navigate_to(BASE + "/p/dam-4")
    await asyncio.sleep(2.0)
    cdp = await session.get_or_create_cdp_session()
    client, sid = cdp.cdp_client, cdp.session_id
    await client.send.Runtime.enable(session_id=sid)

    print("1. the attribute as it reached the browser")
    print("  ", await js("document.querySelector('a[up-layer]')?.getAttribute('up-on-accepted')"))

    print("\n2. listen for the layer events")
    await js("""
      window.__ev = [];
      up.on('up:layer:accepted',  (e) => window.__ev.push(['accepted',  JSON.stringify(e.value)]));
      up.on('up:layer:dismissed', (e) => window.__ev.push(['dismissed', JSON.stringify(e.value)]));
      up.on('up:layer:accept',    (e) => window.__ev.push(['accept',    JSON.stringify(e.value)]));
    """)

    await js("document.querySelector('a[up-layer]').click()")
    await asyncio.sleep(2.0)
    print("   overlay open:", await js("!!document.querySelector('up-modal')"))

    await js("document.querySelector('up-modal form.sizes button[value=M]').click()")
    await asyncio.sleep(2.5)

    print("\n3. what the POST answered")
    for url, (status, hdrs) in seen.items():
        ups = {k: v for k, v in hdrs.items() if k.lower().startswith("x-up-")}
        print(f"   {status} {url}")
        for k, v in ups.items():
            print(f"       {k}: {v}")

    print("\n4. layer events the page saw")
    print("  ", await js("JSON.stringify(window.__ev)"))

    print("\n5. final state")
    print("   overlay still open:", await js("!!document.querySelector('up-modal')"))
    print("   chosen-size       :", await js("document.querySelector('.chosen-size')?.textContent"))
    print("   add-to-cart off   :", await js("document.querySelector('.add-to-cart')?.disabled"))

    await session.kill()


asyncio.run(main())
