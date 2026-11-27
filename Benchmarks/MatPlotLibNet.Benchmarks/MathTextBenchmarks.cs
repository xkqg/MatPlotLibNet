// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Benchmarks;

/// <summary>What parsing the labels of a chart costs: five labels of the kind axes and legends carry, parsed one after the
/// other into the public flat view (<see cref="MathTextParser.Parse"/>): the tree a figure's first draw of a label parses and keeps
/// for its later draws, and the spans written from it, which no draw reads; and what writing them costs in the SVG of a figure that carries them as its titles, axis labels and
/// legend entries (<see cref="MathLabelledFigures"/>), an unchanged figure handed its kept subplots and its kept labels. The
/// class names no type of the Skia package, so its SVG is written with text, not glyph outlines; the pictures and the outlined
/// SVG are <see cref="MathTextSkiaBenchmarks"/>, measured in a run of their own.</summary>
[MemoryDiagnoser]
public class MathTextBenchmarks
{
    private MathLabelledFigures _figures = default!;

    [GlobalSetup]
    public void Setup() => _figures = new MathLabelledFigures();

    [Benchmark(Baseline = true)]
    public int ParseFiveLabels()
    {
        var spans = 0;
        foreach (var label in MathLabelledFigures.Labels) spans += MathTextParser.Parse(label).Spans.Count;
        return spans;
    }

    [Benchmark]
    public int ParseAMathLabel() => MathTextParser.Parse(MathLabelledFigures.Labels[2]).Spans.Count;

    /// <summary>The SVG of four panels whose titles, axis labels and legend entries hold math, under a figure's own theme: every
    /// label measured for the layout and written with its spans.</summary>
    [Benchmark]
    public string MathLabelled_ToSvg() => _figures.Default.ToSvg();

    /// <summary>The SVG of the same four panels under the matplotlib classic theme.</summary>
    [Benchmark]
    public string MathLabelledClassic_ToSvg() => _figures.Classic.ToSvg();
}
