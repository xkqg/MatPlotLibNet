// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The records of the figure itself (the figure, an axes, an axis, the spines, a grid specification, a grid position,
/// an inset's bounds, a Y axis slot and the slots past slot 0 of a side) each write themselves into the figure JSON and read
/// themselves back: <c>Of</c> the model, and <c>To…</c>, <c>ApplyTo</c>, <c>RestoreOnto</c> or <c>AddTo</c> the model. The
/// serializer only reads and writes the document. These tests read both directions at the record; the whole-figure round
/// trips and the golden corpus hold the bytes.</summary>
public sealed class FigureRecordDtoTests
{
    // ── axis ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnAxis_IsWrittenWithItsSettings_AndItsColoursInTheFieldsThatCarryThem()
    {
        var axis = new Axis { Label = "t", Min = 1, Max = 9, Scale = AxisScale.Log, Inverted = true, Margin = 0.1, SymLogLinThresh = 2 };
        axis.MajorTicks = axis.MajorTicks with { Color = Colors.Red, LabelColor = Colors.Blue };
        axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 14 };

        var dto = AxisDto.Of(axis);

        Assert.Equal("t", dto.Label);
        Assert.Equal(1, dto.Min);
        Assert.Equal(9, dto.Max);
        Assert.Equal("log", dto.Scale);
        Assert.True(dto.Inverted);
        Assert.Equal(0.1, dto.Margin);
        Assert.Equal(2, dto.SymLogLinThresh);
        Assert.Equal(Colors.Red, dto.TickColor);
        Assert.Equal(Colors.Blue, dto.TickLabelColor);
        Assert.Equal(Colors.Green, dto.LabelColor);
        Assert.NotNull(dto.LabelStyle);
        Assert.Null(dto.LabelStyle!.Color);
        Assert.Null(dto.MajorTicks);
        Assert.Null(dto.MinorTicks);
    }

    [Fact]
    public void ANewAxis_LeavesEverySettingOut()
    {
        var dto = AxisDto.Of(new Axis());

        Assert.Null(dto.Inverted);
        Assert.Null(dto.SymLogLinThresh);
        Assert.Null(dto.TickLocator);
        Assert.Null(dto.TickFormatter);
        Assert.Null(dto.Link);
        Assert.Null(dto.LabelStyle);
        Assert.Null(dto.LabelColor);
    }

    [Fact]
    public void AnAxis_IsReadBackWithWhatItWrote()
    {
        var written = new Axis { Label = "t", Min = 1, Max = 9, Scale = AxisScale.SymLog, Inverted = true, Margin = 0.2, SymLogLinThresh = 3 };
        written.MajorTicks = written.MajorTicks with { Color = Colors.Red, LabelColor = Colors.Blue, Length = 7 };
        written.MinorTicks = written.MinorTicks with { Visible = true };
        written.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 14 };
        var read = new Axis();

        AxisDto.Of(written).ApplyTo(read);

        Assert.Equal("t", read.Label);
        Assert.Equal(1, read.Min);
        Assert.Equal(9, read.Max);
        Assert.Equal(AxisScale.SymLog, read.Scale);
        Assert.True(read.Inverted);
        Assert.Equal(0.2, read.Margin);
        Assert.Equal(3, read.SymLogLinThresh);
        Assert.Equal(written.MajorTicks, read.MajorTicks);
        Assert.Equal(written.MinorTicks, read.MinorTicks);
        Assert.Equal(written.LabelStyle, read.LabelStyle);
    }

    [Fact]
    public void AnAxisLabelColour_WithoutAStyle_IsReadAsAStyleOfThatColour()
    {
        var read = new Axis();

        new AxisDto { LabelColor = Colors.Green }.ApplyTo(read);

        Assert.Equal(Colors.Green, read.LabelStyle!.Color);
    }

    [Fact]
    public void AnAxisScale_ThatNamesNoScale_LeavesTheAxisScale()
    {
        var read = new Axis { Scale = AxisScale.Log };

        new AxisDto { Scale = "spiral" }.ApplyTo(read);

        Assert.Equal(AxisScale.Log, read.Scale);
    }

    // ── spines ───────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(SpinePosition.Edge, null)]
    [InlineData(SpinePosition.Data, "data")]
    [InlineData(SpinePosition.Axes, "axes")]
    public void ASpine_IsWrittenAndReadBackAtItsPosition(SpinePosition position, string? written)
    {
        var spine = new SpineConfig { Visible = false, Position = position, PositionValue = 0.5, LineWidth = 2, Color = Colors.Red, LineStyle = LineStyle.Dashed, BoundsFrom = 1, BoundsTo = 4 };

        var dto = SpineConfigDto.Of(spine);

        Assert.Equal(written, dto.Position);
        Assert.Equal(LineStyle.Dashed, dto.LineStyle);
        Assert.Equal(spine, dto.ToSpine());
    }

    [Fact]
    public void ASolidSpine_LeavesItsLineStyleOut_AndReadsBackSolid()
    {
        var dto = SpineConfigDto.Of(new SpineConfig());

        Assert.Null(dto.LineStyle);
        Assert.Equal(LineStyle.Solid, dto.ToSpine().LineStyle);
    }

    [Fact]
    public void TheSpines_AreWrittenAndReadBackSideBySide_AndAMissingSideReadsAsANewSpine()
    {
        var spines = new SpinesConfig { Top = new SpineConfig { Visible = false }, Right = new SpineConfig { LineWidth = 3 } };

        Assert.Equal(spines, SpinesConfigDto.Of(spines).ToSpines());
        Assert.Equal(new SpinesConfig(), new SpinesConfigDto().ToSpines());
    }

    // ── grid specification, grid position, inset bounds ───────────────────────────────────────────

    [Fact]
    public void AGridSpecification_IsWrittenAndReadBack()
    {
        var spec = new GridSpec { Rows = 2, Cols = 3, HeightRatios = [1, 2], WidthRatios = [1, 1, 2] };

        var read = GridSpecDto.Of(spec).ToGridSpec();

        Assert.Equal(2, read.Rows);
        Assert.Equal(3, read.Cols);
        Assert.Equal(spec.HeightRatios, read.HeightRatios);
        Assert.Equal(spec.WidthRatios, read.WidthRatios);
    }

    [Fact]
    public void AGridPosition_IsWrittenAndReadBack()
    {
        var position = new GridPosition(0, 2, 1, 3);

        Assert.Equal(position, GridPositionDto.Of(position).ToGridPosition());
    }

    [Fact]
    public void AnInsetsBounds_AreWritten_AndAddTheInsetThereWhenRead()
    {
        var parent = new Axes();
        var dto = InsetBoundsDto.Of(new InsetBounds(0.1, 0.2, 0.3, 0.4));

        var inset = dto.AddInsetTo(parent);

        Assert.Same(inset, Assert.Single(parent.Insets));
        Assert.Equal(new InsetBounds(0.1, 0.2, 0.3, 0.4), inset.InsetBounds);
    }

    // ── Y axis slots ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(3, 1, 2)]
    public void TheSlotsPastSlot0_PutTheFirstInTheFrozenListAndTheRestInTheOverflow(int pastSlot0, int frozen, int overflow)
    {
        var axes = new Axes();
        var stack = axes.YAxes.Right;
        stack.FirstOrAdd();
        for (int i = 0; i < pastSlot0; i++) stack.Add().Axis.Label = $"slot {i + 1}";

        var dto = YAxisSlotsPastSlot0Dto.Of(stack);

        Assert.Equal(frozen, dto.Frozen?.Count ?? 0);
        Assert.Equal(overflow, dto.Overflow?.Count ?? 0);
        Assert.Equal(Enumerable.Range(1, pastSlot0).Select(i => $"slot {i}"), dto.InOutwardOrder.Select(slot => slot.Axis!.Label));
    }

    [Fact]
    public void TheSlotsPastSlot0_AreReadBackOutward_PastASlot0ThatIsMadeFirst()
    {
        var written = new Axes();
        written.YAxes.Left.Add().Axis.Label = "left 1";
        written.YAxes.Left.Add().Axis.Label = "left 2";
        var read = new Axes();

        YAxisSlotsPastSlot0Dto.Of(written.YAxes.Left).AddTo(read, read.YAxes.Left);

        Assert.Equal(["left 1", "left 2"], read.YAxes.Left.Skip(1).Select(slot => slot.Axis.Label));
    }

    [Fact]
    public void ASlot_IsWrittenWithItsAxisAndItsSeries_AndReadBackOntoTheSlot()
    {
        var written = new Axes();
        var slot = written.YAxes.Right.Add();
        slot.Axis.Label = "rate";
        slot.AddSeries(new LineSeries([1.0, 2], [3.0, 4]) { Label = "r" });
        var read = new Axes();
        var target = read.YAxes.Right.Add();

        var dto = SecondaryAxisSlotDto.Of(slot);
        dto.RestoreOnto(read, target);

        Assert.Equal("rate", target.Axis.Label);
        Assert.Equal("r", Assert.Single(target.Series).Label);
        Assert.Empty(read.Series);
    }

    [Fact]
    public void ASlotWithoutSeries_LeavesTheListOut_AndASlotWithoutAnAxisReadsNone()
    {
        var empty = new Axes().YAxes.Right.Add();
        var read = new Axes();
        var target = read.YAxes.Right.Add();

        Assert.Null(SecondaryAxisSlotDto.Of(empty).Series);
        new SecondaryAxisSlotDto().RestoreOnto(read, target);

        Assert.Null(target.Axis.Label);
        Assert.Empty(target.Series);
    }

    /// <summary>An X axis below the plot is written with its axis and its location and nothing else: it holds no series, and its
    /// record has no field for one. Read back, a location that is not finite is left out, as the builder refuses one.</summary>
    [Fact]
    public void AnXAxisBelowThePlot_IsWrittenWithItsAxisAndLocation_AndReadBackOntoTheSlot()
    {
        var written = new Axes().YAxes.Bottom.Add();
        written.Axis.Label = "°F";
        written.Location = -0.2;
        var read = new Axes();
        var target = read.YAxes.Bottom.Add();

        var dto = XAxisSlotDto.Of(written);
        dto.RestoreOnto(target);

        Assert.Equal("°F", target.Axis.Label);
        Assert.Equal(-0.2, target.Location);
        Assert.DoesNotContain(typeof(XAxisSlotDto).GetProperties(), property => property.Name == "Series");
        Assert.DoesNotContain(typeof(SecondaryAxisSlotDto).GetProperties(), property => property.Name == "Location");
    }

    [Fact]
    public void AnXAxisBelowThePlotWithoutAnAxisOrAFiniteLocation_ReadsNeither()
    {
        var stacked = new Axes().YAxes.Bottom.Add();
        var target = new Axes().YAxes.Bottom.Add();

        Assert.Null(XAxisSlotDto.Of(stacked).Location);
        XAxisSlotDto.Of(stacked).RestoreOnto(target);
        new XAxisSlotDto { Location = double.NaN }.RestoreOnto(target);

        Assert.Null(target.Axis.Label);
        Assert.Null(target.Location);
    }

    // ── the fields every series shares ───────────────────────────────────────────────────────────

    [Fact]
    public void TheSharedFieldsOfASeries_AreWrittenBesideWhatItsTypeWrites()
    {
        var line = new LineSeries([1.0, 2], [3.0, 4])
        {
            Label = "a", LabelPlacement = SeriesLabelPlacement.LineEnd, ZOrder = 5, Visible = false,
            MaxDisplayPoints = 100, RowKeys = ["r1", "r2"], ColorGroup = 3, WearsNeutral = true,
        };

        var dto = line.ToDtoWithSharedFields();

        Assert.Equal("line", dto.Type);
        Assert.Equal("a", dto.Label);
        Assert.Equal("lineend", dto.LabelPlacement);
        Assert.Equal(5, dto.ZOrder);
        Assert.False(dto.Visible);
        Assert.Equal(100, dto.MaxDisplayPoints);
        Assert.Equal(["r1", "r2"], dto.RowKeys!);
        Assert.Equal(3, dto.ColorGroup);
        Assert.True(dto.WearsNeutral);
    }

    [Fact]
    public void ASeriesAtRest_LeavesItsSharedFieldsOut()
    {
        var dto = new LineSeries([1.0, 2], [3.0, 4]).ToDtoWithSharedFields();

        Assert.Null(dto.LabelPlacement);
        Assert.Null(dto.ZOrder);
        Assert.Null(dto.Visible);
        Assert.Null(dto.ColorGroup);
        Assert.Null(dto.WearsNeutral);
    }

    [Fact]
    public void TheSharedFieldsOfASeries_AreReadBack_AndAnAbsentFieldLeavesTheSeriesValue()
    {
        var read = new LineSeries([1.0, 2], [3.0, 4]) { ZOrder = 7 };

        read.RestoreSharedFields(new SeriesDto
        {
            Type = "line", Label = "b", LabelPlacement = "lineend", Visible = false, MaxDisplayPoints = 50,
            RowKeys = ["k1", "k2"], ColorGroup = 2, WearsNeutral = true,
        });

        Assert.Equal("b", read.Label);
        Assert.Equal(SeriesLabelPlacement.LineEnd, read.LabelPlacement);
        Assert.Equal(7, read.ZOrder);
        Assert.False(read.Visible);
        Assert.Equal(50, read.MaxDisplayPoints);
        Assert.Equal(["k1", "k2"], read.RowKeys!);
        Assert.Equal(2, read.ColorGroup);
        Assert.True(read.WearsNeutral);
    }

    // ── axes and figure ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnAxes_IsReadBackOntoTheAxesItIsGiven_WithItsSeriesAndItsSecondaryAxes()
    {
        var written = new Axes { Title = "t" };
        written.Plot([1.0, 2], [3.0, 4]);
        written.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([1.0, 2], [5.0, 6]));
        written.TwinY();
        var read = new Axes();

        AxesDto.Of(written, Theme.Default).RestoreOnto(read);

        Assert.Equal("t", read.Title);
        Assert.Single(read.Series);
        Assert.Single(read.YAxes.Right[0].Series);
        Assert.NotNull(read.SecondaryXAxis);
    }

    [Fact]
    public void AFigure_IsWrittenAndReadBack_WithItsSubplotsSharedByKey()
    {
        var figure = new Figure { Title = "f" };
        var a = figure.AddSubPlot();
        a.Key = "a";
        var b = figure.AddSubPlot();
        b.ShareXWith = a;
        b.ShareYWith = a;

        var read = FigureDto.Of(figure).ToFigure();

        Assert.Equal("f", read.Title);
        Assert.Equal(2, read.SubPlots.Count);
        Assert.Same(read.SubPlots[0], read.SubPlots[1].ShareXWith);
        Assert.Same(read.SubPlots[0], read.SubPlots[1].ShareYWith);
    }
}
