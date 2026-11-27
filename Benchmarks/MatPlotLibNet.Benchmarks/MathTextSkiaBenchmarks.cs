// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Skia;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What measuring, placing and drawing math labels costs with the Skia package loaded (<see cref="MathLabelledFigures"/>):
/// the figures as pictures, and as SVG whose text the package writes as glyph outlines. The setup loads the package, so an outlined
/// row is outlines even when it is the first row of a run; loading it changes how every later SVG of the process is written, so the
/// class is measured in a run of its own, apart from <see cref="MathTextBenchmarks"/>.</summary>
[MemoryDiagnoser]
public class MathTextSkiaBenchmarks
{
    private MathLabelledFigures _figures = default!;

    [GlobalSetup]
    public void Setup()
    {
        FigureSkiaExtensions.Initialize();
        _figures = new MathLabelledFigures();
    }

    /// <summary>The PNG of the four panels under the default theme: every math label laid out once and painted run by run.</summary>
    [Benchmark]
    public byte[] MathLabelled_ToPng() => _figures.Default.ToPng();

    /// <summary>The PNG of the four panels under the matplotlib v2 theme in a tight layout, each panel's title moved on to the next
    /// label before each draw so every panel is laid out and drawn again: every label measured as the matplotlib themes measure text.</summary>
    [Benchmark]
    public byte[] MathLabelledV2_ToPng() => _figures.NextV2().ToPng();

    /// <summary>The PNG of the four panels under the matplotlib classic theme.</summary>
    [Benchmark]
    public byte[] MathLabelledClassic_ToPng() => _figures.Classic.ToPng();

    /// <summary>The SVG of the four panels under the default theme, an unchanged figure handed its kept subplots, with every label
    /// written as glyph outlines.</summary>
    [Benchmark]
    public string MathLabelled_ToOutlinedSvg() => _figures.Default.ToSvg();

    /// <summary>The SVG of the four panels under the matplotlib classic theme, an unchanged figure handed its kept subplots, with
    /// every label written as glyph outlines and its math in the classic theme's Computer Modern faces.</summary>
    [Benchmark]
    public string MathLabelledClassic_ToOutlinedSvg() => _figures.Classic.ToSvg();
}
