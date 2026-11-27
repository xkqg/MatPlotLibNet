// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The stroke of a drawn annotation line, worked out in one place (<see cref="IStrokedTool"/>,
/// <see cref="StrokedToolExtensions"/>): the colour it names, else the one its kind takes where it names none (the theme's neutral,
/// the colour a verb chose for a reference line, the fill of a span for its border), the width its kind draws (a reference line
/// takes the theme's line width where it was given none) and its dash (a Fibonacci retracement's are always dashed).</summary>
public class StrokedToolTests
{
    private static readonly Color Red = Colors.Red;

    public static TheoryData<string> Tools => ["trendline", "level", "fibonacci", "zoom"];

    private static IStrokedTool Make(string kind) => kind switch
    {
        "trendline" => new Trendline(0, 0, 1, 1),
        "level" => new HorizontalLevel(3),
        "fibonacci" => new FibonacciRetracement(10, 5),
        _ => new ZoomIndicator(),
    };

    [Theory]
    [MemberData(nameof(Tools))]
    public void AToolThatNamesNoColour_IsStrokedInTheThemesNeutral_AsWideAndDashedAsItIs(string kind)
    {
        var tool = Make(kind);

        var stroke = tool.StrokeUnder(Theme.Nord);

        Assert.Equal(Theme.Nord.Neutral, stroke.Color);
        Assert.Equal(tool.LineWidth, stroke.Thickness);
        Assert.Equal(tool.LineStyle, stroke.Style);
    }

    [Fact]
    public void AColourTheToolNames_WinsOverTheNeutral()
    {
        var trendline = new Trendline(0, 0, 1, 1) { Color = Red, LineWidth = 3, LineStyle = LineStyle.Dotted };

        Assert.Equal(new StrokeStyle(Red, 3, LineStyle.Dotted), trendline.StrokeUnder(Theme.Default));
        Assert.Equal(Red, trendline.ColorUnder(Theme.Default));
    }

    [Fact]
    public void AFibonacciRetracement_IsAlwaysDashed()
    {
        Assert.Equal(LineStyle.Dashed, new FibonacciRetracement(10, 5).StrokeUnder(Theme.Default).Style);
    }

    /// <summary>A reference line given no colour is drawn in the colour its verb chose (the first slot of the cycle under the matplotlib
    /// themes, as <c>axhline</c> draws <c>lines.color</c>), else in the neutral; given no width, as wide as the theme's lines.</summary>
    [Fact]
    public void AReferenceLine_TakesItsVerbsColour_AndTheThemesLineWidth_WhereItWasGivenNone()
    {
        var plain = new ReferenceLine(1, Orientation.Horizontal);
        var chosen = new ReferenceLine(1, Orientation.Horizontal) { VerbColor = Colors.FibonacciOrange };

        Assert.Equal(Theme.Default.Neutral, plain.StrokeUnder(Theme.Default).Color);
        Assert.Equal(Theme.Default.SeriesSlot(0).ChosenOrCycleColor(Colors.FibonacciOrange), chosen.StrokeUnder(Theme.Default).Color);
        Assert.Equal(Theme.MatplotlibV2.SeriesSlot(0).ChosenOrCycleColor(Colors.FibonacciOrange), chosen.StrokeUnder(Theme.MatplotlibV2).Color);
        Assert.Equal(Theme.MatplotlibV2.Looks.Lines.WidthOf(null, LineWidthRule.OnePixelCollection), plain.StrokeUnder(Theme.MatplotlibV2).Thickness);
        Assert.Equal(1, plain.StrokeUnder(Theme.Default).Thickness);
        Assert.Equal(2.5, new ReferenceLine(1, Orientation.Vertical) { LineWidth = 2.5 }.StrokeUnder(Theme.MatplotlibV2).Thickness);
    }

    /// <summary>A span's border is drawn in its edge colour, else in its fill colour, else in the first colour of the theme's cycle.</summary>
    [Fact]
    public void ASpansBorder_IsItsEdgeColour_ElseItsFill_ElseTheFirstColourOfTheCycle()
    {
        var plain = new SpanRegion(1, 2, Orientation.Horizontal) { LineStyle = LineStyle.Dashed, LineWidth = 2 };
        var filled = new SpanRegion(1, 2, Orientation.Horizontal) { Color = Colors.Green };
        var edged = new SpanRegion(1, 2, Orientation.Horizontal) { Color = Colors.Green, EdgeColor = Red };

        Assert.Equal(new StrokeStyle(Theme.Nord.FirstCycleColor, 2, LineStyle.Dashed), plain.StrokeUnder(Theme.Nord));
        Assert.Equal(Colors.Green, filled.StrokeUnder(Theme.Nord).Color);
        Assert.Equal(Red, edged.StrokeUnder(Theme.Nord).Color);
    }
}
