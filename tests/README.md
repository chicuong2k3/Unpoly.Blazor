# Tests

Everything runs under one command:

```bash
dotnet test
```

| Project | What it covers |
|---|---|
| `Unpoly.Blazor.Tests` | header logic: 9 xunit tests, 97 assertions, no I/O |
| `Unpoly.Blazor.BrowserTests` | 41 tests driving a real Chromium against the sample |

## Browser tests

Playwright for .NET. `UnpolyFixture` starts the sample app as a child process on port 5288
and launches Chromium once for the whole run; every test gets its own page and its own
record of the traffic.

First run on a new machine needs the browser:

```bash
dotnet build
pwsh tests/Unpoly.Blazor.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Watch it happen with `HEADED=1 dotnet test`.

### Why real input events

`[up-instant]` fires on **mousedown**, so `element.click()` cannot tell it apart from an
ordinary follow — `Probe.PressOnly` presses and never releases. `[up-preload]` needs a hover
held past `up.link.config.preloadDelay` (90ms). Both scroll the target into view first: a
bounding box for an element below the fold gives coordinates outside the viewport, and the
press lands on nothing.

### Things that bit, and are now guarded

- **Redirected `stdout`.** The fixture drains both pipes. Redirecting without reading fills
  the OS buffer and blocks the child; the sample logs a line per request, so it froze partway
  and 23 tests timed out on navigation.
- **Statements vs expressions.** `Probe.Js` evaluates one expression; `Probe.Exec` runs a
  block. Passing two statements to `Js` is a syntax error that surfaces as "the listener
  never fired", not as a script error.
- **States that only exist in flight.** `Probe.Poll` samples over time. A settled page never
  shows `up-progress-bar` or a disabled form, so a single assertion afterwards would pass for
  the wrong reason.

### Two findings recorded rather than asserted away

**Caching.** "One click, two requests" is wrong as a blanket claim. Within `cacheExpireAge`
(15s) a cached click makes **no** request; only an **expired** entry is rendered stale and
then refetched once.

**Scroll restoration.** Not automatic. Every sample over four seconds was 0, and the guide
makes it opt-in via `[up-scroll=restore]`. The test asserts the observation instead of a
behaviour Unpoly never promised.
