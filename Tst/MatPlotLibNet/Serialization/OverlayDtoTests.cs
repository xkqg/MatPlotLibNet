// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Each overlay of an axes (a reference line, a span, a trendline, a horizontal level, a Fibonacci retracement, a 3-D
/// pane, an annotation, an axis break) writes itself into the figure JSON and reads itself back through its own record:
/// <c>From</c> (or <c>Of</c>) the model, and <c>AddTo</c> the axes. The serializer holds no mapping of its own for them, so what
/// is written and what is read cannot drift apart in two places. These tests read both directions at the record, and the
/// whole-figure round trips elsewhere hold the bytes.</summary>
public sealed class OverlayDtoTests
{
    // ── reference line ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AReferenceLine_IsWrittenWithItsOrientationStyleWidthLabelAndColour()
    {
        var line = new Axes().AxVLine(4.5);
        line.LineStyle = LineStyle.Dotted;
        line.LineWidth = 2.5;
        line.Label = "limit";
        line.Color = Colors.Red;

        var dto = ReferenceLineDto.From(line);

        Assert.Equal(4.5, dto.Value);
        Assert.Equal("vertical", dto.Orientation);
        Assert.Equal("dotted", dto.LineStyle);
        Assert.Equal(2.5, dto.LineWidth);
        Assert.Equal("limit", dto.Label);
        Assert.Equal(Colors.Red, dto.Color);
    }

    [Theory]
    [InlineData("horizontal")]
    [InlineData("vertical")]
    public void AReferenceLine_IsReadBackOnTheAxisItWasWrittenFor(string orientation)
    {
        var dto = new ReferenceLineDto { Value = 3, Orientation = orientation, LineStyle = "solid", LineWidth = 1.5, Label = "x", Color = Colors.Blue };
        var axes = new Axes();

        dto.AddTo(axes);

        var line = Assert.Single(axes.ReferenceLines);
        Assert.Equal(orientation == "vertical" ? Orientation.Vertical : Orientation.Horizontal, line.Orientation);
        Assert.Equal(3, line.Value);
        Assert.Equal(LineStyle.Solid, line.LineStyle);
        Assert.Equal(1.5, line.LineWidth);
        Assert.Equal("x", line.Label);
        Assert.Equal(Colors.Blue, line.Color);
    }

    [Fact]
    public void AReferenceLineWithoutAStyleName_KeepsTheStyleANewLineHas()
    {
        var axes = new Axes();

        new ReferenceLineDto { Value = 1, Orientation = null, LineStyle = null, LineWidth = 1 }.AddTo(axes);

        var line = Assert.Single(axes.ReferenceLines);
        Assert.Equal(Orientation.Horizontal, line.Orientation);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    // ── span ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASpanAtItsDefaults_LeavesOutItsLineStyleAndWidth()
    {
        var dto = SpanRegionDto.From(new Axes().AxHSpan(1, 2));

        Assert.Equal(1, dto.Min);
        Assert.Equal(2, dto.Max);
        Assert.Equal("horizontal", dto.Orientation);
        Assert.Equal(0.2, dto.Alpha);
        Assert.Null(dto.LineStyle);
        Assert.Null(dto.LineWidth);
        Assert.Null(dto.Label);
        Assert.Null(dto.Color);
        Assert.Null(dto.EdgeColor);
    }

    [Fact]
    public void ASpanWithAnOutline_WritesTheLineStyleWidthAndColours()
    {
        var span = new Axes().AxVSpan(0, 5);
        span.LineStyle = LineStyle.Dashed;
        span.LineWidth = 3;
        span.Label = "zone";
        span.Color = Colors.Green;
        span.EdgeColor = Colors.Black;
        span.Alpha = 0.4;

        var dto = SpanRegionDto.From(span);

        Assert.Equal("vertical", dto.Orientation);
        Assert.Equal("dashed", dto.LineStyle);
        Assert.Equal(3.0, dto.LineWidth);
        Assert.Equal("zone", dto.Label);
        Assert.Equal(Colors.Green, dto.Color);
        Assert.Equal(Colors.Black, dto.EdgeColor);
        Assert.Equal(0.4, dto.Alpha);
    }

    [Theory]
    [InlineData("vertical", true)]
    [InlineData("horizontal", false)]
    [InlineData(null, false)]
    public void ASpan_IsReadBackWithEveryFieldItCarried(string? orientation, bool vertical)
    {
        var dto = new SpanRegionDto
        {
            Min = 1, Max = 2, Orientation = orientation, Alpha = 0.5, LineStyle = "dotted", LineWidth = 2.0,
            Label = "z", Color = Colors.Red, EdgeColor = Colors.Blue,
        };
        var axes = new Axes();

        dto.AddTo(axes);

        var span = Assert.Single(axes.Spans);
        Assert.Equal(vertical ? Orientation.Vertical : Orientation.Horizontal, span.Orientation);
        Assert.Equal((1.0, 2.0), (span.Min, span.Max));
        Assert.Equal(0.5, span.Alpha);
        Assert.Equal(LineStyle.Dotted, span.LineStyle);
        Assert.Equal(2.0, span.LineWidth);
        Assert.Equal("z", span.Label);
        Assert.Equal(Colors.Red, span.Color);
        Assert.Equal(Colors.Blue, span.EdgeColor);
    }

    [Fact]
    public void ASpanWithoutLineStyleOrWidth_KeepsTheOnesANewSpanHas()
    {
        var axes = new Axes();

        new SpanRegionDto { Min = 0, Max = 1, Alpha = 0.2 }.AddTo(axes);

        var span = Assert.Single(axes.Spans);
        Assert.Equal(LineStyle.None, span.LineStyle);
        Assert.Equal(1.0, span.LineWidth);
    }

    // ── trendline ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ATrendlineAtItsDefaults_LeavesOutItsColourAndStyle()
    {
        var dto = TrendlineDto.From(new Axes().AddTrendline(0, 1, 2, 3));

        Assert.Equal((0.0, 1.0, 2.0, 3.0), (dto.X1, dto.Y1, dto.X2, dto.Y2));
        Assert.Null(dto.Color);
        Assert.Null(dto.LineStyle);
        Assert.Equal(1.0, dto.LineWidth);
        Assert.False(dto.IsExtended);
    }

    [Fact]
    public void ATrendlineWithAColourAndStyle_WritesThemAsHexAndName()
    {
        var line = new Axes().AddTrendline(0, 1, 2, 3);
        line.Color = Colors.Red;
        line.LineStyle = LineStyle.Dashed;
        line.LineWidth = 2;
        line.Label = "fit";
        line.IsExtended = true;

        var dto = TrendlineDto.From(line);

        Assert.Equal(Colors.Red.ToHex(), dto.Color);
        Assert.Equal("dashed", dto.LineStyle);
        Assert.Equal(2.0, dto.LineWidth);
        Assert.Equal("fit", dto.Label);
        Assert.True(dto.IsExtended);
    }

    [Fact]
    public void ATrendline_IsReadBackWithEveryFieldItCarried()
    {
        var dto = new TrendlineDto
        {
            X1 = 0, Y1 = 1, X2 = 2, Y2 = 3, Color = Colors.Blue.ToHex(), LineStyle = "dotted", LineWidth = 4, Label = "t", IsExtended = true,
        };
        var axes = new Axes();

        dto.AddTo(axes);

        var line = Assert.Single(axes.Trendlines);
        Assert.Equal((0.0, 1.0, 2.0, 3.0), (line.X1, line.Y1, line.X2, line.Y2));
        Assert.Equal(Colors.Blue, line.Color);
        Assert.Equal(LineStyle.Dotted, line.LineStyle);
        Assert.Equal(4.0, line.LineWidth);
        Assert.Equal("t", line.Label);
        Assert.True(line.IsExtended);
    }

    [Fact]
    public void ATrendlineWithoutColourOrStyle_KeepsTheOnesANewTrendlineHas()
    {
        var axes = new Axes();

        new TrendlineDto { X1 = 0, Y1 = 0, X2 = 1, Y2 = 1 }.AddTo(axes);

        var line = Assert.Single(axes.Trendlines);
        Assert.Null(line.Color);
        Assert.Equal(LineStyle.Solid, line.LineStyle);
    }

    // ── horizontal level ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALevelAtItsDefaults_LeavesOutItsColourAndStyle()
    {
        var dto = HorizontalLevelDto.From(new Axes().AddLevel(7));

        Assert.Equal(7, dto.Value);
        Assert.Null(dto.Color);
        Assert.Null(dto.LineStyle);
        Assert.Equal(1.0, dto.LineWidth);
    }

    [Fact]
    public void ALevelWithAColourAndStyle_WritesThem()
    {
        var level = new Axes().AddLevel(7);
        level.Color = Colors.Green;
        level.LineStyle = LineStyle.Solid;
        level.LineWidth = 2;
        level.Label = "support";

        var dto = HorizontalLevelDto.From(level);

        Assert.Equal(Colors.Green.ToHex(), dto.Color);
        Assert.Equal("solid", dto.LineStyle);
        Assert.Equal(2.0, dto.LineWidth);
        Assert.Equal("support", dto.Label);
    }

    [Fact]
    public void ALevel_IsReadBackWithEveryFieldItCarried()
    {
        var axes = new Axes();

        new HorizontalLevelDto { Value = 9, Color = Colors.Red.ToHex(), LineStyle = "dotted", LineWidth = 3, Label = "l" }.AddTo(axes);

        var level = Assert.Single(axes.HorizontalLevels);
        Assert.Equal(9, level.Value);
        Assert.Equal(Colors.Red, level.Color);
        Assert.Equal(LineStyle.Dotted, level.LineStyle);
        Assert.Equal(3.0, level.LineWidth);
        Assert.Equal("l", level.Label);
    }

    [Fact]
    public void ALevelWithoutColourOrStyle_KeepsTheOnesANewLevelHas()
    {
        var axes = new Axes();

        new HorizontalLevelDto { Value = 1 }.AddTo(axes);

        var level = Assert.Single(axes.HorizontalLevels);
        Assert.Null(level.Color);
        Assert.Equal(LineStyle.Dashed, level.LineStyle);
    }

    // ── Fibonacci retracement ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARetracement_IsWrittenWithItsPricesWidthAndLabelsFlag_AndAColourOnlyWhenSet()
    {
        var plain = FibonacciRetracementDto.From(new Axes().AddFibonacci(100, 50));
        var fib = new Axes().AddFibonacci(100, 50);
        fib.Color = Colors.Blue;
        fib.LineWidth = 2;
        fib.ShowLabels = false;
        var coloured = FibonacciRetracementDto.From(fib);

        Assert.Equal((100.0, 50.0), (plain.PriceHigh, plain.PriceLow));
        Assert.Null(plain.Color);
        Assert.Equal(Colors.Blue.ToHex(), coloured.Color);
        Assert.Equal(2.0, coloured.LineWidth);
        Assert.False(coloured.ShowLabels);
    }

    [Fact]
    public void ARetracement_IsReadBackWithEveryFieldItCarried()
    {
        var axes = new Axes();

        new FibonacciRetracementDto { PriceHigh = 10, PriceLow = 2, Color = Colors.Red.ToHex(), LineWidth = 3, ShowLabels = false }.AddTo(axes);
        new FibonacciRetracementDto { PriceHigh = 5, PriceLow = 1 }.AddTo(axes);

        var first = axes.FibonacciRetracements[0];
        Assert.Equal((10.0, 2.0), (first.PriceHigh, first.PriceLow));
        Assert.Equal(Colors.Red, first.Color);
        Assert.Equal(3.0, first.LineWidth);
        Assert.False(first.ShowLabels);
        var second = axes.FibonacciRetracements[1];
        Assert.Null(second.Color);
        Assert.True(second.ShowLabels);
    }

    // ── 3-D pane ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void APaneAtItsDefaults_IsNotWritten()
    {
        Assert.Null(Pane3DConfigDto.From(new Pane3DConfig()));
    }

    [Fact]
    public void AChangedPane_IsWrittenAndReadBack()
    {
        var pane = new Pane3DConfig { FloorColor = Colors.Red, LeftWallColor = Colors.Green, RightWallColor = Colors.Blue, Alpha = 0.5, Visible = false };

        var dto = Pane3DConfigDto.From(pane);
        var axes = new Axes();
        dto!.AddTo(axes);

        Assert.Equal(pane, axes.Pane3D);
    }

    // ── axis break ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ABreak_IsWrittenWithItsStyleAsAName()
    {
        var dto = AxisBreakDto.Of(new AxisBreak(2, 5, BreakStyle.Straight));

        Assert.Equal((2.0, 5.0, "straight"), (dto.From, dto.To, dto.Style));
    }

    [Theory]
    [InlineData("straight", BreakStyle.Straight)]
    [InlineData("STRAIGHT", BreakStyle.Straight)]
    [InlineData(null, BreakStyle.Zigzag)]
    [InlineData("no-such-style", BreakStyle.Zigzag)]
    public void ABreak_IsReadBackWithTheStyleItNamedOrTheDefault(string? style, BreakStyle expected)
    {
        var dto = new AxisBreakDto(1, 2, style);

        var read = dto.ToAxisBreak();

        Assert.Equal(new AxisBreak(1, 2, expected), read);
    }

    // ── annotation ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnAnnotationAtItsDefaults_WritesOnlyWhatItSays()
    {
        var dto = AnnotationDto.From(new Axes().Annotate("note", 1, 2));

        Assert.Equal("note", dto.Text);
        Assert.Equal((1.0, 2.0), (dto.X, dto.Y));
        Assert.Null(dto.ConnectionStyle);
        Assert.Null(dto.ConnectionRad);
        Assert.Null(dto.BoxStyle);
        Assert.Null(dto.Coordinates);
        Assert.Null(dto.Font);
        Assert.Null(dto.Alignment);
        Assert.Null(dto.Rotation);
        Assert.Null(dto.AvoidOverlap);
    }

    [Fact]
    public void AnAnnotationWithEverythingSet_IsReadBackWithEverything()
    {
        var note = new Axes().Annotate("note", 1, 2);
        note.ArrowTargetX = 3;
        note.ArrowTargetY = 4;
        note.ConnectionStyle = ConnectionStyle.Arc3;
        note.ConnectionRad = 0.4;
        note.ArrowHeadSize = 14;
        note.BoxStyle = BoxStyle.Round;
        note.BoxPadding = 6;
        note.BoxCornerRadius = 3;
        note.Coordinates = AnnotationCoordinates.AxesFraction;
        note.Font = new Font { Size = 20 };
        note.Color = Colors.Red;
        note.ArrowColor = Colors.Blue;
        note.Alignment = TextAlignment.Center;
        note.VerticalAlignment = TextVerticalAlignment.Top;
        note.OffsetX = 5;
        note.OffsetY = 6;
        note.AvoidOverlap = true;
        note.Rotation = 30;
        note.ArrowStyle = ArrowStyle.FancyArrow;
        note.BackgroundColor = Colors.Green;
        note.BoxFaceColor = Colors.Black;
        note.BoxEdgeColor = Colors.White;
        note.BoxLineWidth = 2;

        var dto = AnnotationDto.From(note);
        var axes = new Axes();
        dto.AddTo(axes);

        var read = Assert.Single(axes.Annotations);
        Assert.Equal(AnnotationDto.From(note), AnnotationDto.From(read));
        Assert.Equal("note", read.Text);
        Assert.Equal(ConnectionStyle.Arc3, read.ConnectionStyle);
        Assert.Equal(BoxStyle.Round, read.BoxStyle);
        Assert.Equal(AnnotationCoordinates.AxesFraction, read.Coordinates);
        Assert.Equal(20, read.Font!.Size);
        Assert.True(read.AvoidOverlap);
        Assert.Equal(30, read.Rotation);
    }

    [Fact]
    public void AnAnnotationWithoutText_IsReadBackWithAnEmptyOne()
    {
        var axes = new Axes();

        new AnnotationDto { Text = null, X = 1, Y = 2 }.AddTo(axes);

        Assert.Equal("", Assert.Single(axes.Annotations).Text);
    }
}
