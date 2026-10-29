// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Every graphic that wears the neutral grey takes it from the theme the figure is drawn under, one grey for all of
/// them, and a colour set on the graphic itself wins. Before, each of them wore the constant #808080: seven drawing sites
/// (reference lines, trendlines, price levels, Fibonacci retracements, a waterfall's connectors, a stem's baseline, the lines
/// of a contour without a colour map) and the dumbbell's connector, whose grey was fixed when the verb ran and so followed no
/// theme. Nord's neutral is #8D8D8D; the default theme's is #808080.</summary>
public class NeutralDrawSiteTests
{
    private static readonly double[] X = [1, 2, 3, 4];
    private static readonly double[] Y = [1, 3, 2, 4];

    public static TheoryData<string> Sites =>
        ["reference line", "trendline", "level", "fibonacci", "waterfall connector", "stem baseline", "contour line"];

    private static void Draw(string site, AxesBuilder ax)
    {
        switch (site)
        {
            case "reference line": ax.Plot(X, Y).AxHLine(2.5); break;
            case "trendline": ax.Plot(X, Y).AddTrendline(1, 1, 4, 4); break;
            case "level": ax.Plot(X, Y).AddLevel(2.5); break;
            case "fibonacci": ax.Plot(X, Y).AddFibonacci(4, 1); break;
            case "waterfall connector": ax.Waterfall(["a", "b", "c"], [3.0, -1, 2]); break;
            case "stem baseline": ax.Stem(X, Y); break;
            default:
                ax.Contour([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } });
                break;
        }
    }

    private static string Svg(string site, Theme theme) =>
        Plt.Create().WithSize(500, 350).WithTheme(theme).AddSubPlot(1, 1, 1, ax => Draw(site, ax)).Build().ToSvg();

    private static string Stroke(Color color) => $"stroke=\"{color.ToHex()}\"";

    /// <summary>Measured before the change: under Nord every one of these drew #808080, which stands at 2.55 : 1 against
    /// Nord's plot.</summary>
    [Theory]
    [MemberData(nameof(Sites))]
    public void ASite_WearsTheNeutralOfTheTheme_UnderNord(string site)
    {
        string svg = Svg(site, Theme.Nord);

        Assert.Contains(Stroke(Color.FromHex("#8D8D8D")), svg);
        Assert.DoesNotContain(Stroke(Color.FromHex("#808080")), svg);
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void ASite_WearsTheLibrarysGrey_UnderTheDefaultTheme(string site) =>
        Assert.Contains(Stroke(Color.FromHex("#808080")), Svg(site, Theme.Default));

    /// <summary>The control: a chart with none of the sites draws neither grey, so the strokes found above are the sites'.</summary>
    [Fact]
    public void AChartWithNoSite_DrawsNeitherGrey()
    {
        string svg = Plt.Create().WithSize(500, 350).WithTheme(Theme.Nord).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y)).Build().ToSvg();

        Assert.DoesNotContain(Stroke(Color.FromHex("#8D8D8D")), svg);
        Assert.DoesNotContain(Stroke(Color.FromHex("#808080")), svg);
    }

    // ── a colour of its own wins ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AColourSetOnTheGraphic_WinsOverTheNeutral()
    {
        var red = Colors.Red;
        string svg = Plt.Create().WithSize(500, 350).WithTheme(Theme.Nord).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .AxHLine(2.5, l => l.Color = red)
            .AddLevel(2.0, l => l.Color = red)
            .AddTrendline(1, 1, 4, 4, t => t.Color = red)).Build().ToSvg();

        Assert.Contains(Stroke(red), svg);
        Assert.DoesNotContain(Stroke(Color.FromHex("#8D8D8D")), svg);
        Assert.DoesNotContain(Stroke(Color.FromHex("#808080")), svg);
    }

    [Fact]
    public void AStemBaselineColour_WinsOverTheNeutral()
    {
        string svg = Plt.Create().WithSize(500, 350).WithTheme(Theme.Nord)
            .AddSubPlot(1, 1, 1, ax => ax.Stem(X, Y, s => s.BaselineColor = Colors.Red)).Build().ToSvg();

        Assert.Contains(Stroke(Colors.Red), svg);
        Assert.DoesNotContain(Stroke(Color.FromHex("#8D8D8D")), svg);
    }

    // ── the dumbbell's connector wears the neutral of whatever theme is set after the verb ───────────

    private static Figure Dumbbell(Theme theme) => Plt.Create().WithSize(800, 450).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Dumbbell(["a", "b", "c"], new BarGroup("before", [1.0, 4, 2]), new BarGroup("after", [3.0, 2, 5]))).Build();

    private static ErrorBarSeries ConnectorOf(Figure figure) => figure.SubPlots[0].Series.OfType<ErrorBarSeries>().Single();

    /// <summary>The connector names no colour and says it wears the neutral; the theme it is drawn under decides which.</summary>
    [Theory]
    [InlineData("Default", "#808080")]
    [InlineData("Nord", "#8D8D8D")]
    [InlineData("Dracula", "#949494")]
    public void TheDumbbellConnector_WearsTheNeutralOfTheTheme_ItIsDrawnUnder(string theme, string neutral)
    {
        var figure = Dumbbell((Theme)typeof(Theme).GetProperty(theme)!.GetValue(null)!);

        Assert.Null(ConnectorOf(figure).Color);
        Assert.Contains(Stroke(Color.FromHex(neutral)), figure.ToSvg());
    }

    [Fact]
    public void TheDumbbellConnector_FollowsATheme_SetAfterTheVerbAndChangedAfterTheBuild()
    {
        var figure = Dumbbell(Theme.Default);
        Assert.Contains(Stroke(Color.FromHex("#808080")), figure.ToSvg());

        figure.Theme = Theme.Nord;

        Assert.Contains(Stroke(Color.FromHex("#8D8D8D")), figure.ToSvg());
        Assert.DoesNotContain(Stroke(Color.FromHex("#808080")), figure.ToSvg());
    }

    /// <summary>Measured before the change: the connector's explicit grey took the colour slot after the dots, so a series
    /// added after the dumbbell skipped one colour of the cycle.</summary>
    [Fact]
    public void TheDumbbellConnector_TakesNoColourSlot()
    {
        var figure = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Dumbbell(["a", "b", "c"], new BarGroup("before", [1.0, 4, 2]), new BarGroup("after", [3.0, 2, 5]))
            .Plot([0.0, 1, 2], [1.0, 2, 3])).Build();

        string svg = figure.ToSvg();

        Assert.Contains(Stroke(figure.Theme.CycleColors[2]), svg);
        Assert.DoesNotContain(Stroke(figure.Theme.CycleColors[3]), svg);
    }

    // ── on the wire ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheNeutralRole_IsWrittenAsAFlag_NotAsAColour()
    {
        string json = Dumbbell(Theme.Default).ToJson();

        Assert.Contains("\"wearsNeutral\":true", json);
        Assert.DoesNotContain("#808080", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheNeutralRole_ReadsBack_AndStillFollowsTheTheme()
    {
        var read = new ChartSerializer().FromJson(Dumbbell(Theme.Default).ToJson());

        Assert.True(ConnectorOf(read).Color is null);
        read.Theme = Theme.Nord;
        Assert.Contains(Stroke(Color.FromHex("#8D8D8D")), read.ToSvg());
    }

    /// <summary>JSON written by 1.24.0 carries the connector's grey as an explicit colour; that is data, and it is read back
    /// as written.</summary>
    [Fact]
    public void AnExplicitGrey_InAJson_StaysTheColourItWas()
    {
        var figure = Plt.Create().WithSize(500, 350).WithTheme(Theme.Nord).AddSubPlot(1, 1, 1, ax => ax
            .AddSeries(new ErrorBarSeries([1.0, 2], [1.0, 2], new double[2], [1.0, 1.0]) { Color = Colors.Gray, LineWidth = 3 })).Build();

        var read = new ChartSerializer().FromJson(figure.ToJson());

        Assert.Equal(Colors.Gray, ConnectorOf(read).Color);
        Assert.Contains(Stroke(Colors.Gray), read.ToSvg());
    }

    [Fact]
    public void ASeriesThatWearsNothing_WritesNoFlag()
    {
        string json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y)).Build().ToJson();

        Assert.DoesNotContain("wearsNeutral", json);
    }
}
