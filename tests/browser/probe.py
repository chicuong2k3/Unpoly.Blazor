"""Probe: can browser-use drive a headless Chrome and see Unpoly's X-Up-* headers?

Run the sample first:
    dotnet run --no-build --project sample/Jubin --urls http://localhost:5199
"""
import asyncio, sys

BASE = "http://localhost:5199"


async def main():
    from browser_use.browser.session import BrowserSession
    from browser_use.browser.profile import BrowserProfile

    session = BrowserSession(browser_profile=BrowserProfile(headless=True))
    await session.start()
    print("session started")

    cdp = await session.get_or_create_cdp_session()
    print("cdp session:", type(cdp).__name__)
    print("cdp attrs:", [a for a in dir(cdp) if not a.startswith("_")][:20])

    client = cdp.cdp_client
    print("client attrs:", [a for a in dir(client) if not a.startswith("_")][:20])

    seen = []

    # cdp-use style: client.register.Domain.event(callback)
    try:
        def on_request(event, session_id=None):
            req = event.get("request", {})
            seen.append((req.get("method"), req.get("url"), req.get("headers", {})))

        client.register.Network.requestWillBeSent(on_request)
        print("registered Network.requestWillBeSent")
    except Exception as e:
        print("register failed:", type(e).__name__, e)

    await client.send.Network.enable(session_id=cdp.session_id)
    print("Network.enable ok")

    await session.navigate_to(f"{BASE}/lab")
    await asyncio.sleep(2.0)

    print(f"\ncaptured {len(seen)} requests")
    for m, u, h in seen[:12]:
        ups = {k: v for k, v in h.items() if k.lower().startswith("x-up-")}
        print(f"  {m} {u[:64]}  {ups if ups else ''}")

    await session.kill()
    print("\ndone")


if __name__ == "__main__":
    asyncio.run(main())
