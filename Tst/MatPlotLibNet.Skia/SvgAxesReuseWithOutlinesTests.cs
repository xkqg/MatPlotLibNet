// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>With the package's glyph source installed, the SVG's text is written as outlines; an axes nothing changed in is still
/// handed out again from what the draw before kept, under every theme, with plain labels and with math in them.
/// <para>In the fonts' global-state collection: the text measurer of the process is part of every axes' key, and a test there
/// swaps it mid-run, which rightly draws every axes again.</para></summary>
[Collection("SkiaFontsGlobalState")]
public sealed class SvgAxesReuseWithOutlinesTests
{
    public static TheoryData<string, bool> ThemesAndLabels { get; } = new()
    {
        { "default", false }, { "default", true },
        { "matplotlib-v2", false }, { "matplotlib-v2", true },
        { "matplotlib-classic", false }, { "matplotlib-classic", true },
    };

    [Theory]
    [MemberData(nameof(ThemesAndLabels))]
    public void AFigureNothingChangedIn_HandsOutEveryAxes_UnderEveryTheme(string theme, bool mathLabels)
    {
        Assert.IsType<SkiaGlyphPathProvider>(GlyphPathProviderDefault.Current);
        var figure = FourPanels(theme, mathLabels);
        _ = figure.ToSvg();
        _ = figure.ToSvg();
        var kept = figure.DrawnSvgAxes!;

        string svg = figure.ToSvg();

        for (int i = 0; i < kept.Length; i++) Assert.Same(kept[i], figure.DrawnSvgAxes![i]);
        Assert.Equal(FromScratch(figure), svg);
    }

    /// <summary>Four panels whose titles, axis labels and legend entries are plain text or hold math.</summary>
    private static Figure FourPanels(string theme, bool mathLabels)
    {
        var look = theme.AsTheme();
        var x = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var y = x.Select(v => v * v / 50).ToArray();
        var builder = Plt.Create().WithTheme(look).WithSize(800, 600).WithTitle(mathLabels ? @"$\alpha_i^2 + \beta_i$" : "plain");
        for (int i = 0; i < 4; i++)
            builder = builder.AddSubPlot(2, 2, i + 1, ax => ax
                .Plot(x, y, line => line.Label = mathLabels ? @"$\sqrt{x^2 + y^2}$" : "line")
                .WithTitle(mathLabels ? @"Latency ($\mu$s)" : "title")
                .SetXLabel(mathLabels ? @"$\frac{\Delta y}{\Delta x}$" : "x")
                .SetYLabel("y")
                .WithLegend());
        return builder.Build();
    }

    /// <summary>The picture <paramref name="figure"/> draws from scratch, leaving what the last draw kept as it was.</summary>
    private static string FromScratch(Figure figure)
    {
        var kept = figure.DrawnSvgAxes;
        figure.DrawnSvgAxes = null;
        try
        {
            return figure.ToSvg();
        }
        finally
        {
            figure.DrawnSvgAxes = kept;
        }
    }
}
