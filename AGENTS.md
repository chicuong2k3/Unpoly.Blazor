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
2. **Every public method cites its spec**: `📖 https://unpoly.com/<guide>` in the XML doc.
   If you cannot name the guide, you do not understand the header well enough to ship it.
3. **No new dependencies.** The library has exactly one `FrameworkReference`. Keep it that way.
4. **No database in the sample.** `sample/Jubin/Data/Catalog.cs` is in-memory on purpose.
5. **Every non-trivial behaviour gets a check** in `tests/Unpoly.Blazor.Tests/Program.cs`.
   Plain asserts, no test framework. It must print `OK — N checks passed`.
6. **Update `TASKS.md` in the same change** that completes a checkbox.

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
| DAUB **classless** build until Phase G | `daub.js` initialises components per element. Unpoly swaps a fragment and those components are dead DOM — silently, no error. Phase G handles this with `up.compiler`. |
| Unpoly owns anything that loads a URL | Login modal, cart drawer, quick-view are `[up-layer]`, styled with DAUB CSS. `daub.js` keeps only pure-client widgets (accordion, carousel, tooltip). Never let both own the same modal. |
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

Run the checks. Run the sample. Confirm the feature in a browser or with `curl`.
Do not report completion from reading the diff.
