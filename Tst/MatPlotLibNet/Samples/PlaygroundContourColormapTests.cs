// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// L.9 — every colormap offered in the Playground's dropdown must produce a
/// visually distinct Contour rendering. Pre-L.9 only a subset of the nine
/// colormap names actually took effect because <c>BuildContour</c> routed via
/// <c>AxesBuilder.WithColorMap(string)</c> and the registry lookup could
/// silently no-op on an unknown name. This file pins the behaviour end-to-end.
/// <para>Phase N.1 — the Playground now passes typed <see cref="IColorMap"/>
/// instances straight through; no registry lookup needed at the call site.</para>
/// </summary>
public class PlaygroundContourColormapTests
{
    private static PlaygroundOptions Defaults(IColorMap colorMap) =>
        new() { Title = "Test", ColorMap = colorMap };

    public static readonly IColorMap[] PlaygroundColormaps =
    {
        ColorMaps.Viridis, ColorMaps.Plasma, ColorMaps.Inferno, ColorMaps.Magma,
        ColorMaps.Turbo, ColorMaps.Coolwarm,
        SequentialColorMaps.Greys, SequentialColorMaps.Hot,
        ColorMaps.Jet,
    };

    [Theory]
    [MemberData(nameof(PlaygroundColormapsTheory))]
    public void Contour_AppliesSelectedColormap_ToSeries(IColorMap colorMap)
    {
        var fig = PlaygroundExamples.Build(PlaygroundExample.ContourPlot, Defaults(colorMap)).Figure;
        var contour = fig.SubPlots[0].Series
            .OfType<MatPlotLibNet.Models.Series.ContourSeries>()
            .First();
        Assert.NotNull(contour.ColorMap);
        // The typed instance flows through unchanged — no registry indirection.
        Assert.Same(colorMap, contour.ColorMap);
    }

    [Fact]
    public void Contour_AllNineColormaps_ProduceDistinctSvgOutput()
    {
        var svgs = PlaygroundColormaps
            .Select(cm => PlaygroundExamples.Build(PlaygroundExample.ContourPlot, Defaults(cm)).Figure.ToSvg())
            .ToList();

        // Any pair of distinct colormaps must yield distinct SVG.
        for (int i = 0; i < svgs.Count; i++)
        for (int j = i + 1; j < svgs.Count; j++)
        {
            Assert.True(svgs[i] != svgs[j],
                $"colormap '{PlaygroundColormaps[i].Name}' produced byte-identical SVG to '{PlaygroundColormaps[j].Name}' — routing bug resurfaced");
        }
    }

    public static IEnumerable<object[]> PlaygroundColormapsTheory() =>
        PlaygroundColormaps.Select(n => new object[] { n });
}

/// <summary>L.9 — <c>AxesBuilder.WithColorMap(string)</c> throws on unknown
/// names instead of silently returning the unchanged builder.</summary>
public class AxesBuilderColorMapStrictModeTests
{
    [Fact]
    public void WithColorMap_UnknownName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Plt.Create()
               .AddSubPlot(1, 1, 1, ax => ax
                   .Heatmap(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } })
                   .WithColorMap("definitely-not-a-real-colormap"))
               .Build());

        // Exception message should list the registered names so users can correct
        // the typo immediately.
        Assert.Contains("viridis", ex.Message);
    }

    [Fact]
    public void WithColorMap_KnownName_AppliesWithoutThrowing()
    {
        // Sanity — the valid path stays intact.
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new[,] { { 1.0, 2.0 }, { 3.0, 4.0 } })
                .WithColorMap("plasma"))
            .Build();
        Assert.NotNull(fig);
    }
}
