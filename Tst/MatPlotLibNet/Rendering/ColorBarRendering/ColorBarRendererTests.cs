// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>
/// Direct unit tests for the colorbar renderer hierarchy extracted in Phase B.1
/// (<see cref="ColorBarRenderer"/>, <see cref="HorizontalColorBarRenderer"/>,
/// <see cref="VerticalColorBarRenderer"/>, <see cref="ColorBarRendererFactory"/>).
/// Targets every branch of bar placement, extend-min / extend-max wedges,
/// gradient steps, edge lines, tick labels, and optional bar label.
/// </summary>
public class ColorBarRendererTests
{
    private static readonly IColorMap Map = ColorMaps.Viridis;
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(100, 50, 400, 300);

    // ──────────────────────────────────────────────────────────────────────────
    // Factory
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Factory_Horizontal_ReturnsHorizontalRenderer()
    {
        var cb = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Horizontal };
        var r = ColorBarRendererFactory.Create(cb, Map, 0, 1, Plot, new RecordingRenderContext(), DefaultTheme);
        Assert.IsType<HorizontalColorBarRenderer>(r);
    }

    [Fact]
    public void Factory_Vertical_ReturnsVerticalRenderer()
    {
        var cb = new ColorBar { Visible = true, Orientation = ColorBarOrientation.Vertical };
        var r = ColorBarRendererFactory.Create(cb, Map, 0, 1, Plot, new RecordingRenderContext(), DefaultTheme);
        Assert.IsType<VerticalColorBarRenderer>(r);
    }

    /// <summary>The overload without a normaliser is the one with the linear normaliser: the orientation is chosen once, in the
    /// overload that takes it, and a bar drawn without one places its ticks evenly along the range.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal)]
    [InlineData(ColorBarOrientation.Vertical)]
    public void Factory_WithoutANormalizer_IsTheFactoryWithTheLinearOne(ColorBarOrientation orientation)
    {
        var cb = new ColorBar { Visible = true, Orientation = orientation };
        var linear = ColorBarRendererFactory.Create(cb, Map, 0, 1, Plot, new RecordingRenderContext(), DefaultTheme);
        var log = ColorBarRendererFactory.Create(cb, Map, 1, 10, new LogNormalizer(), Plot, new RecordingRenderContext(), DefaultTheme);

        Assert.Same(LinearNormalizer.Instance, linear.Normalizer);
        Assert.IsType<LogNormalizer>(log.Normalizer);
        Assert.Equal(linear.GetType(), log.GetType());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // What a bar draws, one dimension at a time: the factory picks the renderer of the orientation, so each row reads
    // the orientation it names.
    // ──────────────────────────────────────────────────────────────────────────

    private static RecordingRenderContext Drawn(ColorBar bar, IColorMap? map = null, double max = 1)
    {
        var ctx = new RecordingRenderContext();
        ColorBarRendererFactory.Create(bar, map ?? Map, 0, max, Plot, ctx, DefaultTheme).Render();
        return ctx;
    }

    /// <summary>The 50 gradient steps and the frame, plus one wedge at each end the bar extends to.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Neither, 51)]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Min, 52)]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Max, 52)]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Both, 53)]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Neither, 51)]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Min, 52)]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Max, 52)]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Both, 53)]
    public void ABar_DrawsItsStepsItsFrameAndAWedgeAtEachEndItExtendsTo(ColorBarOrientation orientation, ColorBarExtend extend, int rectangles)
    {
        Assert.Equal(rectangles, Drawn(new ColorBar { Orientation = orientation, Extend = extend }).Count<DrawRectangleCall>());
    }

    /// <summary>One edge line per gradient step when the bar draws its edges, none otherwise.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal, true, 50)]
    [InlineData(ColorBarOrientation.Horizontal, false, 0)]
    [InlineData(ColorBarOrientation.Vertical, true, 50)]
    [InlineData(ColorBarOrientation.Vertical, false, 0)]
    public void ABar_DrawsAnEdgeLinePerStepOnlyWhenItDrawsEdges(ColorBarOrientation orientation, bool drawEdges, int lines)
    {
        Assert.Equal(lines, Drawn(new ColorBar { Orientation = orientation, DrawEdges = drawEdges }).Count<DrawLineCall>());
    }

    /// <summary>Six tick labels over 0 to 1, and a seventh text for the bar's own label when it has one.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal, null, 6)]
    [InlineData(ColorBarOrientation.Horizontal, "Intensity", 7)]
    [InlineData(ColorBarOrientation.Vertical, null, 6)]
    [InlineData(ColorBarOrientation.Vertical, "Z value", 7)]
    public void ABar_DrawsSixTickLabelsAndItsOwnLabelWhenItHasOne(ColorBarOrientation orientation, string? label, int texts)
    {
        var ctx = Drawn(new ColorBar { Orientation = orientation, Label = label });

        Assert.Equal(texts, ctx.Count<DrawTextCall>());
        Assert.Equal(label is not null, ctx.Of<DrawTextCall>().Any(c => c.Text == label));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Geometry: the frame is the bar's border rectangle (a stroke, no fill)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>The bar stands past the plot, below it when horizontal and to its right when vertical, by its padding.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal)]
    [InlineData(ColorBarOrientation.Vertical)]
    public void TheBar_StandsPastThePlotByItsPadding(ColorBarOrientation orientation)
    {
        var rect = Drawn(new ColorBar { Orientation = orientation, Padding = 10 }).Calls.Frame().Rect;

        Assert.Equal(orientation == ColorBarOrientation.Horizontal ? Plot.Y + Plot.Height + 10 : Plot.X + Plot.Width + 10,
            orientation == ColorBarOrientation.Horizontal ? rect.Y : rect.X);
    }

    [Fact]
    public void Horizontal_AspectZero_UsesExplicitWidth()
    {
        var rect = Drawn(new ColorBar { Orientation = ColorBarOrientation.Horizontal, Aspect = 0, Width = 15 }).Calls.Frame().Rect;

        Assert.Equal(15.0, rect.Height, precision: 6);
    }

    /// <summary>fullW = plotW * Shrink = 400; barH = 400/20 = 20.</summary>
    [Fact]
    public void Horizontal_AspectPositive_ComputesHeightFromWidth()
    {
        var rect = Drawn(new ColorBar { Orientation = ColorBarOrientation.Horizontal, Aspect = 20, Shrink = 1 }).Calls.Frame().Rect;

        Assert.Equal(20.0, rect.Height, precision: 6);
    }

    [Fact]
    public void Vertical_BarHeight_EqualsPlotHeightTimesShrink()
    {
        var rect = Drawn(new ColorBar { Orientation = ColorBarOrientation.Vertical, Shrink = 0.5 }).Calls.Frame().Rect;

        Assert.Equal(Plot.Height * 0.5, rect.Height, precision: 6);
    }

    [Fact]
    public void Vertical_WithLabel_DrawsRotatedLabel()
    {
        var labelCall = Drawn(new ColorBar { Orientation = ColorBarOrientation.Vertical, Label = "Z value" }).Of<DrawTextCall>().First(c => c.Text == "Z value");

        Assert.Equal(90.0, labelCall.Rotation);
    }

    /// <summary>A vertical bar reads its ticks from the maximum at the top down to the minimum, a horizontal one from the minimum on the
    /// left to the maximum on the right.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical, "10", "0")]
    [InlineData(ColorBarOrientation.Horizontal, "0", "10")]
    public void TheTickLabels_RunFromOneEndOfTheBarToTheOther(ColorBarOrientation orientation, string first, string last)
    {
        var tickTexts = Drawn(new ColorBar { Orientation = orientation }, max: 10).Of<DrawTextCall>().Select(c => c.Text).ToList();

        Assert.StartsWith(first, tickTexts.First());
        Assert.StartsWith(last, tickTexts.Last());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Under / over color resolution (null fallbacks in the ColorMap)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>The default Viridis has no under or over colour: an extended end takes the map's colour at its end.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Min, 0.0)]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Max, 1.0)]
    public void AnExtendedEnd_WithNoUnderOrOverColor_FallsBackToTheMapsColourAtThatEnd(ColorBarOrientation orientation, ColorBarExtend extend, double position)
    {
        var firstRect = Drawn(new ColorBar { Orientation = orientation, Extend = extend }).Of<DrawRectangleCall>()[0];

        Assert.Equal(Map.GetColor(position), (Color?)firstRect.Fill);
    }

    [Theory]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Min, "#FF0000")]
    [InlineData(ColorBarOrientation.Horizontal, ColorBarExtend.Max, "#00FF00")]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Min, "#0000FF")]
    [InlineData(ColorBarOrientation.Vertical, ColorBarExtend.Max, "#FFFF00")]
    public void AnExtendedEnd_WithAnExplicitUnderOrOverColor_UsesIt(ColorBarOrientation orientation, ColorBarExtend extend, string hex)
    {
        var colour = Color.FromHex(hex);
        var map = extend == ColorBarExtend.Min ? new OverUnderColorMap(under: colour, over: null) : new OverUnderColorMap(under: null, over: colour);

        var wedge = Drawn(new ColorBar { Orientation = orientation, Extend = extend }, map).Of<DrawRectangleCall>().First(c => c.Fill as Color? == colour);

        Assert.NotNull(wedge);
    }

    // Helper ColorMap with explicit under/over colors
    private sealed class OverUnderColorMap(Color? under, Color? over) : TestColorMap("OverUnder", _ => Color.FromHex("#808080")), IColorMap
    {
        public Color? GetUnderColor() => under;
        public Color? GetOverColor() => over;
    }
}
