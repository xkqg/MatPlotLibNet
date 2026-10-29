// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

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
        Assert.Equal(expected.BoxX, actual.BoxX);
        Assert.Equal(expected.BoxY, actual.BoxY);
        Assert.Equal(expected.BoxWidth, actual.BoxWidth);
        Assert.Equal(expected.BoxHeight, actual.BoxHeight);
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
    public void PositionOf_BestUnderTheClassicTheme_IsTheThemesPlace()
        => Assert.Equal(LegendPosition.UpperRight,
            NewCalculator(Theme.MatplotlibClassic).PositionOf(DefaultLegend() with { Position = LegendPosition.Best }));

    [Fact]
    public void PositionOf_BestUnderTheDefaultTheme_IsBest()
        => Assert.Equal(LegendPosition.Best,
            NewCalculator(Theme.MatplotlibV2).PositionOf(DefaultLegend() with { Position = LegendPosition.Best }));

    [Fact]
    public void PositionOf_ANamedPlace_IsThatPlaceUnderAnyTheme()
        => Assert.Equal(LegendPosition.LowerLeft,
            NewCalculator(Theme.MatplotlibClassic).PositionOf(DefaultLegend() with { Position = LegendPosition.LowerLeft }));

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
        Assert.Equal(ThemedFontProvider.TickFont(Theme.Default).Size, layout.Font.Size);
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
        var baseSize = ThemedFontProvider.TickFont(Theme.Default).Size;
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

    [Fact]
    public void Compute_UpperRightPosition_BoxXYMatchesStrategyOutput()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.UpperRight };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var (expX, expY) = LegendPositionStrategyFactory.Create(LegendPosition.UpperRight)
            .ComputeBox(PlotArea, layout.BoxWidth, layout.BoxHeight);
        Assert.Equal(expX, layout.BoxX);
        Assert.Equal(expY, layout.BoxY);
    }

    [Fact]
    public void Compute_CenterPosition_BoxXYMatchesStrategyOutput()
    {
        var legend = DefaultLegend() with { Position = LegendPosition.Center };
        var layout = NewCalculator().Compute(legend, new[] { "A" }, PlotArea);
        var (expX, expY) = LegendPositionStrategyFactory.Create(LegendPosition.Center)
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
}
