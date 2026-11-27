// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The minus sign of a negative tick label. matplotlib 3.11.2: <c>rcParams['axes.unicode_minus']</c> is True in the default and in the
/// classic style, and <c>Formatter.fix_minus</c> replaces the hyphen of every number a <c>ScalarFormatter</c> writes (a tick label, the offset
/// text, a colour bar's label) with U+2212, so <c>[t.get_text() for t in ax.get_xticklabels()]</c> holds "−1.0". The library wrote the ASCII
/// hyphen (U+002D) under every theme and keeps writing it under its own themes. The labels a formatter of the axis' own writes (a date, a
/// percentage) are the formatter's.</summary>
public class TrueMinusTests
{
    private const char TrueMinus = '−';

    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static readonly Theme Classic = Theme.MatplotlibClassic;

    private static List<string> TextsOf(Theme theme, Action<AxesBuilder> chart) =>
        [.. Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, chart).ToSvg().Each("text").Select(e => e.Value)];

    private static readonly Action<AxesBuilder> Negative = ax => ax.Plot([-1.0, 0.0, 1.0], [-2.0, 0.0, 2.0]);

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void ANegativeTickLabel_IsWrittenWithTheTrueMinusSign_OnBothAxes(string theme)
    {
        var texts = TextsOf(theme == "v2" ? V2 : Classic, Negative);

        Assert.Contains(texts, t => t.StartsWith(TrueMinus) && t.Contains('1'));   // -1.0 on X
        Assert.Contains(texts, t => t.StartsWith(TrueMinus) && t.Contains('2'));   // -2.0 on Y
        Assert.DoesNotContain(texts, t => t.StartsWith('-'));
    }

    [Fact]
    public void UnderTheLibrarysThemes_ANegativeTickLabelKeepsItsHyphen()
    {
        var texts = TextsOf(Theme.Default, Negative);

        Assert.Contains(texts, t => t.StartsWith('-'));
        Assert.DoesNotContain(texts, t => t.Contains(TrueMinus));
    }

    [Fact]
    public void ALabelOfAFormatterTheAxisHasOfItsOwn_IsTheFormatters()
    {
        var texts = TextsOf(V2, ax => ax.Plot([-1.0, 0.0, 1.0], [-2.0, 0.0, 2.0])
            .SetYTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.PercentFormatter(100)));

        Assert.DoesNotContain(texts, t => t.Contains('%') && t.Contains(TrueMinus));   // the Y axis' own formatter writes its hyphen
        Assert.Contains(texts, t => t.Contains('%') && t.StartsWith('-'));
    }

    [Fact]
    public void TheScaleText_OfASmallAxis_WritesATrueMinusToo()
    {
        var texts = TextsOf(V2, ax => ax.Plot([0.0, 0.00002], [0.0, 1.0]));

        Assert.Contains("1e" + TrueMinus + "5", texts);
    }

    [Fact]
    public void ATheColourBarsNegativeLabel_IsWrittenWithTheTrueMinusSign()
    {
        var texts = TextsOf(V2, ax => ax.Heatmap(new double[,] { { -2, -1 }, { 1, 2 } }).WithColorBar());

        Assert.Contains(texts, t => t.StartsWith(TrueMinus));
        Assert.DoesNotContain(texts, t => t.StartsWith('-'));
    }

    [Fact]
    public void A3DAxisNegativeLabel_IsWrittenWithTheTrueMinusSign()
    {
        var texts = TextsOf(V2, ax => ax.Plot3D([-2.0, 0.0, 2.0], [-2.0, 0.0, 2.0], [-2.0, 0.0, 2.0]));

        Assert.Contains(texts, t => t.StartsWith(TrueMinus));
        Assert.DoesNotContain(texts, t => t.StartsWith('-'));
    }

    [Fact]
    public void TheOneRule_ReplacesEveryHyphenOfANumber_AndLeavesTheOtherTextAlone()
    {
        Assert.Equal("−1.5e−5", Theme.MatplotlibV2.Looks.TickLabels.Signed("-1.5e-5"));
        Assert.Equal("-1.5", Theme.Default.Looks.TickLabels.Signed("-1.5"));
        Assert.Equal("1.5", Theme.MatplotlibV2.Looks.TickLabels.Signed("1.5"));
        Assert.Equal(["−1", "0", "1"], Theme.MatplotlibV2.Looks.TickLabels.Labelled([-1.0, 0.0, 1.0]));
        Assert.Equal(["-1", "0", "1"], Theme.Default.Looks.TickLabels.Labelled([-1.0, 0.0, 1.0]));
    }
}
