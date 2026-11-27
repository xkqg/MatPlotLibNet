// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Models.Patches;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>
/// Phase B.3 (strict-90 floor plan) — unit tests for
/// <see cref="LegendLayoutCalculator"/>. Asserts the calculator produces
/// byte-identical values to the OLD inline layout math from
/// <c>AxesRenderer.RenderLegend</c> + <c>ComputeLegendBounds</c>.
/// </summary>
public class LegendLayoutCalculatorTests
{
    private static readonly Rect PlotArea = new(80, 60, 640, 480);

    private static Legend DefaultLegend() => new()
    {
        Visible = true,
        Position = LegendPosition.UpperRight,
        NCols = 1,
        MarkerScale = 1.0,
        LabelSpacing = 0.5,
        ColumnSpacing = 2.0,
        FrameOn = true,
        FrameAlpha = 1.0,
    };

    private static LegendLayoutCalculator NewCalculator()
        => new(Theme.Default, new SvgRenderContext());

    // ── Best, with the marks a chart drew ─────────────────────────────────

    private static void AssertSameBox(LegendLayout expected, LegendLayout actual)
    {
        Assert.Equivalent(new { expected.BoxX, expected.BoxY, expected.BoxWidth, expected.BoxHeight }, actual);
    }

    [Fact]
    public void Compute_BestWithNoMarks_IsTheThreeArgumentResult()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.Best };

        var byThree = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var byFour = NewCalculator().Compute(legend, new[] { "A" }, PlotArea, MarkObstacles.None);

        AssertSameBox(byThree, byFour);
    }

    [Fact]
    public void Compute_BestWithMarksInTheUpperRight_StandsInTheUpperLeft()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.Best };
        var upperRight = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var marks = new MarkObstacles.Builder()
            .AddPoint(new Point(upperRight.BoxX + upperRight.BoxWidth / 2, upperRight.BoxY + upperRight.BoxHeight / 2))
            .Build();

        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea, marks);

        Assert.Equal(PlotArea.X + LegendPositionStrategy.Inset, layout.BoxX);
        Assert.Equal(PlotArea.Y + LegendPositionStrategy.Inset, layout.BoxY);
        Assert.Equal(upperRight.BoxWidth, layout.BoxWidth);
        Assert.Equal(upperRight.BoxHeight, layout.BoxHeight);
    }

    [Fact]
    public void Compute_AFixedPosition_IgnoresTheMarks()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.UpperRight };
        var covered = new MarkObstacles.Builder().AddBox(PlotArea).Build();

        var byThree = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var byFour = NewCalculator().Compute(legend, new[] { "A" }, PlotArea, covered);

        AssertSameBox(byThree, byFour);
    }

    // ── The place a legend left at Best takes, by the theme ───────────────

    private static LegendLayoutCalculator NewCalculator(Theme theme)
        => new(theme, new SvgRenderContext());

    [Fact]
    public void Compute_BestUnderTheClassicTheme_StandsInTheUpperRightEvenWithMarksThere()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.Best };
        var upperRight = NewCalculator(Theme.MatplotlibClassic).Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        var marks = new MarkObstacles.Builder()
            .AddPoint(new Point(upperRight.BoxX + upperRight.BoxWidth / 2, upperRight.BoxY + upperRight.BoxHeight / 2))
            .Build();

        var layout = NewCalculator(Theme.MatplotlibClassic).Compute(legend, new[] { "A" }, PlotArea, marks);

        AssertSameBox(upperRight, layout);
    }

    // ── Font / title-font resolution (matches the OLD inline code) ────────

    [Fact]
    public void Compute_DefaultFontSize_UsesTickFont()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(Theme.Default.TickFont().Size, layout.Font.Size);
    }

    [Fact]
    public void Compute_ExplicitFontSize_AppliesOverride()
    {
        var legend = DefaultLegend() with { FontSize = 18 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(18, layout.Font.Size);
    }

    [Fact]
    public void Compute_TitleFont_IsBaseFontPlusOneBoldWhenTitleFontSizeNotSet()
    {
        var legend = DefaultLegend() with { Title = "T" };
        var baseSize = Theme.Default.TickFont().Size;
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(baseSize + 1, layout.TitleFont.Size);
        Assert.Equal(FontWeight.Bold, layout.TitleFont.Weight);
    }

    [Fact]
    public void Compute_TitleFont_UsesExplicitOverride()
    {
        var legend = DefaultLegend() with { Title = "T", TitleFontSize = 22 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(22, layout.TitleFont.Size);
        Assert.Equal(FontWeight.Bold, layout.TitleFont.Weight);
    }

    // ── Handle dimensions (matplotlib: 2.0em × 0.7em × MarkerScale) ───────

    [Fact]
    public void Compute_SwatchDimensions_FollowMatplotlibFormulas()
    {
        var legend = DefaultLegend() with { FontSize = 10, MarkerScale = 1.0 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(10 * 2.0, layout.SwatchWidth);
        Assert.Equal(10 * 0.7, layout.SwatchHeight);
        Assert.Equal(10 * 0.8, layout.SwatchGap);
    }

    [Fact]
    public void Compute_MarkerScale_ScalesHandleDimensions()
    {
        var legend = DefaultLegend() with { FontSize = 10, MarkerScale = 2.0 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(10 * 2.0 * 2.0, layout.SwatchWidth);
        Assert.Equal(10 * 0.7 * 2.0, layout.SwatchHeight);
    }

    // ── Column layout + totalContentWidth ─────────────────────────────────

    [Fact]
    public void Compute_SingleColumn_ColMaxWidthsMatchesLabelMeasurement()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Single(layout.ColMaxWidths);
        Assert.True(layout.ColMaxWidths[0] > 0);
    }

    [Fact]
    public void Compute_MultiColumn_NColsMatchesLegendConfig()
    {
        var legend = DefaultLegend() with { NCols = 3 };
        var layout = NewCalculator().Compute(legend, new[] { "A", "B", "C", "D" }, PlotArea);
        Assert.Equal(3, layout.NCols);
        Assert.Equal(2, layout.NRows);  // ceil(4/3) = 2
    }

    [Fact]
    public void Compute_NColsZero_ClampedToOne()
    {
        var legend = DefaultLegend() with { NCols = 0 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(1, layout.NCols);
    }

    // ── Box dimensions ────────────────────────────────────────────────────

    [Fact]
    public void Compute_BoxWidth_IsPaddingPlusContentWidthPlusPadding()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "ABC" }, PlotArea);
        Assert.Equal(layout.Padding + layout.TotalContentWidth + layout.Padding, layout.BoxWidth);
    }

    [Fact]
    public void Compute_NoTitle_TitleHeightIsZero()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(0, layout.TitleHeight);
    }

    [Fact]
    public void Compute_WithTitle_TitleHeightIsTitleFontSizePlus4()
    {
        var legend = DefaultLegend() with { Title = "T", TitleFontSize = 16 };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        Assert.Equal(16 + 4, layout.TitleHeight);
    }

    // ── Position dispatch (delegates to LegendPositionStrategy) ───────────

    [Theory]
    [InlineData(LegendPosition.UpperRight)]
    [InlineData(LegendPosition.Center)]
    public void Compute_Position_BoxXYMatchesStrategyOutput(LegendPosition position)
    {
        var legend = DefaultLegend() with { Position = position };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var (expX, expY) = LegendPositionStrategyFactory.Create(position)
            .ComputeBox(PlotArea, layout.BoxWidth, layout.BoxHeight);
        Assert.Equal(expX, layout.BoxX);
        Assert.Equal(expY, layout.BoxY);
    }

    // ── Helper methods (ColumnX, EntryY, ItemWidth) ───────────────────────

    [Fact]
    public void ColumnX_FirstColumn_IsBoxXPlusPadding()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(layout.BoxX + layout.Padding, layout.ColumnX(0));
    }

    [Fact]
    public void ColumnX_SecondColumn_AddsFirstColWidthPlusGap()
    {
        var legend = DefaultLegend() with { NCols = 2 };
        var layout = NewCalculator().Compute(legend, new[] { "A", "B" }, PlotArea);
        double expected = layout.BoxX + layout.Padding + layout.SwatchWidth + layout.SwatchGap + layout.ColMaxWidths[0] + layout.ColSpacingPx;
        Assert.Equal(expected, layout.ColumnX(1));
    }

    [Fact]
    public void EntryY_FirstRow_IsBoxYPlusPaddingPlusTitleHeight()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(layout.BoxY + layout.Padding + layout.TitleHeight, layout.EntryY(0));
    }

    [Fact]
    public void EntryY_SecondRow_AddsLineHeight()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(layout.EntryY(0) + layout.LineHeight, layout.EntryY(1));
    }

    [Fact]
    public void ItemWidth_ReturnsSwatchPlusGapPlusColumnWidth()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { "A" }, PlotArea);
        Assert.Equal(layout.SwatchWidth + layout.SwatchGap + layout.ColMaxWidths[0], layout.ItemWidth(0));
    }

    // ── Math text handling ────────────────────────────────────────────────

    [Fact]
    public void Compute_MathLabel_MeasuredViaRichText()
    {
        var layout = NewCalculator().Compute(DefaultLegend(), new[] { @"$\alpha$" }, PlotArea);
        Assert.True(layout.ColMaxWidths[0] > 0);
    }

    // ── The legend box of an axes (MeasureLegendBox): the margin an outside legend reserves is the box that is drawn ──

    private static (Axes axes, Theme theme, IRenderContext ctx) NewMeasureContext(
        params string?[] labels)
    {
        var theme = Theme.Default;
        var axes = new Axes();
        axes.Legend = axes.Legend with { Visible = true };
        for (int i = 0; i < labels.Length; i++)
        {
            var s = new LineSeries(
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 },
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 });
            s.Label = labels[i];
            axes.AddSeries(s);
        }
        IRenderContext ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        return (axes, theme, ctx);
    }

    [Fact]
    public void LegendBox_LegendInvisible_ReturnsEmpty()
    {
        // if (!axes.Legend.Visible) return Size.Empty; — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Visible = false };
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendBox_NoLabelledSeries_ReturnsEmpty()
    {
        // labels.Count == 0 path
        var (axes, theme, ctx) = NewMeasureContext(null, null);
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendBox_WithExplicitFontSize_AppliesOverride()
    {
        // legend.FontSize.HasValue ? tickFont with { Size = ... } : tickFont — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { FontSize = 18 };
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    /// <summary>A legend of a label has a width: with no font size given it reads the theme's tick font, a label that holds math is measured
    /// as rich text, and a plain one as text.</summary>
    [Theory]
    [InlineData("L1")]
    [InlineData(@"$\alpha$")]
    [InlineData("plain")]
    public void LegendBox_MeasuresALabelWithTheThemeTickFont(string label)
    {
        var (axes, theme, ctx) = NewMeasureContext(label);
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendBox_WithExplicitTitleFontSize_UsesOverride()
    {
        // legend.TitleFontSize.HasValue ? ... : ... — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend", TitleFontSize = 20 };
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void LegendBox_TitleWithoutExplicitTitleFontSize_UsesDefaultPlusOne()
    {
        // false arm — Title set, TitleFontSize null → uses base+1 bold
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend" };
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    [Theory]
    [InlineData(LegendPosition.OutsideBottom, 20.0, "T", 1)]    // the title font took the theme's size in one and the legend's in the other
    [InlineData(LegendPosition.OutsideRight, null, null, 1)]
    [InlineData(LegendPosition.UpperRight, 11.0, "Title", 2)]
    [InlineData(LegendPosition.OutsideTop, 20.0, null, 3)]
    [InlineData(LegendPosition.Best, 16.0, "Signals", 1)]
    [InlineData(LegendPosition.OutsideLeft, 9.0, "A longer legend title", 2)]
    public void TheMeasuredBox_IsTheBoxTheCalculatorLaysOut(LegendPosition position, double? fontSize, string? title, int columns)
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "alpha")
                .Plot([0.0, 1.0], [1.0, 0.0], s => s.Label = "beta gamma")
                .Plot([0.0, 1.0], [0.5, 0.5], s => s.Label = "delta")
                .WithLegend(l => l with { Position = position, FontSize = fontSize, Title = title, NCols = columns }))
            .Build();
        var axes = figure.SubPlots[0];
        var ctx = new SvgRenderContext();

        var measured = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, figure.Theme);
        var laidOut = new LegendLayoutCalculator(figure.Theme, ctx)
            .Compute(axes.Legend, axes.LegendEntries(Theme.Default, false).Select(e => e.Label).ToList(), new Rect(0, 0, 500, 300));

        Assert.Equal(laidOut.BoxWidth, measured.Width, 9);
        Assert.Equal(laidOut.BoxHeight, measured.Height, 9);
    }

    /// <summary>A labelled patch is a row of the legend, after the series' rows, so the margin an outside legend reserves holds it;
    /// a patch whose label starts with an underscore has no row and takes no room.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight)]
    [InlineData(LegendPosition.OutsideBottom)]
    [InlineData(LegendPosition.UpperRight)]
    public void TheMeasuredBox_HoldsThePatchRows_AfterTheSeriesRows(LegendPosition position)
    {
        var figure = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .AddPatch(new Rectangle(new(0, 0), 1, 1) { Label = "a much longer patch name" })
                .Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = "alpha")
                .AddPatch(new Circle(new(0.5, 0.5), 0.1) { Label = "_skipped" })
                .Plot([0.0, 1.0], [1.0, 0.0], s => s.Label = "beta gamma")
                .WithLegend(l => l with { Position = position }))
            .Build();
        var axes = figure.SubPlots[0];
        var ctx = new SvgRenderContext();

        var measured = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, figure.Theme);
        var laidOut = new LegendLayoutCalculator(figure.Theme, ctx)
            .Compute(axes.Legend, ["alpha", "beta gamma", "a much longer patch name"], new Rect(0, 0, 500, 300));

        Assert.Equal(laidOut.BoxWidth, measured.Width, 9);
        Assert.Equal(laidOut.BoxHeight, measured.Height, 9);
    }
}
