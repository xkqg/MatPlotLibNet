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
public class ColorBarRendererUnitTests
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

    // ──────────────────────────────────────────────────────────────────────────
    // Base class state — exercised through subclasses
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Renderer_ExtendNeither_ExtendFlagsBothFalse()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Extend = ColorBarExtend.Neither, Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();

        // Neither extend → gradient rect count = Steps (50); no wedge rects at both ends.
        // Plus 1 frame border rectangle. Steps(50) + border(1) = 51 total rectangles.
        Assert.Equal(51, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Renderer_ExtendMinOnly_DrawsUnderWedge()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Extend = ColorBarExtend.Min, Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();

        // 50 gradient + 1 under wedge + 1 border = 52
        Assert.Equal(52, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Renderer_ExtendMaxOnly_DrawsOverWedge()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Extend = ColorBarExtend.Max, Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(52, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Renderer_ExtendBoth_DrawsTwoWedges()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Extend = ColorBarExtend.Both, Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(53, ctx.CountOf("DrawRectangle"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Horizontal layout — geometry
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Horizontal_BarBelowPlotArea()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Padding = 10 };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();

        // Border rectangle (non-null stroke, null fill) runs across the full bar width.
        var border = ctx.Calls.First(c => c.Kind == "DrawRectangle"
            && ((dynamic)c.Args!).fill is null);
        var rect = (Rect)((dynamic)border.Args!).rect;
        Assert.Equal(Plot.Y + Plot.Height + cb.Padding, rect.Y);
    }

    [Fact]
    public void Horizontal_AspectZero_UsesExplicitWidth()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Aspect = 0, Width = 15 };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var border = ctx.Calls.First(c => c.Kind == "DrawRectangle"
            && ((dynamic)c.Args!).fill is null);
        var rect = (Rect)((dynamic)border.Args!).rect;
        Assert.Equal(15.0, rect.Height, precision: 6);
    }

    [Fact]
    public void Horizontal_AspectPositive_ComputesHeightFromWidth()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Aspect = 20, Shrink = 1 };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var border = ctx.Calls.First(c => c.Kind == "DrawRectangle"
            && ((dynamic)c.Args!).fill is null);
        var rect = (Rect)((dynamic)border.Args!).rect;
        // fullW = plotW * Shrink = 400; barH = 400/20 = 20
        Assert.Equal(20.0, rect.Height, precision: 6);
    }

    [Fact]
    public void Horizontal_DrawEdges_EmitsEdgeLinePerStep()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, DrawEdges = true };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        // One DrawLine per gradient step when DrawEdges=true → 50
        Assert.Equal(50, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Horizontal_DrawEdgesFalse_EmitsNoEdgeLines()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, DrawEdges = false };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Horizontal_TickLabels_SixByDefault()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        // 6 tick labels, no bar label (Label = null)
        Assert.Equal(6, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Horizontal_WithLabel_DrawsSeventhText()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Label = "Intensity" };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(7, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "Intensity");
    }

    [Fact]
    public void Horizontal_WithLabel_Null_SkipsLabelDraw()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Label = null };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.DoesNotContain(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vertical layout — geometry
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Vertical_BarRightOfPlotArea()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Padding = 10 };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var border = ctx.Calls.First(c => c.Kind == "DrawRectangle"
            && ((dynamic)c.Args!).fill is null);
        var rect = (Rect)((dynamic)border.Args!).rect;
        Assert.Equal(Plot.X + Plot.Width + cb.Padding, rect.X);
    }

    [Fact]
    public void Vertical_BarHeight_EqualsPlotHeightTimesShrink()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Shrink = 0.5 };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var border = ctx.Calls.First(c => c.Kind == "DrawRectangle"
            && ((dynamic)c.Args!).fill is null);
        var rect = (Rect)((dynamic)border.Args!).rect;
        Assert.Equal(Plot.Height * 0.5, rect.Height, precision: 6);
    }

    [Fact]
    public void Vertical_ExtendMax_DrawsOverWedgeAtTop()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Max };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(52, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Vertical_ExtendMin_DrawsUnderWedgeAtBottom()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Min };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(52, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Vertical_ExtendBoth_DrawsBothWedges()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Both };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(53, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Vertical_ExtendNeither_DrawsNoWedges()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Neither };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(51, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Vertical_DrawEdges_EmitsEdgeLinePerStep()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, DrawEdges = true };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(50, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Vertical_DrawEdgesFalse_NoEdgeLines()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, DrawEdges = false };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Vertical_WithLabel_DrawsRotatedLabel()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Label = "Z value" };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var labelCall = ctx.OfKind("DrawText").First(c =>
            (string)((dynamic)c.Args!).text == "Z value");
        Assert.Equal(90.0, (double)((dynamic)labelCall.Args!).rotation);
    }

    [Fact]
    public void Vertical_WithoutLabel_DrawsSixTickLabelsOnly()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Label = null };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        Assert.Equal(6, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Vertical_TickLabelsDescendTopToBottom()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical };
        new VerticalColorBarRenderer(cb, Map, 0, 10, Plot, ctx, DefaultTheme).Render();
        var tickTexts = ctx.OfKind("DrawText")
            .Select(c => (string)((dynamic)c.Args!).text)
            .ToList();
        // First tick = Max, last = Min (top-to-bottom in vertical bar)
        Assert.StartsWith("10", tickTexts.First());
        Assert.StartsWith("0", tickTexts.Last());
    }

    [Fact]
    public void Horizontal_TickLabelsAscendLeftToRight()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal };
        new HorizontalColorBarRenderer(cb, Map, 0, 10, Plot, ctx, DefaultTheme).Render();
        var tickTexts = ctx.OfKind("DrawText")
            .Select(c => (string)((dynamic)c.Args!).text)
            .ToList();
        Assert.StartsWith("0", tickTexts.First());
        Assert.StartsWith("10", tickTexts.Last());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Under / over color resolution (null fallbacks in the ColorMap)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Horizontal_ExtendMin_NullUnderColor_FallsBackToMapZero()
    {
        // Default Viridis returns null for GetUnderColor → fallback to GetColor(0.0).
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Extend = ColorBarExtend.Min };
        new HorizontalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var firstRect = ctx.OfKind("DrawRectangle")[0];
        var fill = ((dynamic)firstRect.Args!).fill;
        // Under-color should equal Map.GetColor(0)
        Assert.Equal(Map.GetColor(0), (Color?)fill);
    }

    [Fact]
    public void Vertical_ExtendMax_NullOverColor_FallsBackToMapOne()
    {
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Max };
        new VerticalColorBarRenderer(cb, Map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var firstRect = ctx.OfKind("DrawRectangle")[0];
        var fill = ((dynamic)firstRect.Args!).fill;
        Assert.Equal(Map.GetColor(1), (Color?)fill);
    }

    [Fact]
    public void Horizontal_ExtendMin_ExplicitUnderColor_UsesIt()
    {
        var map = new OverUnderColorMap(under: Color.FromHex("#FF0000"), over: null);
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Extend = ColorBarExtend.Min };
        new HorizontalColorBarRenderer(cb, map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var firstRect = ctx.OfKind("DrawRectangle")[0];
        var fill = ((dynamic)firstRect.Args!).fill;
        Assert.Equal(Color.FromHex("#FF0000"), (Color?)fill);
    }

    [Fact]
    public void Horizontal_ExtendMax_ExplicitOverColor_UsesIt()
    {
        var map = new OverUnderColorMap(under: null, over: Color.FromHex("#00FF00"));
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Horizontal, Extend = ColorBarExtend.Max };
        new HorizontalColorBarRenderer(cb, map, 0, 1, Plot, ctx, DefaultTheme).Render();
        // Over wedge comes AFTER gradient steps (index ~50). Look for fill==Green.
        var overWedge = ctx.OfKind("DrawRectangle").First(c =>
        {
            var f = ((dynamic)c.Args!).fill as Color?;
            return f == Color.FromHex("#00FF00");
        });
        Assert.NotNull(overWedge);
    }

    [Fact]
    public void Vertical_ExtendMin_ExplicitUnderColor_UsesIt()
    {
        var map = new OverUnderColorMap(under: Color.FromHex("#0000FF"), over: null);
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Min };
        new VerticalColorBarRenderer(cb, map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var underWedge = ctx.OfKind("DrawRectangle").First(c =>
        {
            var f = ((dynamic)c.Args!).fill as Color?;
            return f == Color.FromHex("#0000FF");
        });
        Assert.NotNull(underWedge);
    }

    [Fact]
    public void Vertical_ExtendMax_ExplicitOverColor_UsesIt()
    {
        var map = new OverUnderColorMap(under: null, over: Color.FromHex("#FFFF00"));
        var ctx = new RecordingRenderContext();
        var cb = new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = ColorBarExtend.Max };
        new VerticalColorBarRenderer(cb, map, 0, 1, Plot, ctx, DefaultTheme).Render();
        var overWedge = ctx.OfKind("DrawRectangle")[0];
        var fill = ((dynamic)overWedge.Args!).fill as Color?;
        Assert.Equal(Color.FromHex("#FFFF00"), fill);
    }

    // Helper ColorMap with explicit under/over colors
    private sealed class OverUnderColorMap : IColorMap
    {
        private readonly Color? _under;
        private readonly Color? _over;
        public OverUnderColorMap(Color? under, Color? over) { _under = under; _over = over; }
        public string Name => "OverUnder";
        public Color GetColor(double value) => Color.FromHex("#808080");
        public Color? GetUnderColor() => _under;
        public Color? GetOverColor() => _over;
    }
}
