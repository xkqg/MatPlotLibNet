// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace MatPlotLibNet.Benchmarks.MatplotlibThemes;

/// <summary>What one draw costs of each figure the matplotlib themes are judged by, written as SVG (see
/// <see cref="MatplotlibThemeFigures"/>). Every row changes the figure before it draws, so each is a draw and not the kept drawing
/// of an unchanged figure. The class does not touch the Skia package: that package changes how SVG text is written once it is
/// loaded, so its rows are in <see cref="MatplotlibThemeSkiaBenchmarks"/>, which a run of its own measures.</summary>
[MemoryDiagnoser]
public class MatplotlibThemeSvgBenchmarks
{
    private MatplotlibThemeFigures _figures = default!;
    private int _tick;

    [GlobalSetup]
    public void Setup() => _figures = new MatplotlibThemeFigures();

    private string Draw(string row) => _figures.Of(row, MatplotlibThemeOutput.Svg).ChangedFor(_tick++).ToSvg();

    /// <summary>Nine panels under the matplotlib v2 theme, each with minor ticks and a grid on both tick sets.</summary>
    [Benchmark(Baseline = true)]
    public string V2Grid3x3() => Draw(nameof(V2Grid3x3));

    /// <summary>The same nine panels with no minor ticks switched on: a grid that asks for its minor lines stands on the major ticks
    /// alone under the matplotlib themes, as matplotlib's does.</summary>
    [Benchmark]
    public string V2MinorGridNoTicks3x3() => Draw(nameof(V2MinorGridNoTicks3x3));

    /// <summary>The same nine panels under the classic theme.</summary>
    [Benchmark]
    public string ClassicGrid3x3() => Draw(nameof(ClassicGrid3x3));

    /// <summary>Nine panels under the matplotlib v2 theme with a log Y axis and a grid on its major and minor ticks, the minor ticks
    /// the ones the log scale places.</summary>
    [Benchmark]
    public string V2LogGrid3x3() => Draw(nameof(V2LogGrid3x3));

    /// <summary>A heatmap with a colour bar extended at both ends.</summary>
    [Benchmark]
    public string V2HeatmapColorBarExtend() => Draw(nameof(V2HeatmapColorBarExtend));

    /// <summary>A line of 10 000 points, an inset showing a stretch of it, and the zoom indicator between them.</summary>
    [Benchmark]
    public string V2InsetZoom10k() => Draw(nameof(V2InsetZoom10k));

    /// <summary>Twenty patches over a line.</summary>
    [Benchmark]
    public string V2Patches20() => Draw(nameof(V2Patches20));

    /// <summary>Seven panels and the legend of the figure.</summary>
    [Benchmark]
    public string V2SevenPanelsFigLegend() => Draw(nameof(V2SevenPanelsFigLegend));

    /// <summary>A line contour under the classic theme.</summary>
    [Benchmark]
    public string ClassicContour() => Draw(nameof(ClassicContour));

    /// <summary>A 3-D surface whose SVG carries the script that turns it in the browser; the length of the document is printed by
    /// the benchmark program's <c>--theme-rows-bytes</c>.</summary>
    [Benchmark]
    public string V2Interactive3D() => Draw(nameof(V2Interactive3D));

    /// <summary>Two heatmaps with a labelled colour bar each under the constrained layout.</summary>
    [Benchmark]
    public string V2ConstrainedColorBar() => Draw(nameof(V2ConstrainedColorBar));

    /// <summary>Twenty stems of a sine: the stems, their heads and the baseline.</summary>
    [Benchmark]
    public string V2Stem() => Draw(nameof(V2Stem));

    /// <summary>Sixty-four wind barbs: staffs, half and full barbs, and the circles of calm.</summary>
    [Benchmark]
    public string V2Barbs() => Draw(nameof(V2Barbs));

    /// <summary>A streamplot of a 40 by 40 field: where its streamlines are seeded, how far each is integrated, and their arrows.</summary>
    [Benchmark]
    public string V2Streamplot() => Draw(nameof(V2Streamplot));

    /// <summary>A swarm plot of three groups of a hundred dots: where each dot stands, given the dots already placed.</summary>
    [Benchmark]
    public string V2Swarm() => Draw(nameof(V2Swarm));
}
