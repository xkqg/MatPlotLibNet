# Contributing to MatPlotLibNet

Thank you for your interest in contributing to MatPlotLibNet! This guide covers the practical workflow and the engineering discipline the project expects from every contributor, human or AI-assisted.

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
dotnet build MatPlotLibNet.CI.slnf -c Release
```

This is the solution filter CI builds. A bare `dotnet build` in the repository root takes the full
`MatPlotLibNet.slnx`, which also holds the Maui head and so needs the Maui workload installed: without it the build stops
at NETSDK1147 before it compiles any project. To build everything a box without the workload can build (Wpf, Uno, the samples
and the benchmarks as well), use the filter that leaves only the Maui head out:

```bash
dotnet build MatPlotLibNet.NoMaui.slnf -c Release -t:Rebuild -warnaserror
```

A build ends with 0 warnings and 0 errors.

## Running Tests

The suites are xunit v3 programs: `dotnet test` finds nothing to run and still reports success, so run each suite
with `dotnet run`.

```bash
dotnet run --project Tst/MatPlotLibNet/MatPlotLibNet.Tests.csproj -c Release
```

Each package has a test project of its own under `Tst/`, run the same way. CI runs the core suite, one suite for
each of eleven other packages (Geo, Skia, Blazor, Avalonia, AspNetCore, Interactive, GraphQL, DataFrame, Mcp, Verso,
Maui: its suite compiles the package's sources and needs no workload), the Wiki suite below and
`Tst/MatPlotLibNet.Fidelity` (a comparison with images drawn by matplotlib, scored by a perceptual diff). The Wpf and
Uno suites need Windows, and the MAUI, Wpf and Uno packages need a workload or a Windows SDK: the `windows-heads` job of
`ci.yml` builds them and runs those suites on every push. When you
change how something is drawn, run the Fidelity suite too; a reference picture is made again with
`tools/mpl_reference/generate.py`. A check draws the numbers its reference was drawn from, which the generator writes to
`Tst/MatPlotLibNet.Fidelity/Fixtures/Matplotlib/data` (read with `ReferenceData.Load`), the decorations and the geometry the reference
draws (a colour bar, which takes the room `fig.colorbar` takes, through the library's own verbs), and the generator stops when a package it needs is
missing instead of drawing a substitute. A score that drops is traced to its cause and the cause is fixed; a limit is not widened. A limit is the worst score the row measures on Windows and in the ubuntu container (`MPL_FIDELITY_SCORES=<file>` writes every score beside its limit), plus a margin of 1 in RMS, 2 in the colour distance and 0.01 in SSIM, kept for each style (a row whose classic and v2 pictures measure differently names its `V2Rms`, `V2Ssim` and `V2DeltaE` beside the classic limits); a change that moves a row's score tightens its limit to the new measure. The tables of the colour maps that carry matplotlib's names are written by `tools/mpl_reference/generate_colormaps.py`, which also writes matplotlib's own lookup that the unit test holds them to. To run one test class, call the built executable with an xunit filter:

```bash
Tst/MatPlotLibNet/bin/Release/net10.0/MatPlotLibNet.Tests.exe -filter "/*/*/LegendTests/*"
```

`Tst/MatPlotLibNet.Wiki` compiles every C# block of the wiki, so it needs the wiki cloned beside this repository. It also compiles
every call the documents write outside those blocks: a call or a signature in a sentence or a table of the wiki, of the READMEs and of
`docs/`, every csharp and razor block of a repository document, the wiki's notebook cells and its blocks for another head, and the
code of the XML comments, a name a document leaves undefined standing for any value and a name of a toolkit the suite cannot load
standing for itself, never for the calls of the library beside it (the wiki suite's WrittenCallsTests). What a call is read on
decides: a member a type of the packages does not have is wrong wherever the call stands, even when another library has a member of
that name, and a call is set aside only when what it is read on is no type of the packages; the test prints every
call it sets aside with the reason. The matplotlib call a document puts beside one is held to the calls of matplotlib 3.11.2, read
from `Tst/MatPlotLibNet.Wiki/Python/signatures.txt`, which `tools/mpl_reference/generate_signatures.py` writes; run it again when
a document names a package the table does not hold. A matplotlib call, a keyword (`loc='best'`) or a setting given a value is read
wherever it is written: in a code span, one that wraps a line included, in the words beside a span, in a tree drawn in a fence without
a language, in a fence of Python and in the words of an XML comment. A keyword is written as Python writes it, without blanks around
its `=`; a local name of matplotlib's source that is no keyword is written by its name alone, never as a name given a value.
From the repository's root:

```bash
git clone https://github.com/xkqg/MatPlotLibNet.wiki.git ../MatPlotLibNet.wiki
dotnet run --project Tst/MatPlotLibNet.Wiki/MatPlotLibNet.Wiki.Tests.csproj -c Release
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

These rules apply to every change that lands on this repo. There are no exceptions.

## Versions

- **The maintainer owns version numbers.** Contributors never bump, revert, or "align" a version on their own.
- **Version bumps require a behavioural change.** That means a public API change, different output for the same input, a changed default, or a new feature. Refactors, tests, dead-code removal, docs and CI changes get **NO bump; they stay on the current version**.
- If the signal is ambiguous (for example "no v1.7.3" or "let's not go to..."), **ASK the maintainer**. Never guess which direction is meant.
- When a bump is explicitly requested, update `<Version>` in ALL 14 `.csproj` files atomically (loop over them, not one at a time), AND the two version fields in `Src/MatPlotLibNet.Mcp/.mcp/server.json`. An MCP host resolves the server by that manifest, so if the manifest carries a version that the package does not have, the host finds nothing. `ReleaseVersionContractTests` fails when the two disagree.
- **The CHANGELOG heading carries the number, never a placeholder.** The maintainer names the version when the
  work starts, and that number stays in place through every commit until he judges the accumulated work good
  enough to make packages. So the section is `## [1.16.0]` from the first entry under it, and the 14 csprojs
  plus the MCP manifest carry that same number from that moment. `## [Next]` was the old convention and is no
  longer used: a heading without a number tells a reader nothing, and someone has to remember to rename it
  later. Never invent the number yourself. Ask, and write what you are told.
- **No dates in the CHANGELOG.** The version numbers are the timeline. A date adds nothing a reader can act on.
- **The package icon comes from one file too.** `icon.png` at the repository root, declared once in
  `Directory.Build.props`, packed into all fourteen. It is generated by the console sample generator rather
  than hand-drawn; regenerate it there if it ever changes, and keep it 128x128 and under 1 MB, which is what
  nuget.org enforces at push time and `ReleasePackagingContractTests` enforces here.
- **Release notes come from one file.** `Directory.Build.targets` at the repository root builds the sentence
  every package carries, out of the version the project already declares, and links into the changelog section
  for that version. Nothing per-package to edit, and `ReleasePackagingContractTests` checks the link lands on a heading
  that exists.

## TDD — Red, Green, Refactor. In that order. Always.

- **Write the failing test FIRST.** The build must fail because the new symbol does not exist yet. This is the Red step. If you skip it, the work counts as failed TDD, no matter how many tests you add later.
- Then write the minimum production code to make the test pass (Green).
- Then switch the call sites over and wire up the composition (Refactor).
- **Refactors that move logic between classes inherit that logic's test debt.** "Existing tests
  stay green" is necessary but not sufficient. Coverage is attributed to the class that holds the
  code, so uncovered branches that barely moved the percentage of a large class can drop a small
  receiving class below the ≥90/90 gate with zero behavior change. Re-run the per-class coverage
  check (`tools/coverage/run.ps1` + `check-thresholds.ps1 -Strict`) on every receiving class as
  part of the same PR, and write the missing tests before you declare the refactor done.
- **A test never judges a clock.** The same line took 38 ms on a quiet box and 2,295 ms on a busy one, so a test that asserts on a stopwatch fails when the box is loaded rather than when the code is slower. Assert the fact that makes the speed (the work done, the bytes allocated, with `GC.GetAllocatedBytesForCurrentThread`) and read the time in `Benchmarks/`, which measures process CPU time; `WallClockContractTests` goes red on an assertion that names a stopwatch or a number taken from one. A wait on another thread has a deadline and waits on a fact, not on a sleep chosen by guess.
- **A rule that holds for every property is one case per subject, not one case per property.** A theory over every property of every series type built the same figure once per property: one case per type (or part, or seat) that checks every property and names each one it finds wrong with `Assert.All` is the same check in a tenth of the cases. A sweep over generated values is one case with a loop, and a product of two independent dimensions is tested one dimension at a time. `TheoryRowBudgetContractTests`, which every suite links, refuses a theory of more than 200 rows unless it is named with the reason each row has to be a case of its own. A figure sent through its JSON and read back is `figure.RoundTripped()`.
- **One test class per class, named for it, in a file of its name.** A test that reaches a branch the class's own tests missed goes
  into that class's tests, not into a class named for the round that found it (`*CoverageTests`, `*BranchTests`, `*EdgeCaseTests`,
  `V170*`, `Phase*`): `TestClassContractTests` refuses a second class for one library type and a file that holds a class of another
  name, and `TestCodeRules` refuses a class named for a phase. A test asserts what the code does, so a test whose only check is that
  a figure drew some SVG (`Assert.Contains("<svg", ...)`, `Assert.NotNull(figure.ToSvg())`) is refused too: it names the token the
  case draws, or compares with the picture of a case it must equal. A test double or fixture with no logic of its own has no test
  file. A comment of a test says what the test pins, never the line of a library file it once reached (`line 45`, `L45`, `File.cs:45`): the line
  moves with the next edit and the sentence goes stale, so `TestCodeRules` refuses it; a matplotlib citation naming its `.py` file
  on the same line is the one exception.
- Anti-patterns that indicate a TDD drift:
  - "Extract X, then add tests." Tests as a trailing phrase counts as failed TDD.
  - "Direct unit tests at 100/100" as a plan footnote counts as failed TDD; that belongs in the leading section.
  - "We'll add tests for this" (future tense) counts as failed TDD.

## Engineering discipline — same level as TDD

These rules apply to every non-trivial action:

- **Deep dive.** Before you propose a change, read the full context: the whole method, every caller, the related tests, and the upstream config. No surface-level scans.
- **Full understanding.** If you don't understand *why* the code is the way it is, ask before changing it. "It looked wrong so I rewrote it" is a violation.
- **SOLID.** One responsibility per class and per method. A method that does 15 things violates SRP; fix that before you add a 16th.
- **DRY.** Two copies of the same logic must be extracted into one. "It was faster to copy it" is not a reason.
- **Stacked classes.** Prefer an OO hierarchy plus composition over if/switch dispatch. Extend existing abstractions instead of adding new branches to a god-method.
- **No assumptions.** If you are guessing what the maintainer wants, why code exists, or what a method returns, ask or verify. An assumption that goes uncorrected becomes rework later.
- **No bandaids.** Fix root causes. Silencing the warning, skipping the failing test, or wrapping the exception in a try-catch without understanding why it happens is a bandaid, and a bandaid is technical debt.
- **Be accurate.** Do not write "approximately", "probably", or "should work". Measure, verify, and cite file:line. A precise-sounding claim without proof, such as "100% covered", costs trust.
- **Keep overview.** While you deep-dive, keep the system-level picture in mind. Report back as an overview, not as a log dump.

## Class design

- **No catch-all static classes.** A class whose name ends in `Helper`, `Util` or `Utilities` is refused outright: it has no owner, and nobody finds it from the call site.
- **Shared behaviour on a value is an extension block**, `extension(T value) { public R M(...) }`, in a static class named for what it adds (`LineSegmentExtensions`, `AxisScaleExtensions`), never the old `static R M(this T value, ...)`; `ExtensionShapeContractTests` reads every source file and goes red on the old shape. Converting a published `this` method in place is not a break. A published static that never took `this` stays as it is, because package validation reads its replacement by a block as a removal.
- **Shared behaviour across concrete types** is a generic base class or a generic method with overrides, not a static method.
- **A procedure belongs to the thing it is about.** A static whose first parameter is its subject is an extension member of that subject, or a member of it. A subject demoted to a parameter, or to a bag of loose arguments, is the smell: give the arguments a name (a `readonly record struct`, an options value, the object itself).
- **Statics that are admitted:**
  - an extension block;
  - a factory: a static whose body makes a new, fresh object (`Of`, `For`, `Create`, `FromSeriesDto`), including one that dispatches to polymorphic subtypes;
  - a domain noun: a static class that IS the named thing and holds no state (`Fft`, `Linalg`, `Delaunay`, `Welch`, `StudentT`, `ColorMaps`);
  - one process-wide slot or registry that ARCHITECTURE.md documents in its section "Process-wide state" (below).
- **A global class is banned:** a class that holds state any code in the process can reach and write. The slots and registries are the admitted exception, and each keeps to the same conditions. It holds the one thing it exists to provide (a `Current` slot behind a `volatile` field, or a `NamedTable<TKey, TValue>`) and nothing else; it is separated from every other slot; it is named in the ARCHITECTURE section, which `ProcessWideStateContractTests` holds to the public places a caller can write; and a test that writes one joins a `*GlobalState` collection and puts the old value back. A new one changes the public API for good, so the maintainer decides first.
- **A public interface carries only public members.** What the library's own types add to it (a member only the library may call) goes on an internal interface that derives from it, reached through one internal extension block on the public interface that falls back to its public members (ARCHITECTURE.md, "Internal interface beside a public one"). A non-public member, with a body or without, makes the interface impossible to implement, proxy or mock outside the library; `PublicInterfaceContractTests` goes red on one.
- **A pair or a triple a method hands back is a `readonly record struct`,** never a tuple, so the name shows in IntelliSense, in a stack trace and in the XML docs. A single value is returned bare.
- **A value that chooses behaviour at two or more places is a type,** not a `switch` repeated at each place: one sealed leaf per enum value, found in one place (ARCHITECTURE.md, "A sealed shape per enum value"). A `switch` whose every arm is a value (a colour, an offset, a factory) stays a `switch`.
- Instance collaborator classes with real state, and proper OO subclasses with composition and polymorphism, are the default shape.

## One owner per rule, and a difference is a value

A rule that two places would each work out has one owner that every reader asks (ARCHITECTURE.md, "One owner per rule"). What really
differs between two themes or two kinds of series is a value the owner reads, never a member copied into each class, a flag for each
behaviour or a fallback in each renderer. In practice:

- A theme's numbers live in a look of `ThemeLooks` (`Styling/Looks`): the library's own themes wear the look's `Library` value, the
  matplotlib themes are written once in `MatplotlibThemeFactory`, and a copy of a theme copies `Looks` as one reference.
- A kind of series says what it is to matplotlib once, in its `Artist` override (the call its colour is asked with, its z-order and
  the look that colours it); `SeriesSlot` reads that value, so a renderer never decides a colour or a z-order of its own.
- A series that takes a line width or a dash from its caller holds them as one `GivenStroke` and names its `LineWidthRule`; a bar-like
  patch implements `IEdgedPatch` for its edge and a dot series `IDotSeries`. A value the caller gave wins over the theme's, and the
  JSON writes only the values that were given.
- The major ticks of any axis come from `TickRequest.Majors`, the room of a colour bar from `ColorBarRoom`, where a stroke stands on a
  raster's pixel grid from `AggPixelGrid`, and the extent of a text from the one seam, `TextExtent`.

A change that adds a second place for any of these deletes the first in the same commit.

A rule given one owner gets a row in the area file of `Tst/MatPlotLibNet/Packaging/OwnedRules/` it belongs to: the rule in a
sentence, the pattern that spells it, the files allowed to spell it, and at least one line it must find (`Finds`, a spelling the
change removed) beside the lines it must pass (`Ignores`). `OneOwnerSourceContractTests` reads every row over the tree and over its
own lines, so a pattern that stops finding what it was written for goes red instead of passing on a tree it no longer reads.

## Coding conventions

- Use C# records for immutable data types.
- Follow the existing fluent API patterns.
- No `Base` suffix on abstract classes — use descriptive names.
- Prefer generic base classes with overrides over static methods when behavior is shared.
- Keep the public API intuitive and discoverable.

## Adding a chart verb

A chart that is a drawing of series the library already has (a Pareto chart, a ridgeline, a calendar heatmap) is an extension method on `AxesBuilder`, not a new series type: it adds no `ISeries` class, no `SeriesRegistry` key and no second serializer path, `SeriesCountContractTests` stays as it is, and the chart goes through JSON as its parts. It sits in the family class that matches the data the caller holds, in namespace `MatPlotLibNet` and folder `Builders/`: `CategoricalAxesExtensions` for data keyed by names, `DistributionAxesExtensions` for a chart estimated from a sample, `XYAxesExtensions` for x and y arrays, `GridAxesExtensions` for the cells of a grid, `PriceAxesExtensions` for a series of prices in time order drawn without its time axis, `SignalAxesExtensions` for a signal sampled at a steady rate and handed over in the order of its samples (a spectrum of it). A verb that fits none of them starts a new family class, added together with that verb, and the maintainer decides first, because the public API is permanent. It takes at most 4 parameters besides `this`, with its optional inputs before `configure`; `configure` comes last and defaults to `null`. A verb that draws one series type takes `Action<TSeries>`; one that draws several takes `Action<XxxParts>`, a non-positional readonly record struct with an internal constructor and get-only properties, so only the library makes one. One drawn unit is one series (a ridge, a class of rows, a layer): each takes a colour slot, a legend entry and a JSON entry of its own, and no verb needs a grouping carrier. The verb sets no axis limit unless the chart cannot be read without it. Write the tests first and see them RED: one for each behaviour the verb has of its own, then a shape in `ComposedChartShapes` (`new("name", Build, colours, legendEntries, "Verb")`, built by the verb) with the verb's call and its callback (`WithCallback`), the hand chain of public calls it replaces (`ByHand`), the calls it refuses with the parameter each names (`RefusedCalls`) and, only when the chart cannot be read without one, `SetsLimits`. `ComposedChartContractTests` and the MCP project's spec reader then hold the shape to a write, a read back that draws the same picture with and without a missing value, one colour for each drawn unit, a missing value never written into the picture, the SVG of the hand chain, refusals by name that leave the axes as they were and the caller's limits; `VerbContractTests` holds the verb, as every verb that takes a callback, to what it owes the callback (it runs once on each unit the verb added, after the verb, so what it sets wins; no callback draws what an empty one does; the verb returns its builder; a missing builder is refused by name), and turns red until the verb's call is a row. None of these is written again in the verb's own test file. Give the verb and every type it introduces XML documentation (summary, one `param` for each parameter, `returns`, `exception`, and an `example` that calls it; `ComposedVerbDocumentationTests` requires them), and add it to the lists in the section "Where a verb lives" of `Src/MatPlotLibNet/ARCHITECTURE.md`, which `VerbPlacementContractTests` holds to the public API, along with the shape every verb must have.

## The JSON keeps what is drawn

A figure written with `ToJson` and read back with `FromJson` (the MCP server and the transports use the same reader) must draw the same picture, so every property a series or an axes draws has a field in its DTO, and two reflected contracts hold that. `SeriesDrawnPropertiesRoundTripTests` takes every settable property of every series type listed in `DrawnSeriesFixtures`, sets it to a value that is not its default, writes and reads the figure and compares the property; `AxesDrawnPropertiesRoundTripTests` does the same over `DrawnAxesFixtures` for what an axes draws besides its series (each axis with its ticks, the legend, the colour bar, the grid, the spines, every overlay) and for the figure's own settings and theme. Nothing is listed by hand per property. A property you add joins the contract on its own, and its Theory goes red with the type and the property named until the DTO carries it: add the field to the DTO, write it where the series writes its settings (`ToSeriesDto`), read it where it reads them (`FromSeriesDto`), and leave a value at its default out of the JSON (`OrNullAtRest`, compared with a `Resting` constant that sits beside the property and that the property, the reader and every verb default read, so the starting number is typed once: `OneOwnerSourceContractTests`, `RestingValueContractTests` and `VerbLayerParityContractTests` refuse a second spelling), so JSON from an earlier release reads at the defaults and a figure that never sets it writes the same bytes. What the property starts from joins its subject's file under `Serialization/Golden/Resting` (`RestingValueCorpusTests`, one file per series type, part and settings record), which the commit that adds the property writes again with `MPL_REGEN_GOLDEN=1`; a test of one start, one stored setting or one series' trip through JSON says nothing those files and contracts do not, and `OneOwnerSourceContractTests` refuses its name and the shape of a fact that only gives a series a setting and reads it back. The fields of a positional record are the compiler's, so no test restates them either. A property no JSON can carry (a predicate, a tick locator you wrote yourself) is named in `DrawnSeriesFixtures.NotCarried` with the reason, and a series type that is neither under the contract nor named in `DrawnSeriesFixtures.NotUnderContract` with its reason fails a test, so a new series cannot slip past. The MCP test project links the same fixtures and holds its spec reader to the same contract.

## Dead-code deletion

- Before deleting any file: check the **declaring class visibility**.
- For an `internal` class, "dead" means no callers in `Src/` or `Tst/`. Confirm that with grep, then delete.
- For a `public` class, "dead" is a MUCH higher bar. Grep `Src/ + Tst/ + Samples/ + docs/cookbook/`. A mention in the cookbook is a published contract. NEVER delete a `public` class without explicit maintainer sign-off, even if the repo-internal grep finds nothing.

## Git — main repo (MatPlotLibNet)

- **Default: AI assistants stage with `git add` only. The maintainer runs `git commit`, `git push`, `git tag`.**
- Explicit scoped approval ("you may commit to the feature branch") unlocks commits for THAT scope only. When the scope is done, permission expires and must be re-requested.
- Never add `Co-Authored-By` lines to commits.
- Never force-push to main. Never push to GitHub on your own (the maintainer pushes via VS Code).

## Git — wiki repo

- Wiki commits and pushes are allowed freely — no need to wait for explicit permission.

## CHANGELOG

- **CHANGELOG.md is never committed on a feature branch.** It is committed on `main` only, at merge time, ideally as part of the squash-merge commit so that the release has one curated entry.

## Documentation

- "Update all docs" means README, CHANGELOG, wiki, XML doc comments, samples, docs/cookbook, docs/api and Playground. All of them. Do not skip the hard ones.
- A new cookbook page needs three edits, not one: the page itself, a `href` in `docs/cookbook/toc.yml` and a row
  in `docs/cookbook/index.md`. A page missing from the table of contents is a 404 behind a link the index already
  carries; `ReleaseDocumentContractTests` fails on it.
- The roadmap is one table in two places: the wiki's Roadmap page and `ROADMAP.md`. A change that finishes a row
  moves it from Later to Done in both copies, and its section on the wiki from what is still to come to what is
  done; the wiki suite fails when the two tables differ.
- Every picture the wiki shows is drawn here: by a sample of the sample program or, when no sample draws it, by
  `Samples/MatPlotLibNet.Samples.Console/WikiPictures.cs`, and the wiki keeps a byte-for-byte copy in its `images`
  folder. After a rendering change, re-run the sample program and copy the pictures it changed over the repository's
  `images` folder, then over the wiki's; the wiki suite fails, naming each picture, while a copy is older than the picture drawn
  here, and the wiki suite's sample-pictures test (SamplePicturesTests) fails while a picture in `images` is older than the one the sample program draws. That test
  runs the program and is held on Windows only, where the repository's pictures are drawn: measured from the committed tree in
  the ubuntu container, none of the 313 files the program writes (200 PNGs, 112 SVGs, one PDF) is byte-identical to its
  Windows twin. The PNGs differ in 0.5 to 1.5 % of their pixels, in small clusters where text stands (the two builds of Skia set a
  glyph differently); the SVGs differ in the outline of every glyph, which the Linux build writes with another verb for a curve and with a
  line back to the start of a contour before its close; the PDF differs as well. The Fidelity suite is what judges a drawing on every platform, by score.
- A number a document states about the library is a claim, and a claim that can be checked gets a test. The
  colormap count is held against the registry in every current-state document; the series and indicator counts
  are held against the assembly, the verbs and the number of them against the public API (`VerbPlacementContractTests`), and the map projections against the Geo package. Add the test with the number. The class and test counts are the exception: a
  test that counts tests fails on the next test anyone writes, so those two are measured at the gate and copied
  into the documents before a release.

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
