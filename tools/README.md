# Tools

Scripts and procedures that sit beside the build. The rules a contributor keeps are in [CONTRIBUTING.md](../CONTRIBUTING.md).

## Before a push

`tools/prepush.ps1` runs everything CI judges, including the docfx site (`-SkipCoverage` for a documentation-only change). It calls
`git`, so the PowerShell that runs it needs `git` on its PATH. Its builds run through the newest installed 10.0 SDK, the one the
workflows install, because a newer SDK beside it compiles nullability differently and answers 0 warnings where the runner finds some.

`tools/coverage/run.ps1 -Check -Strict` runs every suite and the per-class gate (at least 90 % of lines and of branches in every class,
and no class that fell). Run it from PowerShell, never as `bash tools/coverage/run.sh` on Windows: under Git Bash the profiler of
`dotnet-coverage` does not attach and nothing is measured. `tools/coverage/thresholds.json` carries no exemptions; a class that cannot be
tested gets `[ExcludeFromCodeCoverage]` at its source, which takes it out of the report.

## The ubuntu half, run locally in docker

The runners are ubuntu, so a change to a target framework, a project property or `docfx.json` is judged in a Linux container before it is
pushed. Start it from PowerShell (Git Bash rewrites the mount paths):

```
docker run --rm -v <repo>:/mnt/repo:ro -v <wiki>:/mnt/wiki:ro mcr.microsoft.com/dotnet/sdk:10.0-noble bash -c "<script>"
```

The script installs `libfontconfig1 libfreetype6 git ca-certificates` (and `python3` for the Pages job) and clones both mounts beside each
other, so the committed state is built with LF endings, as on the runner and not as the CRLF working copy. It points `origin` at the GitHub
URL (without it SourceLink warns and the build is not at 0 warnings), then runs the steps of `ci.yml`: restore, build
`MatPlotLibNet.CI.slnf`, and the suites with `dotnet run --no-build`. For `pages.yml` it installs docfx with `dotnet tool install -g docfx`
and runs `docfx metadata docs/docfx.json` and `docfx build docs/docfx.json` with `DOTNET_ROLL_FORWARD=Major` (the image holds the 10 runtime
only). The `noble` and the `resolute` image both build at 0 warnings.

**The runner's `coverage` job is part of the ubuntu half, and the steps above do not run it.** A line reached only by a path that depends on
timing is measured on a Windows run and missed on Linux (a subscription client's handler went from 100 % to 99 % on the runner while the
Windows run was green). In the container, install `dotnet-coverage` and `dotnet-reportgenerator-globaltool`, run `bash tools/coverage/run.sh`
(no `--check`: the image has no `pwsh`), copy `out/coverage/coverage.cobertura.xml` out through a mounted directory, and run
`tools/coverage/check-thresholds.ps1 -Cobertura <file> -Strict` on the host. A class whose figure differs from the Windows merge needs a test
that waits on the fact.

## A release

A release does not publish itself. The maintainer pushes the tag. `release.yml` then makes the GitHub release with the built-in token, and
GitHub starts no workflow from an event a bot token raised, so `publish.yml` (`release: published`) does not fire: dispatch it by hand with
its `tag` input. Its three jobs (`publish-core`, `publish-platform`, `publish-uno`) first refuse a tag that is not the version the packages
carry, then push through NuGet trusted publishing (`NuGet/login`, no stored key). `publish-mcp-registry.yml` is manual on purpose: run it once
nuget.org has indexed the new version, or the registry cannot resolve the package.

## The matplotlib references of the Fidelity suite

`tools/mpl_reference/generate.py --chart <name>` makes one reference picture again and writes only the charts named (`--out <dir>` writes the
same layout elsewhere, to compare a run with what is checked in). The suite scores the PNG that Skia draws straight from the figure (`ToPng`,
never through the SVG writer) against matplotlib's own pictures, so a change to the SVG writer alone cannot move a score. A score means
something only when the check and the reference draw the same thing: a chart whose numbers come from numpy's random generator writes them to
`Fixtures/Matplotlib/data/<chart>.json` and its check reads them with `ReferenceData.Load(chart)` (never `System.Random`, never numbers pasted
into the check: `ReferenceContractTests` refuses both); the decorations the reference draws (a colour bar, a quiver key, table labels) are drawn
by the check too, through the library's own verbs; and a package a reference needs that is not installed (squarify, seaborn, pandas_ta) stops
the generator, which never draws a substitute. A limit is never widened to the measured value of a row whose score dropped: the cause is found
and fixed, and the limit stays where it was or is made tighter.

## The other generated tables

Three more scripts in `tools/mpl_reference/` write a table the library or a suite reads, each deterministic and run by hand, never by a
suite: `generate_colormaps.py` writes the colour maps the library ships under matplotlib's names, `generate_mathtext.py` the names
matplotlib's mathtext knows (symbols, functions, accents, spaces), and `generate_signatures.py` the calls, keywords and settings of
matplotlib and of the packages the documents compare with (`Tst/MatPlotLibNet.Wiki/Python/signatures.txt`). `tools/unicode/` holds the
generator of the bidirectional character classes, and `tools/seo/` the scripts that write the docs site's page metadata and tell search
engines about a new page (IndexNow).
