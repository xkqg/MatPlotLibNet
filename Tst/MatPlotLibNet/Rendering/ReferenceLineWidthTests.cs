// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A reference line given no width is drawn as matplotlib's <c>axhline</c> and <c>axvline</c> draw one: a <c>Line2D</c> the
/// verb makes itself, so no cycler names its width and it takes <c>lines.linewidth</c>. matplotlib 3.11.2, printed:
/// <c>ax.axhline(1).get_linewidth()</c> and <c>ax.axvline(1).get_linewidth()</c> are 1.5 under <c>plt.style.use('default')</c> and 1.0
/// under <c>'classic'</c>, 2.08 and 1.39 px at 100 dpi. The library's own themes keep the 1 px a reference line has always rested at, and
/// a width the caller gives (1 included) is drawn as given, under every theme.</summary>
public class ReferenceLineWidthTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 2.0, 1.0];
    private static string DrawnWidth(Theme theme, Orientation orientation, Action<ReferenceLine>? configure = null)
    {
        var svg = Plt.Create().WithTheme(theme).WithSize(400, 300).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, Y);
            Action<ReferenceLine> dress = line => { line.Color = Colors.Red; configure?.Invoke(line); };
            if (orientation == Orientation.Horizontal) ax.AxHLine(1, dress);
            else ax.AxVLine(1, dress);
        }).ToSvg();
        return Assert.Single(svg.Xml().Descendants(), e => (string?)e.Attribute("stroke") == Colors.Red.ToHex())
            .Attribute("stroke-width")!.Value;
    }

    [Theory]
    [InlineData("MatplotlibV2", Orientation.Horizontal, "2.08")]
    [InlineData("MatplotlibV2", Orientation.Vertical, "2.08")]
    [InlineData("MatplotlibClassic", Orientation.Horizontal, "1.39")]
    [InlineData("MatplotlibClassic", Orientation.Vertical, "1.39")]
    [InlineData("Default", Orientation.Horizontal, "1")]
    [InlineData("Dark", Orientation.Vertical, "1")]
    public void ALineGivenNoWidth_IsAsWideAsItsThemesLines(string themeName, Orientation orientation, string expected)
    {
        Assert.Equal(expected, DrawnWidth(themeName.AsTheme(), orientation));
    }

    [Fact]
    public void AGivenWidth_IsDrawnAsGiven_UnderEveryTheme()
    {
        Assert.Equal("1", DrawnWidth(Theme.MatplotlibV2, Orientation.Horizontal, line => line.LineWidth = 1));
        Assert.Equal("3", DrawnWidth(Theme.Default, Orientation.Vertical, line => line.LineWidth = 3));
    }

    [Fact]
    public void TheWidth_ReadsOnePixelAtRest_UntilGiven()
    {
        var line = new ReferenceLine(1, Orientation.Horizontal);

        Assert.Equal(1.0, line.LineWidth);
        Assert.Null(line.GivenLineWidth);
        line.LineWidth = 1.0;
        Assert.Equal(1.0, line.GivenLineWidth);
    }

    /// <summary>The JSON writes a width that was given and none at rest, so a line read back at rest takes its theme's again; and a
    /// line written with no width reads back at rest (it read as 0 before: no line at all).</summary>
    [Fact]
    public void TheJson_WritesAGivenWidthOnly_AndALineWithNoWidthReadsBackAtRest()
    {
        var atRest = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).AxHLine(1)).Build();
        var given = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).AxHLine(1, line => line.LineWidth = 1)).Build();

        var back = atRest.RoundTripped().SubPlots[0].ReferenceLines[0];

        Assert.DoesNotContain("\"lineWidth\"", ReferenceLinesOf(atRest.ToJson()));
        Assert.Contains("\"lineWidth\":1", ReferenceLinesOf(given.ToJson()));
        Assert.Null(back.GivenLineWidth);
        Assert.Equal(1.0, back.LineWidth);
        Assert.Equal(atRest.ToSvg(), atRest.RoundTripped().ToSvg());
    }

    /// <summary>A figure's JSON written by hand (or by a chart spec) may leave the width out: the line is drawn at rest, not 0 wide.</summary>
    [Fact]
    public void AReferenceLineWrittenWithNoWidth_ReadsBackAtRest()
    {
        const string json = """{"width":400,"height":300,"subPlots":[{"referenceLines":[{"value":1,"orientation":"horizontal","lineStyle":"solid"}]}]}""";

        var line = ChartSerializerDefault.Current.FromJson(json).SubPlots[0].ReferenceLines[0];

        Assert.Equal(1.0, line.LineWidth);
    }

    /// <summary>A Fibonacci retracement hands its levels its own width (the indicator's 1.5 at rest) as a given one, so they keep it under
    /// every theme.</summary>
    [Fact]
    public void AFibonacciRetracementsLevels_KeepTheIndicatorsWidth()
    {
        var axes = new Axes();
        new FibonacciRetracement(1, 2).Apply(axes);

        Assert.All(axes.ReferenceLines, line => Assert.Equal(1.5, line.GivenLineWidth));
    }

    private static string ReferenceLinesOf(string json) => json[json.IndexOf("\"referenceLines\"", StringComparison.Ordinal)..];
}
