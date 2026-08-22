# Unpoly.Blazor

Server-side adapter for [Unpoly 3](https://unpoly.com) on **Blazor static SSR**.

Unpoly requests full pages and extracts the target fragment on the client, which matches
how Blazor static SSR already renders. So this library is *not* a fragment-rendering
engine — it is a typed wrapper over the [server protocol](https://unpoly.com/up.protocol)
plus one component that skips rendering page chrome when only a fragment was asked for.

No Blazor internals are touched. No extra dependencies.

```
src/Unpoly.Blazor      the library (Razor Class Library)
sample/Jubin           e-commerce sample, static SSR — the lab
tests/                 9 header tests + 41 browser tests (Playwright), one dotnet test
```

## Run

```bash
dotnet build
dotnet test                                      # 50 tests: 9 header + 41 browser
dotnet run --project sample/Jubin
```

## Use

```csharp
// Program.cs
builder.Services.AddUnpoly(o => o.MainTargets = [".content"]);
```

```razor
@* App.razor — inside <head>, and drop blazor.web.js *@
<UnpolyHead />
```

```razor
@* MainLayout.razor — chrome is skipped on fragment requests *@
<UpChrome><NavMenu /></UpChrome>
<main class="content">@Body</main>
```

```razor
@* any page *@
@code {
    [CascadingParameter] public HttpContext Ctx { get; set; } = default!;
}
```

## For AI agents using this library

[`.claude/skills/unpoly-blazor/SKILL.md`](.claude/skills/unpoly-blazor/SKILL.md) is a
consumer-facing reference: setup, the API that currently exists, and the mistakes worth
checking first. Copy it into the consuming project:

```bash
mkdir -p .claude/skills/unpoly-blazor
curl -o .claude/skills/unpoly-blazor/SKILL.md   https://raw.githubusercontent.com/chicuong2k3/Unpoly.Blazor/main/.claude/skills/unpoly-blazor/SKILL.md
```

Claude Code picks it up automatically. Other agents read it as plain markdown — point their
`AGENTS.md` at the file. It is deliberately **not** an MCP server: this is static reference
text, and a server process would add installation and a runtime for nothing.

Note the file states which methods exist and which throw. That is load-bearing — without it
an agent will confidently suggest `UpAcceptLayer` and hand you a `NotImplementedException`.

## Status

**Phases A–D of 7 · 20 of 24 protocol headers.** Unimplemented methods throw
`NotImplementedException` carrying their phase and the guide URL that explains them.

- [`TASKS.md`](TASKS.md) — the phase-by-phase plan and what to do next
- [`VERIFY.md`](VERIFY.md) — the acceptance tests each phase must pass to count as done
- [`CONCEPTS.md`](CONCEPTS.md) — per-guide coverage: what is covered, what has nothing to cover
- [`AGENTS.md`](AGENTS.md) — working rules, locked decisions

## Why the project is shaped this way

It is a learning vehicle: each phase pairs a set of Unpoly guides with the methods and
sample feature that exercise them. Implementing ahead of the phase order defeats it —
see `AGENTS.md`.
