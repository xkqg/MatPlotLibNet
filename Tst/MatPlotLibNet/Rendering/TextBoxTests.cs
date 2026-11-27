// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// The one rule for the box a drawn text occupies: its width from the alignment, its ascent above the baseline and its
/// descent below, both read from the text's extent, the whole box turned counter-clockwise about the
/// point the text is drawn at.
/// </summary>
public class TextBoxTests
{
    private const double Tolerance = 1e-9;
    private static readonly Point Anchor = new(100, 50);
    private static readonly TextExtent Extent = new(40, 12, 10);

    private static TextBox Box(TextAlignment alignment = TextAlignment.Left,
        TextVerticalAlignment vertical = TextVerticalAlignment.Baseline, double rotation = 0, Point? anchor = null) =>
        new(new TextPlacement(anchor ?? Anchor, alignment) { Vertical = vertical, Rotation = rotation }, Extent);

    private static void AssertPoint(double x, double y, Point actual)
    {
        Assert.Equal(x, actual.X, Tolerance);
        Assert.Equal(y, actual.Y, Tolerance);
    }

    [Fact]
    public void Upright_OnTheBaseline_StandsFromTheFontSizeAboveItToTheDescentBelow()
    {
        var corners = Box().Corners();
        AssertPoint(100, 40, corners.TopLeft);
        AssertPoint(140, 40, corners.TopRight);
        AssertPoint(140, 52, corners.BottomRight);
        AssertPoint(100, 52, corners.BottomLeft);
    }

    [Theory]
    [InlineData(TextAlignment.Left, 100)]
    [InlineData(TextAlignment.Center, 80)]
    [InlineData(TextAlignment.Right, 60)]
    public void TheAlignment_DecidesWhereTheBoxStartsAlongTheBaseline(TextAlignment alignment, double left)
    {
        var corners = Box(alignment).Corners();
        AssertPoint(left, 40, corners.TopLeft);
        AssertPoint(left + 40, 40, corners.TopRight);
    }

    [Theory]
    [InlineData(TextVerticalAlignment.Baseline, 50.0, 40.0, 52.0)]
    [InlineData(TextVerticalAlignment.Center, 53.5, 43.5, 55.5)]   // the baseline stands 0.35 of the size below the anchor
    [InlineData(TextVerticalAlignment.Top, 60.0, 50.0, 62.0)]      // the box hangs from the anchor
    [InlineData(TextVerticalAlignment.Bottom, 48.0, 38.0, 50.0)]   // the box stands on the anchor
    public void TheVerticalAlignment_DecidesWhereTheBaselineStandsFromTheAnchor(
        TextVerticalAlignment vertical, double baselineY, double top, double bottom)
    {
        var box = Box(vertical: vertical);
        AssertPoint(100, baselineY, box.Baseline);
        Assert.Equal(top, box.Corners().TopLeft.Y, Tolerance);
        Assert.Equal(bottom, box.Corners().BottomLeft.Y, Tolerance);
    }

    [Fact]
    public void TheAscent_IsTheExtents()
    {
        var corners = new TextBox(new TextPlacement(Anchor, TextAlignment.Left) { Vertical = TextVerticalAlignment.Top }, new TextExtent(40, 12, 8)).Corners();
        AssertPoint(100, 50, corners.TopLeft);
        AssertPoint(100, 62, corners.BottomLeft);
    }

    /// <summary>A text turned and aligned in its own frame (matplotlib's <c>rotation_mode='anchor'</c>, the label of a contour line) has its
    /// baseline moved along the text's own down direction, turned with it, where an upright alignment moves it down the picture.</summary>
    [Theory]
    [InlineData(TextVerticalAlignment.Center, 0, 100.0, 53.5)]
    [InlineData(TextVerticalAlignment.Center, 90, 103.5, 50.0)]
    [InlineData(TextVerticalAlignment.Top, 90, 110.0, 50.0)]
    [InlineData(TextVerticalAlignment.Baseline, 90, 100.0, 50.0)]
    public void AlignedInItsOwnFrame_TheBaselineMovesAlongTheTurnedDown(TextVerticalAlignment vertical, double rotation, double x, double y)
    {
        AssertPoint(x, y, Box(vertical: vertical, rotation: rotation).AnchoredBaseline);
    }

    [Fact]
    public void AlignedInItsOwnFrame_TheBaselineIsTheUprightOne_WhenTheTextIsUpright()
    {
        foreach (var vertical in Enum.GetValues<TextVerticalAlignment>())
            Assert.Equal(Box(vertical: vertical).Baseline, Box(vertical: vertical).AnchoredBaseline);
    }

    [Fact]
    public void TurnedAQuarter_RunsTheTextUpwardFromItsBaselinePoint()
    {
        var corners = Box(anchor: new Point(0, 0), rotation: 90).Corners();
        AssertPoint(-10, 0, corners.TopLeft);
        AssertPoint(-10, -40, corners.TopRight);
        AssertPoint(2, -40, corners.BottomRight);
        AssertPoint(2, 0, corners.BottomLeft);
    }

    [Theory]
    [InlineData(30, 29.641016151377546, -28.660254037844386)]
    [InlineData(-30, 39.641016151377546, 11.339745962155614)]
    public void TurnedByAnAngle_TheFarEndOfTheTopEdgeFollowsTheTurn(double degrees, double x, double y)
    {
        var corners = Box(anchor: new Point(0, 0), rotation: degrees).Corners();
        AssertPoint(x, y, corners.TopRight);
    }

    [Theory]
    [InlineData(0, 100, 40, 40, 12)]
    [InlineData(90, 100 - 10, 50 - 40, 12, 40)]
    public void TheBounds_AreTheRectOfTheTurnedCorners(double degrees, double x, double y, double width, double height)
    {
        var bounds = Box(rotation: degrees).Bounds();
        Assert.Equal(x, bounds.X, Tolerance);
        Assert.Equal(y, bounds.Y, Tolerance);
        Assert.Equal(width, bounds.Width, Tolerance);
        Assert.Equal(height, bounds.Height, Tolerance);
    }
}
