// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A rich text is laid out once in core and drawn through the public members every surface has: each run of glyphs a
/// <c>DrawText</c> where the layout puts it (a superscript raised, a subscript dropped, a numerator above its denominator), each rule
/// (a fraction's line, a radical's bar) a <c>DrawRectangle</c>. So the SVG writes a text element per run and a rectangle per rule,
/// and a surface that implements nothing of rich text draws math as well.</summary>
public class RichTextRenderTests
{
    private static readonly Font Font = new() { Size = 12 };

    /// <summary>Every text element of <paramref name="svg"/>: its characters, its baseline's y and its size.</summary>
    private static List<SvgText> Texts(string svg) => [.. svg.Elements("text").Select(text => new SvgText(text.Text, text.Number("y"), text.Number("font-size")))];

    private readonly record struct SvgText(string Text, double Y, double Size);

    private static string Svg(string label)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRichText(MathTextParser.Parse(label), new Point(50, 50), Font, TextAlignment.Left);
        return ctx.GetOutput();
    }

    [Fact]
    public void DrawRichText_PlainText_EmitsOneTextElement()
    {
        var texts = Texts(Svg("hello"));

        Assert.Equal("hello", Assert.Single(texts).Text);
        Assert.Equal(50, texts[0].Y);
    }

    /// <summary>The superscript is a text of its own, smaller (one size down, 0.7) and raised above the baseline.</summary>
    [Fact]
    public void DrawRichText_WithSuperscript_RaisesASmallerRun()
    {
        var texts = Texts(Svg("$R^{2}$"));

        var r = texts.Single(t => t.Text == "R");
        var two = texts.Single(t => t.Text == "2");
        Assert.Equal(50, r.Y);
        Assert.True(two.Y < r.Y, $"the superscript stands at {two.Y}, the R at {r.Y}");
        Assert.Equal(12 * 0.7, two.Size, 9);
    }

    [Fact]
    public void DrawRichText_WithSubscript_DropsASmallerRun()
    {
        var texts = Texts(Svg("$x_{i}$"));

        Assert.True(texts.Single(t => t.Text == "i").Y > texts.Single(t => t.Text == "x").Y);
    }

    /// <summary>A fraction is stacked (a named fix: the flat parse set the numerator beside the denominator, raised and dropped): the
    /// numerator above a rule above the denominator, the rule a rectangle.</summary>
    [Fact]
    public void DrawRichText_AFraction_IsStackedOverARule()
    {
        string svg = Svg(@"$\frac{a}{b}$");
        var texts = Texts(svg);

        double a = texts.Single(t => t.Text == "a").Y, b = texts.Single(t => t.Text == "b").Y;
        var rules = svg.Elements("rect");
        Assert.True(rules.Count > 0, "the rule is a rectangle");
        double ruleY = rules[0].Number("y");
        Assert.True(a < ruleY && ruleY < b, $"numerator {a}, rule {ruleY}, denominator {b}");
    }

    /// <summary>A radical draws its sign and a bar over its body (a named fix: the flat parse drew the body alone).</summary>
    [Fact]
    public void DrawRichText_ARadical_DrawsItsSignAndABarOverTheBody()
    {
        string svg = Svg(@"$\sqrt{x}$");

        Assert.Contains(Texts(svg), t => t.Text == "√");
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void DrawRichText_BoldSwitch_WritesItsRunBold()
    {
        // \mathbf is a value the caller gave: the run it makes is bold under every theme, and the SVG says so.
        Assert.Contains(Svg(@"$\mathbf{y} = x$").Elements("text"), text => text.Text == "y" && text["font-weight"] == "bold");
    }

    [Fact]
    public void DrawRichText_ItalicSwitch_WritesItsRunItalic()
    {
        Assert.Contains(Svg(@"$\mathit{ab}$").Elements("text"), text => text.Text == "ab" && text["font-style"] == "italic");
    }

    [Fact]
    public void MeasureRichText_MeasuresABoldSwitchInTheBoldFace()
    {
        var ctx = new FallbackRenderContext();

        var size = ((IRenderContext)ctx).MeasureRichText(MathTextParser.Parse(@"$\mathbf{y}$"), Font);

        Assert.Equal(1, ctx.Measured.Count(f => f.Weight == FontWeight.Bold));
        Assert.Equal(ctx.MeasureText("y", Font with { Weight = FontWeight.Bold }), size);
    }

    // --- A surface that implements no rich text ---

    /// <summary>The default draw of a rich text lays it out and draws each run through the surface's own <c>DrawText</c>: the alpha and
    /// its superscript are two texts, the superscript in its smaller size.</summary>
    [Fact]
    public void DrawRichText_OnASurfaceWithoutRichText_DrawsEachRunThroughDrawText()
    {
        IRenderContext ctx = new FallbackRenderContext();

        ctx.DrawRichText(MathTextParser.Parse("$\\alpha^{2}$"), new Point(0, 0), Font, TextAlignment.Left);

        var drawn = ((FallbackRenderContext)ctx).Drawn;
        Assert.Equal(["α", "2"], drawn.Select(d => d.Text));
        Assert.Equal(12 * 0.7, drawn[1].Size, 9);
        Assert.True(drawn[1].Y < 0, "the superscript stands above the baseline");
    }

    // --- Integration: chart title with math mode ---

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // α
        // β
        new DrawnToken("ChartTitle_WithGreek_ProducesUnicodeInSvg", () => Plt.Create()
            .WithTitle("$\\alpha$ vs $\\beta$")
            .Plot([1.0, 2.0], [1.0, 2.0])
            .ToSvg(), "α", "β"),
        // σ
        new DrawnToken("AxisLabel_WithGreek_ProducesUnicodeInSvg", () => new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetYLabel("$\\sigma$ (Pa)");
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
            })
            .Build()
            .ToSvg(), "σ"),
        new DrawnToken("DrawRichText_GreekAlpha_EmitsAlphaCharacter", () => Svg("$\\alpha$"), "α"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AxesTitle_WithSuperscript_DrawsTheSuperscriptAsARunOfItsOwn()
    {
        string svg = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("R$^{2}$");
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
            })
            .Build()
            .ToSvg();

        var texts = Texts(svg);
        Assert.True(texts.Single(t => t.Text == "2").Y < texts.Single(t => t.Text == "R").Y);
    }

    // --- Helper: minimal IRenderContext that keeps every text it draws ---

    private readonly record struct DrawnText(string Text, double Y, double Size);

    private sealed class FallbackRenderContext : SilentRenderContext
    {
        public List<DrawnText> Drawn { get; } = [];
        public List<Font> Measured { get; } = [];

        public override void DrawText(string text, Point position, Font font, TextAlignment alignment) => Drawn.Add(new(text, position.Y, font.Size));

        public override Size MeasureText(string text, Font font)
        {
            Measured.Add(font);
            return new(text.Length * font.Size * (font.Weight == FontWeight.Bold ? 0.7 : 0.6), font.Size * 1.2);
        }
    }
}
