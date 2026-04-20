# Contributing to MatPlotLibNet

Thank you for your interest in contributing to MatPlotLibNet! This guide covers the practical workflow **and** the engineering discipline the project expects from every contributor (human or AI-assisted).

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/MatPlotLibNet.git`
3. Create a branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Push and open a pull request

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An IDE such as Visual Studio 2022, VS Code, or JetBrains Rider

## Building

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

## Project Structure

```
Src/          Source projects (one folder per package)
Tst/          Test projects (mirrors Src/ layout)
Samples/      Runnable sample projects
Benchmarks/   BenchmarkDotNet performance suite
```

---

# Project rules — read before any PR or commit

These rules govern how changes land on this repo. They apply without exception.

## Versions

- **The maintainer owns version numbers.** Contributors never bump, revert, or "align" versions autonomously.
- **Version bumps require a behavioural change** — public API change, different output for the same input, changed defaults, new features. Refactors, tests, dead-code removal, docs, CI changes → **NO bump, stay on current version**.
- Ambiguous signals ("no v1.7.3", "let's not go to...") → **ASK the maintainer**. Never guess which direction.
- When a bump is explicitly requested, update `<Version>` in ALL 13 `.csproj` files atomically (loop, not one at a time).

## TDD — Red, Green, Refactor. In that order. Always.

- **Write the failing test FIRST.** Build must fail because the new symbol does not exist yet. This is the Red step; skipping it = FAILED TDD regardless of how many tests you later add.
- Then write the minimum production code to make the test pass (Green).
- Then flip call sites / wire the composition (Refactor).
- Anti-patterns that indicate a TDD drift:
  - "Extract X, then add tests." ← tests as trailing phrase = FAILED TDD.
  - "Direct unit tests at 100/100" as a plan footnote = FAILED TDD; that belongs as the leading section.
  - "We'll add tests for this" (future tense) = FAILED TDD.

## Engineering discipline — same level as TDD

Applies to every non-trivial action:

- **Deep dive.** Read the full context — the whole method, every caller, the related tests, the upstream config — before proposing a change. No surface-level scans.
- **Full understanding.** If you don't understand *why* the code is the way it is, ask before changing it. "It looked wrong so I rewrote it" is a violation.
- **SOLID.** Single responsibility per class/method. One method doing 15 things = SRP violation, correct before adding line 16.
- **DRY.** Two copies of the same logic = extract. No "it was faster to copy it".
- **Stacked classes.** Prefer OO hierarchy + composition over if/switch dispatch. Extend existing abstractions instead of grafting new branches onto a god-method.
- **No assumptions.** If you're guessing what the maintainer wants, why code exists, or what a method returns — ask or verify. Assumption that goes uncorrected = later rework.
- **No bandaids.** Fix root causes. "Silence the warning" / "skip the failing test" / "try-catch the exception" without understanding why = bandaid = technical debt.
- **Be accurate.** No "approximately", "probably", "should work". Measure, verify, cite file:line. False precision ("100% covered") without proof is a trust hit.
- **Keep overview.** While deep-diving, hold the system-level picture. Don't lose the forest for the tree. Report back in overview form, not a log dump.

## Class design

- **No static helper classes.** Banned shape: `static class FooHelper { public static ... }`.
- **Allowed shapes:** extensions (`public static T M(this Type self, ...)`), instance collaborator classes with real state, proper OO subclasses + composition + polymorphism.
- Factory methods are the single legitimate static-method exception — one static `Create(...)` that dispatches to polymorphic subtypes.

## Coding conventions

- Use C# records for immutable data types.
- Follow the existing fluent API patterns.
- No `Base` suffix on abstract classes — use descriptive names.
- Prefer generic base classes with overrides over static methods when behavior is shared.
- Keep the public API intuitive and discoverable.

## Dead-code deletion

- Before deleting any file: check the **declaring class visibility**.
- `internal` class → "dead" means no callers in `Src/` or `Tst/`. Grep confirms, then delete.
- `public` class → "dead" is a MUCH higher bar. Grep `Src/ + Tst/ + Samples/ + docs/cookbook/`. Cookbook mention = published contract. NEVER delete a `public` class without explicit maintainer sign-off, even if repo-internal grep is empty.

## Git — main repo (MatPlotLibNet)

- **Default: AI assistants stage with `git add` only. The maintainer runs `git commit`, `git push`, `git tag`.**
- Explicit scoped approval ("you may commit to the feature branch") unlocks commits for THAT scope only. When the scope is done, permission expires and must be re-requested.
- Never add `Co-Authored-By` lines to commits.
- Never force-push to main. Never push to GitHub autonomously (the maintainer pushes via VS Code).

## Git — wiki repo

- Wiki commits and pushes are allowed freely — no need to wait for explicit permission.

## CHANGELOG

- **CHANGELOG.md is never committed on a feature branch.** It lands on `main` only, at merge time — ideally as part of the squash-merge commit so the release has one curated entry.

## Documentation

- "Update all docs" = README + CHANGELOG + wiki + XML doc comments + samples + docs/cookbook + docs/api + Playground. All of them. No skipping the hard ones.

---

## Pull Request Guidelines

- Keep PRs focused on a single change.
- Include tests for new functionality (per the TDD section above — tests come first, not after).
- Ensure all existing tests pass.
- Update documentation if your change affects the public API (per the Documentation section above — update ALL docs, not just README).
- Follow the existing code style and the engineering discipline above.

## Reporting Issues

- Use GitHub Issues for bug reports and feature requests.
- Include steps to reproduce for bugs.
- Include the .NET version and OS you are using.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
