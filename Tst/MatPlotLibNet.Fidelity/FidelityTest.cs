// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Fidelity;

/// <summary>
/// Base class for fidelity tests.  Provides helpers to render a MatPlotLibNet figure to PNG,
/// load the corresponding matplotlib reference fixture, run <see cref="PerceptualDiff.Compare"/>,
/// and emit a side-by-side diff image on failure.
/// </summary>
public abstract class FidelityTest
{
    // Root directory that contains the matplotlib reference PNGs copied to the test output.
    // Each style has its own subdir: Fixtures/Matplotlib/{classic|v2}/.
    private static readonly string FixtureRoot =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Matplotlib");

    // Where diff images are written on failure (next to the test binary).
    private static readonly string FailureDir =
        Path.Combine(AppContext.BaseDirectory, "fidelity-failures");

    // Where rendered PNGs are dumped when MPL_FIDELITY_DUMP=1. Each render is written to
    // fidelity-renders/{classic|v2}/{name}.png alongside the matplotlib reference so a
    // human can eyeball all 146 side-by-side without having to fail the suite.
    private static readonly string? DumpFolderFromEnvironment =
        string.Equals(Environment.GetEnvironmentVariable("MPL_FIDELITY_DUMP"), "1", StringComparison.Ordinal)
            ? Path.Combine(AppContext.BaseDirectory, "fidelity-renders")
            : null;

    /// <summary>The folder the rendered picture of this row is written to, as <c>{style}/{fixture}.png</c>; <see langword="null"/>
    /// writes none.</summary>
    internal virtual string? DumpFolder => DumpFolderFromEnvironment;

    /// <summary>Resolves the matplotlib fixture subdirectory name for a theme. Defaults to "classic".</summary>
    protected static string FixtureSubdir(Theme theme) =>
        theme.Name == "matplotlib-v2" ? "v2" : "classic";

    /// <summary>The points of a grid, row by row: every <paramref name="xs"/> value for each of the <paramref name="ys"/>, as numpy's
    /// <c>meshgrid</c> then <c>ravel</c> lists them.</summary>
    /// <param name="xs">The values along x.</param>
    /// <param name="ys">The values along y.</param>
    protected static MeshPoints Meshgrid(double[] xs, double[] ys) =>
        new([.. ys.SelectMany(_ => xs)], [.. ys.SelectMany(y => xs.Select(_ => y))]);

    /// <summary>A figure of the size every fidelity render uses, under the theme the row names.</summary>
    /// <param name="themeId">The row's theme: <c>classic</c> or <c>v2</c>.</param>
    protected static FigureBuilder FidelityFigure(string themeId) => FidelityFigure(themeId.AsTheme());

    /// <summary>A figure of the size every fidelity render uses, under <paramref name="theme"/>.</summary>
    /// <param name="theme">The theme.</param>
    protected static FigureBuilder FidelityFigure(Theme theme) => Plt.Create().WithSize(FigWidth, FigHeight).WithTheme(theme);

    /// <summary>Figure width used for all fidelity renders — matches the Python generator (800 px).</summary>
    protected const double FigWidth  = 800;

    /// <summary>Figure height used for all fidelity renders — matches the Python generator (600 px).</summary>
    protected const double FigHeight = 600;

    /// <summary>DPI used for the Skia rasterization pass — matches the Python generator (100 dpi).</summary>
    protected const int RenderDpi = 100;

    // ──────────────────────────────────────────────────────────────────────────
    // Assert helper
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders <paramref name="figure"/> to PNG, compares it to the named matplotlib fixture,
    /// and fails the test with a descriptive message + diff image if any metric exceeds its threshold.
    /// </summary>
    /// <param name="figure">The figure to render.</param>
    /// <param name="fixtureName">Name of the reference PNG (without extension) in the Fixtures folder.</param>
    /// <param name="callerMember">Automatically resolved caller method name (used for the diff filename).</param>
    protected void AssertFidelity(Figure figure, string fixtureName,
        [CallerMemberName] string? callerMember = null)
    {
        string subdir = FixtureSubdir(figure.Theme);
        var (rmsThreshold, ssimThreshold, deltaEThreshold) = GetThresholds(callerMember, subdir);

        byte[] actual    = RenderToPng(figure);
        byte[] reference = LoadFixture(subdir, fixtureName);

        // MPL_FIDELITY_DUMP=1 writes the rendered PNG to fidelity-renders/{subdir}/{name}.png
        // without affecting the pass/fail decision. Used to get a full gallery of our output
        // alongside the matplotlib references for visual inspection.
        if (RecordsItsRows && DumpFolder is { } dumpFolder)
        {
            string dumpSub = Path.Combine(dumpFolder, subdir);
            Directory.CreateDirectory(dumpSub);
            File.WriteAllBytes(Path.Combine(dumpSub, $"{fixtureName}.png"), actual);
        }

        var result = PerceptualDiff.Compare(reference, actual);
        RecordScore(subdir, fixtureName, result, new FidelityThresholds(rmsThreshold, ssimThreshold, deltaEThreshold));
        if (!result.Passed(rmsThreshold, ssimThreshold, deltaEThreshold))
        {
            string diffPath = WriteDiff($"{subdir}_{fixtureName}", reference, actual);
            string colorDiag = PerceptualDiff.DiagnoseColors(reference, actual);
            Assert.Fail(
                $"Fidelity check failed for '{subdir}/{fixtureName}'.\n" +
                $"  {result}\n" +
                $"  Thresholds: RMS≤{rmsThreshold}  SSIM≥{ssimThreshold}  ΔE≤{deltaEThreshold}\n" +
                $"  Colors: {colorDiag}\n" +
                $"  Diff image: {diffPath}");
        }
    }

    /// <summary>Draws one subplot of the size every fidelity render uses, titled <paramref name="title"/> and filled by
    /// <paramref name="chart"/>, under the row's theme, and compares it to the named matplotlib fixture: the whole of a row whose
    /// picture is one titled chart.</summary>
    /// <param name="themeId">The row's theme: <c>classic</c> or <c>v2</c>.</param>
    /// <param name="fixtureName">Name of the reference PNG (without extension) in the Fixtures folder.</param>
    /// <param name="title">The title of the axes.</param>
    /// <param name="chart">Draws the chart on the axes.</param>
    /// <param name="callerMember">Automatically resolved caller method name (used for the limits and the diff filename).</param>
    protected void AssertFidelity(string themeId, string fixtureName, string title, Action<AxesBuilder> chart,
        [CallerMemberName] string? callerMember = null) =>
        AssertFidelity(FidelityFigure(themeId).AddSubPlot(1, 1, 1, ax => chart(ax.WithTitle(title))).Build(), fixtureName, callerMember);

    // MPL_FIDELITY_SCORES=<file> appends one "style/name<TAB>rms<TAB>ssim<TAB>deltaE<TAB>limit rms<TAB>limit ssim<TAB>limit deltaE"
    // line per compared fixture, pass or fail, so a change that moves pictures can be scored before and after, and a row can be
    // read against the limit it is held to.
    private static readonly string? ScoresPathFromEnvironment = Environment.GetEnvironmentVariable("MPL_FIDELITY_SCORES");
    private static readonly object ScoresLock = new();

    /// <summary>The file the scores of this row are appended to; <see langword="null"/> or empty writes none.</summary>
    internal virtual string? ScoresPath => ScoresPathFromEnvironment;

    /// <summary>Whether the rows of this class are recorded: their scores in the score file and their rendered pictures in the dump
    /// folder. A class whose rows compare something other than the matplotlib picture of the row they name (the limit bind tests
    /// compare a blank page) says no, or its records would stand under the keys of the real rows, and its picture would be written to
    /// the very file the real row writes, by another class run beside it.</summary>
    internal virtual bool RecordsItsRows => true;

    private void RecordScore(string subdir, string fixtureName, PerceptualDiff.Result result, FidelityThresholds limits)
    {
        if (!RecordsItsRows || string.IsNullOrEmpty(ScoresPath)) return;
        string line = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{subdir}/{fixtureName}\t{result.Rms:F2}\t{result.Ssim:F4}\t{result.MaxDeltaE:F2}\t{limits.Rms:F2}\t{limits.Ssim:F4}\t{limits.DeltaE:F2}{Environment.NewLine}");
        lock (ScoresLock) File.AppendAllText(ScoresPath, line);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Rendering
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Renders a figure to PNG bytes using the Skia backend.</summary>
    protected static byte[] RenderToPng(Figure figure) =>
        figure.ToPng();

    // ──────────────────────────────────────────────────────────────────────────
    // Fixture loading
    // ──────────────────────────────────────────────────────────────────────────

    private static byte[] LoadFixture(string subdir, string name)
    {
        string path = Path.Combine(FixtureRoot, subdir, $"{name}.png");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Matplotlib reference fixture '{subdir}/{name}.png' not found at '{path}'.\n" +
                $"Run: python tools/mpl_reference/generate.py --all --style {(subdir == "v2" ? "v2" : "classic")}", path);
        return File.ReadAllBytes(path);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Threshold resolution
    // ──────────────────────────────────────────────────────────────────────────

    private FidelityThresholds GetThresholds(string? callerMember, string subdir)
    {
        if (callerMember is null)
            return FidelityThresholds.Defaults;

        var method = GetType().GetMethod(callerMember,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var attr = method?.GetCustomAttribute<FidelityToleranceAttribute>();
        return attr is null
            ? FidelityThresholds.Defaults
            : attr.For(subdir);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Diff image emission
    // ──────────────────────────────────────────────────────────────────────────

    private static string WriteDiff(string name, byte[] reference, byte[] actual)
    {
        Directory.CreateDirectory(FailureDir);
        string path = Path.Combine(FailureDir, $"{name}.diff.png");
        try { PerceptualDiff.WriteDiffImage(reference, actual, path); }
        catch { /* diff write failure must not hide the real assertion error */ }
        return path;
    }
}
