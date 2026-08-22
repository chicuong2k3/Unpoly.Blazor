"""Capture the sample's pages so the visual result can be judged by a human.

The one VERIFY.md item no assertion can close: whether it looks right.
"""
import asyncio, base64, os, sys

BASE = "http://localhost:5199"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "shots")

PAGES = [
    ("home", "/"),
    ("shop", "/shop"),
    ("product", "/p/dam-4"),
    ("login", "/login"),
    ("lab", "/lab"),
]


async def main():
    from browser_use.browser.session import BrowserSession
    from browser_use.browser.profile import BrowserProfile

    os.makedirs(OUT, exist_ok=True)
    session = BrowserSession(browser_profile=BrowserProfile(
        headless=True, viewport={"width": 1280, "height": 900}))
    await session.start()

    cdp = await session.get_or_create_cdp_session()
    for name, path in PAGES:
        await session.navigate_to(BASE + path)
        await asyncio.sleep(2.0)
        cdp = await session.get_or_create_cdp_session()
        shot = await cdp.cdp_client.send.Page.captureScreenshot(
            params={"format": "png", "captureBeyondViewport": True},
            session_id=cdp.session_id)
        dest = os.path.join(OUT, f"{name}.png")
        with open(dest, "wb") as f:
            f.write(base64.b64decode(shot["data"]))
        print(f"{dest}  ({os.path.getsize(dest) // 1024} KB)")

    await session.kill()


asyncio.run(main())
