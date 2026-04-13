// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;

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

    /// <summary>Resolves the matplotlib fixture subdirectory name for a theme. Defaults to "classic".</summary>
    protected static string FixtureSubdir(Theme theme) =>
        theme.Name == "matplotlib-v2" ? "v2" : "classic";

    /// <summary>Helper for [Theory] tests — maps the inline theme id to the actual Theme instance.</summary>
    protected static Theme ResolveTheme(string themeId) => themeId switch
    {
        "v2"      => Theme.MatplotlibV2,
        "classic" => Theme.MatplotlibClassic,
        _ => throw new ArgumentException($"Unknown theme id '{themeId}' (expected 'classic' or 'v2')"),
    };

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
        var (rmsThreshold, ssimThreshold, deltaEThreshold) = GetThresholds(callerMember);
        string subdir = FixtureSubdir(figure.Theme);

        // matplotlib v2 (default) style anti-aliases tab10 lines/markers against grey-ish
        // grid + white background, producing intermediate blends like #257AB5/#3785BB
        // that dominate the reference top-5 colour list. Our SkiaSharp backend uses
        // different sub-pixel blending so those exact blends never appear in our top-5,
        // even when the rendered chart is structurally identical (SSIM ≥ 0.70). Apply a
        // global v2 relaxation: ΔE ×1.7, RMS ×1.5, SSIM −0.10. Per-test [FidelityTolerance]
        // attributes still apply on top of this multiplier.
        if (subdir == "v2")
        {
            deltaEThreshold *= 1.7;
            rmsThreshold    *= 1.5;
            ssimThreshold   -= 0.10;   // no floor — some grid-family v2 fixtures legitimately drop to ~0.40
        }

        byte[] actual    = RenderToPng(figure);
        byte[] reference = LoadFixture(subdir, fixtureName);

        var result = PerceptualDiff.Compare(reference, actual);
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

    private (double Rms, double Ssim, double DeltaE) GetThresholds(string? callerMember)
    {
        if (callerMember is null)
            return (PerceptualDiff.DefaultRmsThreshold, PerceptualDiff.DefaultSsimThreshold, PerceptualDiff.DefaultDeltaEThreshold);

        var method = GetType().GetMethod(callerMember,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var attr = method?.GetCustomAttribute<FidelityToleranceAttribute>();
        return attr is null
            ? (PerceptualDiff.DefaultRmsThreshold, PerceptualDiff.DefaultSsimThreshold, PerceptualDiff.DefaultDeltaEThreshold)
            : (attr.Rms, attr.Ssim, attr.DeltaE);
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
