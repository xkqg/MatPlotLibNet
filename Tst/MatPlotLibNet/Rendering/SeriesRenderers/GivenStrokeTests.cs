// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A line width or a dash pattern the author gives is drawn as given; one the author leaves alone is the theme's. The
/// series keeps what was given apart from what it reads at rest (a nullable value behind the public one), so a width of 1.5 that
/// was given cannot be taken for the 1.5 a new line reads. matplotlib 3.11.2: <c>plt.plot(x, y, lw=1.5)</c> under the default
/// style draws 1.5 pt, and <c>plt.plot(x, y)</c> draws <c>rcParams['lines.linewidth']</c>, 1.5 pt too; the library states a
/// series' width in pixels, so a given 1.5 is 1.5 px and the theme's is 1.5 pt = 2.08 px. An explicit <c>ls='-'</c> or
/// <c>lw</c> wins over the prop_cycle's, as an explicit colour does.</summary>
public class GivenStrokeTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 2.0, 1.5, 3.0];
    private static readonly double[] Low = [0.1, 0.1, 0.1, 0.1];
    private static readonly double[] Sample = [1.0, 2, 2, 3, 3, 3, 4, 4, 5, 7];
    private static readonly double[] Angles = [0.0, 1.5, 3.0, 4.5];

    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static string Blue => V2.CycleColors[0].ToHex();

    private static XDocument Doc(Theme theme, Action<AxesBuilder> build) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, build).ToSvg().Xml();

    /// <summary>The widths of every element stroked in the first cycle colour, outside the legend.</summary>
    private static List<string> Widths(XDocument doc, string colour) =>
        [.. doc.Descendants()
            .Where(e => (string?)e.Attribute("stroke") == colour && e.Attribute("stroke-width") is not null
                && !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))
            .Select(e => (string)e.Attribute("stroke-width")!)];

    private static Action<T> Width<T>(double? width, Action<T, double> set) => s => { if (width is { } w) set(s, w); };

    /// <summary>Every series whose line the theme widens when it names no width.</summary>
    public static TheoryData<string> Families => new()
    {
        "line", "step", "errorbar", "ecdf", "kde", "signal", "signalxy", "sparkline", "polar", "ternary", "line3d",
    };

    private static Action<AxesBuilder> One(string family, double? width) => family switch
    {
        "line" => ax => ax.Plot(X, Y, Width<LineSeries>(width, (s, w) => s.LineWidth = w)),
        "step" => ax => ax.Step(X, Y, Width<StepSeries>(width, (s, w) => s.LineWidth = w)),
        "errorbar" => ax => ax.ErrorBar(X, Y, Low, Low, Width<ErrorBarSeries>(width, (s, w) => s.LineWidth = w)),
        "ecdf" => ax => ax.Ecdf(Sample, Width<EcdfSeries>(width, (s, w) => s.LineWidth = w)),
        "kde" => ax => ax.Kde(Sample, Width<KdeSeries>(width, (s, w) => s.LineWidth = w)),
        "signal" => ax => ax.Signal(Y, 1.0, 0.0, Width<SignalSeries>(width, (s, w) => s.LineWidth = w)),
        "signalxy" => ax => ax.SignalXY(X, Y, Width<SignalXYSeries>(width, (s, w) => s.LineWidth = w)),
        "sparkline" => ax => ax.Sparkline(Y, Width<SparklineSeries>(width, (s, w) => s.LineWidth = w)),
        "polar" => ax => ax.PolarPlot(Y, Angles, Width<PolarLineSeries>(width, (s, w) => s.LineWidth = w)),
        "ternary" => ax => ax.TernaryPlot([0.2, 0.3, 0.5], [0.5, 0.3, 0.2], [0.3, 0.4, 0.3],
            Width<TernaryLineSeries>(width, (s, w) => s.LineWidth = w)),
        "line3d" => ax => ax.Plot3D(X, Y, Y, Width<Line3DSeries>(width, (s, w) => s.LineWidth = w)),
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Not a line family."),
    };

    [Theory]
    [MemberData(nameof(Families))]
    public void AWidthOfOnePointFiveTheAuthorGives_IsDrawnOnePointFive_UnderV2(string family)
    {
        var widths = Widths(Doc(V2, One(family, 1.5)), Blue);

        Assert.NotEmpty(widths);
        Assert.All(widths, w => Assert.Equal("1.5", w));
    }

    [Theory]
    [MemberData(nameof(Families))]
    public void AWidthTheAuthorLeavesAlone_IsTheThemes_UnderV2(string family)
    {
        var widths = Widths(Doc(V2, One(family, null)), Blue);

        Assert.NotEmpty(widths);
        Assert.All(widths, w => Assert.Equal("2.08", w));
    }

    [Fact]
    public void TheLegendSwatch_OfALineGivenOnePointFive_IsOnePointFive()
    {
        var doc = Doc(V2, ax => ax.Plot(X, Y, s => { s.Label = "a"; s.LineWidth = 1.5; }).WithLegend());

        var legend = Assert.Single(doc.Descendants(), e => (string?)e.Attribute("class") == "legend");
        var swatch = legend.Descendants().First(e => (string?)e.Attribute("stroke") == Blue);
        Assert.Equal("1.5", (string)swatch.Attribute("stroke-width")!);
    }

    /// <summary>A cycler that names a width for the slot (4) gives way to a width the author gives, 1.5 included.</summary>
    [Fact]
    public void AWidthTheAuthorGives_WinsOverTheCyclers_EvenWhenItIsOnePointFive()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithLineWidths(4.0).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

        Assert.Equal(["1.5"], Widths(Doc(theme, ax => ax.Plot(X, Y, s => s.LineWidth = 1.5)), Colors.Red.ToHex()));
        Assert.Equal(["4"], Widths(Doc(theme, ax => ax.Plot(X, Y)), Colors.Red.ToHex()));
    }

    /// <summary>A solid line the author asks for stays solid on a slot whose cycler dashes the line.</summary>
    [Fact]
    public void ASolidLineTheAuthorGives_WinsOverTheCyclersDash()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithLineStyles(LineStyle.Dashed).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

        static string? Dash(XDocument doc) => (string?)Assert.Single(doc.Each("polyline"), e => (string?)e.Attribute("stroke") == Colors.Red.ToHex()).Attribute("stroke-dasharray");

        Assert.Null(Dash(Doc(theme, ax => ax.Plot(X, Y, s => s.LineStyle = LineStyle.Solid))));
        Assert.NotNull(Dash(Doc(theme, ax => ax.Plot(X, Y))));
    }

    [Fact]
    public void ALineLeftAlone_WritesNoWidthAndNoDash_AndAGivenOneWritesBoth()
    {
        var unset = new LineSeries(X, Y).ToSeriesDto();
        var given = new LineSeries(X, Y) { LineWidth = 1.5, LineStyle = LineStyle.Solid }.ToSeriesDto();

        Assert.Null(unset.LineWidth);
        Assert.Null(unset.LineStyle);
        Assert.Equal(1.5, given.LineWidth);
        Assert.Equal("solid", given.LineStyle);
    }

    [Fact]
    public void AGivenWidthOfOnePointFive_SurvivesTheJson_AndIsDrawnAsGiven()
    {
        var figure = Plt.Create().WithTheme(V2).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.LineWidth = 1.5)).Build();

        var back = figure.RoundTripped();

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(1.5, ((LineSeries)back.SubPlots[0].Series[0]).Stroke.Width);
    }

    /// <summary>JSON written before this release names the line's 1.5 and solid: it reads back as given, which is what that
    /// release drew.</summary>
    [Fact]
    public void AnEarlierJsonsWidth_ReadsBackAsGiven()
    {
        var dto = new SeriesDto { Type = "line", XData = X, YData = Y, LineWidth = 1.5, LineStyle = "solid" };
        var axes = new Axes();

        var line = LineSeries.FromSeriesDto(axes, dto);

        Assert.Equal(new GivenStroke(1.5, LineStyle.Solid), line.Stroke);
    }

    /// <summary>An indicator that names no width or dash leaves its lines to the theme; one that names 1.5 and solid gives both.</summary>
    [Fact]
    public void AnIndicatorsLines_FollowTheGivenRule()
    {
        double[] close = [.. Enumerable.Range(0, 40).Select(i => 10.0 + i % 7)];
        var unsetAxes = new Axes();
        var givenAxes = new Axes();

        new Sma(close, 5).Apply(unsetAxes);
        new Sma(close, 5) { LineWidth = 1.5, LineStyle = LineStyle.Solid }.Apply(givenAxes);

        var unset = Assert.IsType<LineSeries>(Assert.Single(unsetAxes.Series));
        var given = Assert.IsType<LineSeries>(Assert.Single(givenAxes.Series));
        Assert.Equal(default, unset.Stroke);
        Assert.Equal(new GivenStroke(1.5, LineStyle.Solid), given.Stroke);
    }

    [Fact]
    public void AStreamingLine_FollowsTheGivenRule()
    {
        static XDocument Streamed(Action<StreamingLineSeries> configure)
        {
            var figure = Plt.Create().WithTheme(V2).WithSize(600, 400).Build();
            var line = new StreamingLineSeries(16);
            configure(line);
            var axes = figure.AddSubPlot();
            axes.AddSeries(line);
            for (int i = 0; i < 8; i++) line.AppendPoint(i, i % 3);
            return figure.ToSvg().Xml();
        }

        Assert.All(Widths(Streamed(_ => { }), Blue), w => Assert.Equal("2.08", w));
        Assert.All(Widths(Streamed(s => s.LineWidth = 1.5), Blue), w => Assert.Equal("1.5", w));
        var dashed = Streamed(s => s.LineStyle = LineStyle.Dashed);
        Assert.Contains(dashed.Descendants(), e => (string?)e.Attribute("stroke") == Blue && e.Attribute("stroke-dasharray") is not null);
    }

    [Fact]
    public void AStreamingLine_WritesWhatItWasGiven_AndReadsItBackAsGiven()
    {
        var unset = new StreamingLineSeries(8).ToSeriesDto();
        var given = new StreamingLineSeries(8) { LineWidth = 1.5, LineStyle = LineStyle.Solid }.ToSeriesDto();

        Assert.Null(unset.LineWidth);
        Assert.Null(unset.LineStyle);
        var back = StreamingLineSeries.FromSeriesDto(new Axes(), given);
        Assert.Equal(new GivenStroke(1.5, LineStyle.Solid), back.Stroke);
    }

    /// <summary>Giving a line the width it reads at rest changes what it draws under the matplotlib themes, so it is a change
    /// of the line; giving it the same width again is not.</summary>
    [Fact]
    public void GivingTheWidthALineReadsAtRest_IsAChange_AndGivingItAgainIsNot()
    {
        var line = new LineSeries(X, Y);
        long before = line.LatestChange;

        line.LineWidth = line.LineWidth;
        long given = line.LatestChange;
        line.LineWidth = 1.5;

        Assert.True(given > before);
        Assert.Equal(given, line.LatestChange);
    }

    /// <summary>A cycler that names a width of 1.5 for the slot is a cycler that names a width: a line given none is drawn 1.5,
    /// not the theme's 2.08, as a prop_cycle linewidth wins over <c>lines.linewidth</c> in matplotlib, and a regression line
    /// takes it too. A cycler that names no width still leaves the line to the theme.</summary>
    [Fact]
    public void ACyclerThatNamesOnePointFive_NamesAWidth()
    {
        var names = Theme.CreateFrom(V2).WithPropCycler(new PropCyclerBuilder().WithColors(Colors.Red).WithLineWidths(1.5).Build()).Build();
        var namesNone = Theme.CreateFrom(V2).WithPropCycler(new PropCyclerBuilder().WithColors(Colors.Red).Build()).Build();

        Assert.Equal(["1.5"], Widths(Doc(names, ax => ax.Plot(X, Y)), Colors.Red.ToHex()));
        Assert.Equal(["1.5"], Widths(Doc(names, ax => ax.Regression(X, Y)), Colors.Red.ToHex()));
        Assert.Equal(["2.08"], Widths(Doc(namesNone, ax => ax.Plot(X, Y)), Colors.Red.ToHex()));
        Assert.Equal(["2"], Widths(Doc(namesNone, ax => ax.Regression(X, Y)), Colors.Red.ToHex()));
    }

    /// <summary>The figure JSON keeps whether the cycler names widths: a cycler of colours writes none and reads back naming none
    /// (its lines stay the theme's 2.08), one that names 1.5 writes it and reads back naming it.</summary>
    [Fact]
    public void WhetherACyclerNamesWidths_SurvivesTheJson()
    {
        foreach (var cycler in new[]
        {
            new PropCyclerBuilder().WithColors(Colors.Red).Build(),
            new PropCyclerBuilder().WithColors(Colors.Red).WithLineWidths(1.5).Build(),
        })
        {
            var theme = Theme.CreateFrom(V2).WithPropCycler(cycler).Build();
            var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y)).Build();

            var back = figure.RoundTripped();

            Assert.Equal(cycler.LineWidthCycle.Count, back.Theme.PropCycler!.LineWidthCycle.Count);
            Assert.Equal(figure.ToSvg(), back.ToSvg());
        }
    }
}
