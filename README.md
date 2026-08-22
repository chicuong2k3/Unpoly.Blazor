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
tests/                 plain console checks, no framework
```

## Run

```bash
dotnet build
dotnet run --project tests/Unpoly.Blazor.Tests   # OK — N checks passed
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

## Status

**Phase A of 7 · 6 of 26 protocol headers.** Unimplemented methods throw
`NotImplementedException` carrying their phase and the guide URL that explains them.

- [`TASKS.md`](TASKS.md) — the phase-by-phase plan and what to do next
- [`AGENTS.md`](AGENTS.md) — working rules, locked decisions

## Why the project is shaped this way

It is a learning vehicle: each phase pairs a set of Unpoly guides with the methods and
sample feature that exercise them. Implementing ahead of the phase order defeats it —
see `AGENTS.md`.
