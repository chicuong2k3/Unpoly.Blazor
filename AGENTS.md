# AGENTS.md

Rules for any agent or human working in this repo. Read `TASKS.md` for what to do next.

## What this project is

A server-side adapter for [Unpoly 3](https://unpoly.com) on **Blazor static SSR**,
built deliberately as a *learning vehicle*. The code is a by-product; understanding
Unpoly is the deliverable.

This changes the usual trade-offs:

- **Do not implement ahead of the phase.** `TASKS.md` defines an order. Later-phase
  methods are intentionally `throw new NotImplementedException("Phase X · 📖 <guide url>")`.
  Filling them in early destroys the point of the exercise.
- **Do not gold-plate.** No extra overloads, fluent builders, or options objects for a
  concept that is already understood. That is fake productivity — it buys no understanding.
- **The phase order is load-bearing.** Phase B (`/caching`) teaches that one click causes
  two server requests. Doing D or F before B bakes in wrong assumptions.

## Hard rules

1. **All comments and XML docs in English.** UI copy in `sample/Jubin` stays Vietnamese —
   it is content, not comments.
2. **Update `CONCEPTS.md`** when a guide's concepts are covered, so a page can be marked
   finished with confidence. It distinguishes *not done* from *nothing to do*.
3. **Every public method cites its spec**: `📖 https://unpoly.com/<guide>` in the XML doc.
   If you cannot name the guide, you do not understand the header well enough to ship it.
4. **No new dependencies.** The library has exactly one `FrameworkReference`. Keep it that way.
5. **No database in the sample.** `sample/Jubin/Data/Catalog.cs` is in-memory on purpose.
6. **Every non-trivial behaviour gets a check** in `tests/Unpoly.Blazor.Tests/Program.cs`.
   Plain asserts, no test framework. It must print `OK — N checks passed`.
7. **Update `TASKS.md` in the same change** that completes a checkbox.
8. **Update `.claude/skills/unpoly-blazor/SKILL.md` whenever the public API changes.**
   That file tells downstream agents which methods exist and which throw. A stale copy is
   worse than none: it makes an agent recommend a method that throws. Treat it as part of
   the API, not as documentation.
9. **A guide is only "read" when every section in its table of contents has a row in
   `CONCEPTS.md`** — including sections with nothing to do. Summarising a guide instead of
   enumerating it is how `/failed-responses` §3 "Customizing failure detection" went missing
   for three phases. Fetch the page's headings; do not work from memory of it.

   A row must also carry **what the section teaches**, not just its title. `/failed-responses`
   §1 was present as one cell naming `UpFailTarget()`, which hid the `fail` prefix convention
   and the reason the protocol has exactly three `X-Up-Fail-*` headers. If a section explains
   a rule, the rule goes in the file — under the table if it does not fit a cell.
10. **Every `CONCEPTS.md` row names where the sample exercises it** (`file:line · token`), or
   a dash if it does not. The dash is the useful half: it is how the "Implemented but never
   exercised" list stays honest, and that list is where the next sample feature comes from.
   Line numbers drift — the token after the dot is what to grep.
11. **Marking something ➖ in `CONCEPTS.md` means adding it to the skill's "Reach for HTML
   first" table.** ➖ means *we* have nothing to write, not that a caller has nothing to
   know — it is precisely where an agent would otherwise invent a C# helper for something
   that is one attribute. The ➖ list is the most useful thing the skill carries.

## Layout

```
src/Unpoly.Blazor      RCL. UpRequest.cs (read X-Up-*), UpResponse.cs (write X-Up-*),
                       UnpolySetup.cs (DI + antiforgery), UnpolyHead.razor (assets +
                       config script), UpChrome.razor (skip chrome on fragment requests).
                       wwwroot/unpoly.min.{js,css} → served as _content/Unpoly.Blazor/...
sample/Jubin           Blazor Web App, interactivity=None. The lab.
tests/                 Console app. `dotnet run` → OK or throw.
```

## Locked architectural decisions

Do not "fix" these without reading the reasoning:

| Decision | Why |
|---|---|
| `blazor.web.js` is **removed** from `App.razor` | With `interactivity=None` it only adds enhanced navigation and enhanced forms — exactly Unpoly's job. Both present means they fight over clicks and submits. |
| Hand-written CSS, no framework | DAUB was dropped: its JS initialises components per element, so an Unpoly swap leaves them dead DOM, silently. Tailwind was declined too — a build step and a watch process would be a second toolchain in a repo whose whole workflow is `dotnet run`. Five pages do not need one. `sample/Jubin/wwwroot/app.css` is the whole theme. |
| Unpoly owns anything that loads a URL | Login modal, cart drawer, quick-view are `[up-layer]`. Any client-only widget added later (carousel, tooltip) must never own the same element. |
| Full-page responses are the default | Unpoly extracts the target client-side. `UpChrome` is the opt-in optimisation, not a requirement. Do not build fragment-only endpoints. |
| SDK pinned in `global.json` | 10.0.3xx stable. The machine default is a preview. |

## Commands

```bash
dotnet build
dotnet run --project tests/Unpoly.Blazor.Tests
dotnet run --project sample/Jubin
```

Build failing on a locked DLL means a previous `dotnet run` is still alive:
`taskkill //F //IM dotnet.exe`.

## Before claiming a phase is done

Work through that phase's section of [`VERIFY.md`](VERIFY.md). Every box is a command to
run or a thing to watch happen. Do not report completion from reading the diff.

If an item cannot be verified — no browser available, no way to reproduce — leave it
unticked and write why. Never tick something you did not observe.
