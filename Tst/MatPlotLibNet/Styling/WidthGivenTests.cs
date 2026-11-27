// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A kind states the width it was given and the rule it is drawn wide by once (<see cref="IWidthGiven"/>, <see cref="IGivenStroked"/>),
/// and every reader takes the kind: the width it reports, the slot, the theme's door, the legend swatch.</summary>
public class WidthGivenTests
{
    private static ISeries Series(string kind) => DrawnSeriesFixtures.Instance(kind);

    public static TheoryData<string, string> KindRules { get; } = new()
    {
        { nameof(EventplotSeries), nameof(LineWidthRule.OnePixelCollection) },
        { nameof(StreamplotSeries), nameof(LineWidthRule.OnePixelCollection) },
        { nameof(OhlcBarSeries), nameof(LineWidthRule.LineCollection) },
        { nameof(WireframeSeries), nameof(LineWidthRule.HalfPixelCollection) },
        { nameof(ErrorBarSeries), nameof(LineWidthRule.Line) },
        { nameof(SparklineSeries), nameof(LineWidthRule.Line) },
        { nameof(RegressionSeries), nameof(LineWidthRule.TwoPixelLine) },
        { nameof(LineSeries), nameof(LineWidthRule.Line) },
        { nameof(KdeSeries), nameof(LineWidthRule.Line) },
    };

    private static LineWidthRule Named(string rule) => (LineWidthRule)typeof(LineWidthRule)
        .GetProperty(rule, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;

    [Theory]
    [MemberData(nameof(KindRules))]
    public void AKind_StatesItsRuleOnce_ThroughTheInterface(string kind, string rule)
    {
        Assert.Same(Named(rule), ((IWidthGiven)Series(kind)).WidthRule);
    }

    [Fact]
    public void TheKindsThatAreNoSeries_StateTheirRulesToo()
    {
        Assert.Same(LineWidthRule.OnePixelCollection, ((IWidthGiven)new ReferenceLine(1, Orientation.Horizontal)).WidthRule);
        Assert.Same(LineWidthRule.OnePixelLine, ((IWidthGiven)new StreamingSignalSeries(16, 1, 0)).WidthRule);
    }

    [Fact]
    public void AKindGivenNoWidth_ReportsItsRulesResting_AndAGivenWidthAsGiven()
    {
        var events = (EventplotSeries)Series(nameof(EventplotSeries));
        var regression = (RegressionSeries)Series(nameof(RegressionSeries));

        Assert.Equal(1, events.LineWidth);
        Assert.Equal(2, regression.LineWidth);
        events.LineWidth = 3;
        regression.LineWidth = 1.5;
        Assert.Equal(3, events.LineWidth);
        Assert.Equal(1.5, regression.LineWidth);
        Assert.Equal(3, ((IWidthGiven)events).GivenLineWidth);
        Assert.Equal(1.5, ((IWidthGiven)regression).GivenLineWidth);
    }

    [Fact]
    public void TheSlot_DrawsAKindsWidthAndStroke_ByItsRule()
    {
        var slot = Theme.Default.SeriesSlot(0);
        var events = (IWidthGiven)Series(nameof(EventplotSeries));
        var regression = (IGivenStroked)Series(nameof(RegressionSeries));

        Assert.Equal(slot.LineWidthOf(null, LineWidthRule.OnePixelCollection), slot.LineWidthOf(events));
        Assert.Equal(new StrokeStyle(Colors.Blue, 2, LineStyle.Solid), slot.StrokeOf(Colors.Blue, regression));
        Assert.Equal(LineWidthRule.OwnHairline.Resting, slot.LineWidthOf(LineWidthRule.OwnHairline));
        Assert.Equal(1, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(LineWidthRule.OwnPixel));
    }

    [Fact]
    public void TheThemesDoor_ReadsTheKind()
    {
        var line = new ReferenceLine(1, Orientation.Horizontal) { LineWidth = 4 };

        Assert.Equal(4, Theme.Default.Looks.Lines.WidthOf(line));
        Assert.Equal(Theme.Default.Looks.Lines.WidthOf(null, LineWidthRule.OnePixelCollection),
            Theme.Default.Looks.Lines.WidthOf(new ReferenceLine(1, Orientation.Horizontal)));
    }

    [Fact]
    public void TheLegendSwatch_ReadsTheKind()
    {
        var regression = (RegressionSeries)Series(nameof(RegressionSeries));
        regression.LineWidth = 3;
        var bars = (ErrorBarSeries)Series(nameof(ErrorBarSeries));

        var line = LegendSwatch.Line(regression);
        var of = LegendSwatch.Of(LegendSwatchKind.ErrorBar, bars);

        Assert.Same(LineWidthRule.TwoPixelLine, line.LineRule);
        Assert.Equal(3, line.Stroke.Width);
        Assert.Same(LineWidthRule.Line, of.LineRule);
        Assert.Null(of.Stroke.Width);
    }

    [Fact]
    public void ASeriesText_IsTheSizeGiven_ElseTheThemes()
    {
        var table = (IGivenTextSize)Series(nameof(TableSeries));
        var text = (TableSeries)table;

        Assert.Equal(Theme.Default.SeriesTextSizeOver(null, table.RestingTextSize), table.TextSizeUnder(Theme.Default));
        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Size, table.TextSizeUnder(Theme.MatplotlibV2));
        text.FontSize = 17;
        Assert.Equal(17, table.TextSizeUnder(Theme.MatplotlibV2));
    }

    [Fact]
    public void AQuiverKeysLabel_IsThePointsGiven_ElseTheThemesTextUnderEveryTheme()
    {
        var key = (QuiverKeySeries)Series(nameof(QuiverKeySeries));

        Assert.Equal(Theme.Default.DefaultFont.Size, key.LabelSizeUnder(Theme.Default));
        key.FontSize = 10;
        Assert.Equal(10.0.PointsInPixels, key.LabelSizeUnder(Theme.Default));
    }

    [Fact]
    public void APointPlotsLines_AreTheWidthGiven_ElseTheDotLooksRule()
    {
        var points = (PointplotSeries)Series(nameof(PointplotSeries));
        var slot = Theme.Default.SeriesSlot(0);

        Assert.Equal(slot.LineWidthOf(null, Theme.Default.Looks.Dots.PointplotLine), points.LineWidthIn(slot));
        points.LineWidth = 5;
        Assert.Equal(5, points.LineWidthIn(slot));
    }

    [Fact]
    public void TheLinesOfAContour_AreACollectionsWidth()
    {
        var contour = (ContourSeries)Series(nameof(ContourSeries));
        var tricontour = (TricontourSeries)Series(nameof(TricontourSeries));

        Assert.Equal(Theme.MatplotlibV2.Looks.Lines.WidthOf(null, LineWidthRule.LineCollection), contour.LineWidthUnder(Theme.MatplotlibV2));
        Assert.Equal(1, tricontour.LineWidthUnder(Theme.Default));
    }
}

