// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that BeginGroup/EndGroup are available on IRenderContext
/// and that type casts to SvgRenderContext are no longer needed.</summary>
public class RenderContextGroupTests
{
    /// <summary>Verifies that IRenderContext has BeginGroup with default no-op.</summary>
    [Fact]
    public void IRenderContext_BeginGroup_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.BeginGroup("test"); // should not throw
        ctx.EndGroup();
    }

    /// <summary>A raster backend has nothing to link: the hyperlink members default to no-ops, so a
    /// linked tile renders on Skia exactly as an unlinked one.</summary>
    [Fact]
    public void IRenderContext_BeginHyperlink_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.BeginHyperlink("/?panel=processes", "Processes", expanded: true); // should not throw
        ctx.EndHyperlink();
    }

    /// <summary>A backend without markup draws a levelled text as ordinary text — the tree grid degrades to a
    /// flat list rather than throwing at it.</summary>
    [Fact]
    public void IRenderContext_DrawTextWithLevel_FallsBackToPlainText()
    {
        IRenderContext ctx = new NullRenderContext();

        ctx.DrawTextWithLevel("Svc.Ingest", new Point(0, 0), new Font { Size = 12 }, level: 2); // should not throw
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that SvgRenderContext.BeginGroup emits a g element.
        new DrawnToken("SvgRenderContext_BeginGroup_EmitsGElement", () =>
        {
            var ctx = new SvgRenderContext();
            ctx.BeginGroup("legend");
            ctx.EndGroup();
            return ctx.GetOutput();
        }, "<g class=\"legend\">", "</g>"),
        // Verifies that spine rendering uses BeginGroup without type casts.
        new DrawnToken("DrawSpineLine_NoTypeCast_StillEmitsGroup", () =>
        {
            var fig = new Figure();
            var ax = fig.AddSubPlot();
            ax.Plot([1.0, 2.0], [3.0, 4.0]);
            return fig.ToSvg();
        }, "class=\"spine\""),
        // Verifies that legend rendering emits a group element.
        new DrawnToken("RenderLegend_StillEmitsGroup", () =>
        {
            var fig = new Figure();
            var ax = fig.AddSubPlot();
            ax.Plot([1.0, 2.0], [3.0, 4.0]).Label = "Series 1";
            return fig.ToSvg();
        }, "class=\"legend\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Phase J — IRenderContext default-impl branch coverage ────────────────

    private static readonly Font TestFont = new() { Family = "Arial", Size = 12 };
    private static readonly Point Origin = new(0, 0);

    /// <summary>DrawText 5-arg default — rotation is ignored and 4-arg overload called.</summary>
    [Fact]
    public void IRenderContext_DrawText5Arg_DefaultDelegatesToNoRotation()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.DrawText("hello", Origin, TestFont, TextAlignment.Left, 45.0); // default: ignores rotation
    }

    /// <summary>SetNextElementData default no-op — does not throw.</summary>
    [Fact]
    public void IRenderContext_SetNextElementData_DefaultNoOp()
    {
        IRenderContext ctx = new NullRenderContext();
        ctx.SetNextElementData("data-x", "42");
    }

    /// <summary>MeasureRichText default — an empty rich text takes no width and the line of its font (1.2 font sizes).</summary>
    [Fact]
    public void IRenderContext_MeasureRichText_EmptySpans_IsOneLineOfTheFont()
    {
        IRenderContext ctx = new NullRenderContext();
        var rt = new RichText([]);
        var size = ctx.MeasureRichText(rt, TestFont);
        Assert.Equal(0.0, size.Width);
        Assert.Equal(12 * 1.2, size.Height, 9);
    }

    /// <summary>MeasureRichText default — each span of normal text at its own size, laid side by side: the widths summed, the letters
    /// of each rising its own font size and the rest of its measured height below the baseline.</summary>
    [Fact]
    public void IRenderContext_MeasureRichText_MultipleSpans_EachAtItsOwnSize()
    {
        IRenderContext ctx = new NullRenderContext(); // MeasureText always returns (50, 12)
        var rt = new RichText([new TextSpan("A"), new TextSpan("B", FontSizeScale: 0.7)]);
        var size = ctx.MeasureRichText(rt, TestFont);
        Assert.Equal(100.0, size.Width);   // 50 + 50
        Assert.Equal(12 + (12 - 12 * 0.7), size.Height, 9); // A rises 12; B, at 8.4, reaches 3.6 below the baseline
    }

    /// <summary>DrawRichText 4-arg default — lays the spans out and draws each run through DrawText.</summary>
    [Fact]
    public void IRenderContext_DrawRichText_DefaultDrawsEachRun()
    {
        var ctx = new RecordingNullContext();
        IRenderContext iCtx = ctx;
        var rt = new RichText([new TextSpan("Hello"), new TextSpan(" World")]);
        iCtx.DrawRichText(rt, Origin, TestFont, TextAlignment.Left);
        Assert.Equal(["Hello", " World"], ctx.Drawn);
    }

    /// <summary>DrawRichText 5-arg default — delegates to 4-arg, ignoring rotation.</summary>
    [Fact]
    public void IRenderContext_DrawRichText5Arg_DefaultIgnoresRotation()
    {
        var ctx = new RecordingNullContext();
        IRenderContext iCtx = ctx;
        var rt = new RichText([new TextSpan("Hi")]);
        iCtx.DrawRichText(rt, Origin, TestFont, TextAlignment.Left, 90.0);
        Assert.Equal("Hi", ctx.LastDrawnText);
    }

    /// <summary>A surface that draws nothing and measures every text as 50 by 12 (verifies default interface methods work).</summary>
    private sealed class NullRenderContext : SilentRenderContext
    {
        public override Size MeasureText(string text, Font font) => new(50, 12);
    }

    private sealed class RecordingNullContext : SilentRenderContext
    {
        public string? LastDrawnText { get; private set; }
        public List<string> Drawn { get; } = [];

        public override void DrawText(string text, Point position, Font font, TextAlignment alignment)
        {
            LastDrawnText = text;
            Drawn.Add(text);
        }

        public override Size MeasureText(string text, Font font) => new(50, 12);
    }
}
