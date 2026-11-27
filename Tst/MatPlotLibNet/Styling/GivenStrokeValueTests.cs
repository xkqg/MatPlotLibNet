// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The width and the dash pattern a caller gave a series' line are one value (<see cref="GivenStroke"/>): each part is
/// <see langword="null"/> while it was not given, the series reports its kind's resting width and the solid line in its place, the slot
/// builds the stroke a renderer and a legend swatch draw from it in one place (<see cref="SeriesSlot.StrokeOf(Color, GivenStroke,
/// LineWidthRule)"/>, whose branches <c>SeriesSlotTests</c> holds), and the JSON writes and reads it in one place.</summary>
public class GivenStrokeValueTests
{
    [Fact]
    public void AStrokeGivenNothing_ReportsNoWidth_AndTheSolidLine()
    {
        var none = default(GivenStroke);

        Assert.Null(none.Width);
        Assert.Null(none.Style);
        Assert.Equal(LineStyle.Solid, GivenStroke.RestingStyle);
        Assert.Equal(GivenStroke.RestingStyle, none.StyleOrResting);
    }

    [Fact]
    public void AStrokeGivenAWidthAndADash_ReportsThemAsGiven_TheRestingValuesIncluded()
    {
        var given = new GivenStroke(1.5, LineStyle.Solid);

        Assert.Equal(1.5, given.Width);
        Assert.Equal(LineStyle.Solid, given.StyleOrResting);
        Assert.NotEqual(default, given);
    }

    /// <summary>A stroke laid over another keeps every part it was given and takes the other's part where it was given none: an
    /// indicator hands its lines what it was given, and a JSON key that is absent leaves what the series was created with.</summary>
    [Fact]
    public void AStrokeOverAnother_KeepsWhatItWasGiven_AndTakesTheOthersElsewhere()
    {
        var held = new GivenStroke(3, LineStyle.Dotted);

        Assert.Equal(held, default(GivenStroke).Over(held));
        Assert.Equal(new GivenStroke(1, LineStyle.Dotted), new GivenStroke(1, null).Over(held));
        Assert.Equal(new GivenStroke(3, LineStyle.Dashed), new GivenStroke(null, LineStyle.Dashed).Over(held));
        Assert.Equal(new GivenStroke(1, LineStyle.Dashed), new GivenStroke(1, LineStyle.Dashed).Over(held));
    }

    /// <summary>One part given and the other not: the slot keeps the given part and takes its own for the other.</summary>
    [Fact]
    public void TheSlot_KeepsTheGivenPart_AndTakesItsOwnForTheOther()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithLineStyles(LineStyle.Dashed).WithLineWidths(4.0).Build();
        var slot = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build().SeriesSlot(0);

        Assert.Equal(new StrokeStyle(Colors.Blue, 4.0, LineStyle.Dotted), slot.StrokeOf(Colors.Blue, new GivenStroke(null, LineStyle.Dotted), LineWidthRule.Line));
        Assert.Equal(new StrokeStyle(Colors.Blue, 1.0, LineStyle.Dashed), slot.StrokeOf(Colors.Blue, new GivenStroke(1.0, null), LineWidthRule.Line));
    }

    /// <summary>A series hands its given stroke to its wire form as one value: the width as <c>lineWidth</c>, the dash by its lower-case
    /// name as <c>lineStyle</c>, each only while it was given; the one writer every kind with a given stroke goes through.</summary>
    [Fact]
    public void TheJson_WritesTheGivenWidthAndTheDashByItsLowerCaseName_AndNothingNotGiven()
    {
        var both = new SeriesDto { Stroking = new GivenStroke(2.5, LineStyle.DashDot) };
        var none = new SeriesDto { Stroking = default };

        Assert.Equal(2.5, both.LineWidth);
        Assert.Equal("dashdot", both.LineStyle);
        Assert.Null(none.LineWidth);
        Assert.Null(none.LineStyle);
        Assert.Equal(new GivenStroke(2.5, LineStyle.DashDot), both.StrokeOver(default));
    }

    /// <summary>The JSON's stroke is laid over the one the series holds: an absent key keeps it, a given key wins, and a dash name that
    /// is no <see cref="LineStyle"/> keeps it too (and is announced, as every unknown name the reader meets).</summary>
    [Fact]
    public void TheJsonsStroke_IsLaidOverTheOneTheSeriesHolds()
    {
        var held = new GivenStroke(3, LineStyle.Dotted);

        Assert.Equal(held, new SeriesDto().StrokeOver(held));
        Assert.Equal(new GivenStroke(1.5, LineStyle.Solid), new SeriesDto { LineWidth = 1.5, LineStyle = "solid" }.StrokeOver(held));
        Assert.Equal(new GivenStroke(3, LineStyle.Dashed), new SeriesDto { LineStyle = "DASHED" }.StrokeOver(held));
        Assert.Equal(new GivenStroke(2, LineStyle.Dotted), new SeriesDto { LineWidth = 2, LineStyle = "wavy" }.StrokeOver(held));
        Assert.Equal(default, new SeriesDto().StrokeOver(default));
    }
}
