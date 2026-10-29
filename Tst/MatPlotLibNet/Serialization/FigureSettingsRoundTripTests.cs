// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The figure JSON carries what the figure itself draws with: its theme (the colours, fonts, grid, cycle and
/// spacing every part falls back on), its spacing, and its interaction settings (the scripts its SVG embeds and the
/// tokens they read). A figure drawn under a theme that came back on the default theme would be a different picture the
/// moment it is sent over the wire, handed to the MCP server or saved and opened again.
/// <para>The reflected rows of every setting live with the axes parts (<see cref="DrawnAxesFixtures"/>, parts "Figure"
/// and "Theme"); this class holds the facts a row cannot state: the drawn picture, the bytes at rest, older JSON.</para></summary>
public class FigureSettingsRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>A small chart that shows what a theme changes: two series (the cycle), a title, labels and ticks (the
    /// fonts and the text colour), a grid, a legend.</summary>
    private static Figure Chart(Theme theme) => Plt.Create()
        .WithTheme(theme)
        .WithTitle("Revenue")
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3, 4], [3.0, 1, 4, 1], s => s.Label = "north")
            .Plot([1.0, 2, 3, 4], [2.0, 3, 1, 4], s => s.Label = "south")
            .SetXLabel("quarter")
            .SetYLabel("units")
            .WithLegend())
        .Build();

    /// <summary>Every theme the library ships, by name.</summary>
    public static TheoryData<string> BuiltInThemes { get; } =
        new(typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Theme))
            .Select(p => p.Name));

    private static Theme BuiltIn(string name) => (Theme)typeof(Theme).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    [Theory]
    [MemberData(nameof(BuiltInThemes))]
    public void AFigureDrawnUnderABuiltInTheme_ReadBackFromJson_DrawsTheSameSvg(string theme)
    {
        var figure = Chart(BuiltIn(theme));

        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    /// <summary>A theme built from parts (a cycle of colours, line styles and markers, a font, a grid, a 3-D pane colour,
    /// an alarm palette) is data too: it comes back whole, not only the themes the library ships.</summary>
    [Fact]
    public void AFigureDrawnUnderACustomTheme_ReadBackFromJson_DrawsTheSameSvgAndKeepsItsName()
    {
        var theme = Theme.CreateFrom(Theme.Dark)
            .WithName("night-shift")
            .WithBackground(Color.FromHex("#101820"))
            .WithAxesBackground(Color.FromHex("#18242E"))
            .WithForegroundText(Color.FromHex("#D8E0E6"))
            .WithCycleColors(Color.FromHex("#F2AA4C"), Color.FromHex("#4CB6F2"))
            .WithFont(f => f with { Family = "serif", Size = 15, Weight = FontWeight.Bold })
            .WithGrid(g => g with { Visible = true, LineStyle = LineStyle.Dotted, Alpha = 0.4 })
            .WithPropCycler(new PropCyclerBuilder()
                .WithColors(Color.FromHex("#F2AA4C"), Color.FromHex("#4CB6F2"), Color.FromHex("#9BF24C"))
                .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
                .WithMarkerStyles(MarkerStyle.Circle, MarkerStyle.Square)
                .Build())
            .WithPane3DColor(Color.FromHex("#202A33"))
            .WithAlarmPalette(new AlarmPalette(Color.FromHex("#707070"), Color.FromHex("#FFB000"), Color.FromHex("#FF4000"), Color.FromHex("#606060")))
            .Build();
        var figure = Chart(theme);

        var read = RoundTrip(figure);

        Assert.Equal("night-shift", read.Theme.Name);
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    /// <summary>A grid switched off over a theme that draws one ("the theme's grid, but none") stays off: before the
    /// theme travelled the question never arose, since every figure read back on a theme without a grid.</summary>
    [Fact]
    public void AGridSwitchedOffUnderAGriddedTheme_ReadBackFromJson_DrawsNoGrid()
    {
        var figure = Plt.Create().WithTheme(Theme.Ggplot).AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2, 3], [1.0, 3, 2])
            .WithGrid(g => g with { Visible = false })).Build();

        string drawn = figure.ToSvg();
        string readBack = RoundTrip(figure).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ── a grid the author changed is a change over the grid of the theme the figure is drawn under ──────────────────

    /// <summary>What an author does to the grid of a themed chart, each as the change they wrote: one property, several, the
    /// grid on over a theme that draws none, the grid off over one that does.</summary>
    private static Func<GridStyle, GridStyle> GridChangeNamed(string name) => name switch
    {
        "alpha" => g => g with { Alpha = 0.4 },
        "style" => g => g with { Color = Colors.Red, LineStyle = LineStyle.Dashed, LineWidth = 2 },
        "on" => g => g with { Visible = true },
        _ => g => g with { Visible = false },
    };

    private static readonly string[] GridChangeNames = ["alpha", "style", "on", "off"];

    public static TheoryData<string, string> ThemesAndGridChanges { get; } = WithEveryGridChange();

    private static TheoryData<string, string> WithEveryGridChange()
    {
        var rows = new TheoryData<string, string>();
        foreach (var theme in typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Theme)))
            foreach (var change in GridChangeNames)
                rows.Add(theme.Name, change);
        return rows;
    }

    private static Figure GridChart(Theme theme, params Func<GridStyle, GridStyle>[] changes) => Plt.Create()
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 2, 3], [1.0, 3, 2]);
            foreach (var change in changes) ax.WithGrid(change);
        })
        .Build();

    /// <summary>Measured before the change: the JSON wrote the grid the change had produced under the figure's own theme and
    /// read it back as a constant, so a figure written under one theme and drawn under another kept the first theme's grid
    /// colour and line width. The change is the author's; the grid it changes is the theme's at the moment of drawing.</summary>
    [Theory]
    [MemberData(nameof(ThemesAndGridChanges))]
    public void AGridChangedUnderOneTheme_ReadBackFromJson_FollowsTheThemeItIsDrawnUnder(string theme, string change)
    {
        var figure = GridChart(BuiltIn(theme), GridChangeNamed(change));
        var readBack = RoundTrip(figure);
        var other = theme == nameof(Theme.Dark) ? Theme.Ggplot : Theme.Dark;

        figure.Theme = other;
        readBack.Theme = other;

        Assert.Equal(figure.ToSvg(), readBack.ToSvg());
    }

    /// <summary>"Visible" said under a theme that draws a grid equals the theme's value, and is still the author's choice: it
    /// must survive a theme that draws none. A change kept as the properties that differ from the theme's would lose it.</summary>
    [Fact]
    public void AFieldSetEqualToTheThemesValue_StillFollowsTheAuthorsChoice()
    {
        var figure = GridChart(Theme.Ggplot, g => g with { Visible = true });
        var readBack = RoundTrip(figure);

        figure.Theme = Theme.MatplotlibV2;
        readBack.Theme = Theme.MatplotlibV2;

        Assert.True(figure.SubPlots[0].GridUnder(Theme.MatplotlibV2).Visible);
        Assert.True(readBack.SubPlots[0].GridUnder(Theme.MatplotlibV2).Visible);
        Assert.Equal(figure.ToSvg(), readBack.ToSvg());
    }

    /// <summary>Measured before the change: the second <c>WithGrid</c> replaced the change of the first, so the colour the
    /// first asked for was gone from the grid the chart drew under a theme.</summary>
    [Fact]
    public void ASecondWithGrid_KeepsTheFirstChange()
    {
        var figure = GridChart(Theme.Ggplot, g => g with { Color = Colors.Red }, g => g with { Alpha = 0.3 });
        var readBack = RoundTrip(figure);

        foreach (var axes in new[] { figure.SubPlots[0], readBack.SubPlots[0] })
        {
            var drawn = axes.GridUnder(Theme.Dark);
            Assert.Equal(Colors.Red, drawn.Color);
            Assert.Equal(0.3, drawn.Alpha);
            Assert.True(drawn.Visible);
        }
    }

    /// <summary>Measured before the change: <c>ShowGrid(false)</c> under a theme that draws a grid drew it anyway (22 lines
    /// under Ggplot), because it set the axes' own grid without saying it was a change over the theme's. Showing and hiding
    /// the grid are the same change as <c>WithGrid</c> makes.</summary>
    [Fact]
    public void ShowGridFalse_UnderAGriddedTheme_DrawsNoGrid()
    {
        var shown = Plt.Create().WithTheme(Theme.Ggplot).AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2])).Build();
        var hidden = Plt.Create().WithTheme(Theme.Ggplot).AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]).ShowGrid(false)).Build();
        var viaWithGrid = GridChart(Theme.Ggplot, g => g with { Visible = false });

        Assert.False(hidden.SubPlots[0].GridUnder(Theme.Ggplot).Visible);
        Assert.NotEqual(shown.ToSvg(), hidden.ToSvg());
        Assert.Equal(viaWithGrid.ToSvg(), hidden.ToSvg());
        Assert.Equal(hidden.ToSvg(), RoundTrip(hidden).ToSvg());
    }

    /// <summary>The same verb the other way: <c>ShowGrid()</c> is the theme's grid switched on, as <c>WithGrid</c> says it.</summary>
    [Fact]
    public void ShowGrid_UnderAThemeWithoutOne_IsTheThemesGridSwitchedOn()
    {
        var viaShow = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]).ShowGrid()).Build();
        var viaWithGrid = GridChart(Theme.MatplotlibV2, g => g with { Visible = true });

        Assert.Equal(viaWithGrid.ToSvg(), viaShow.ToSvg());
    }

    /// <summary>JSON written before the change: a grid marked as the axes' own is the grid it draws whatever the theme is
    /// (the constant it always was), one that is not is read as it stands. Neither carries a <c>gridChange</c>.</summary>
    [Fact]
    public void AJsonWrittenBeforeTheGridChange_ReadsBackAsItAlwaysDid()
    {
        const string own = """{"width":400,"height":300,"subPlots":[{"grid":{"visible":true,"color":"#FF0000","lineWidth":2},"gridOverridesTheme":true,"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";
        const string plain = """{"width":400,"height":300,"subPlots":[{"grid":{"visible":true},"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";
        const string off = """{"width":400,"height":300,"subPlots":[{"grid":{"visible":false},"gridOverridesTheme":true,"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

        var ownGrid = Serializer.FromJson(own).SubPlots[0];
        foreach (var theme in new[] { Theme.Ggplot, Theme.MatplotlibV2, Theme.Dark })
        {
            Assert.Equal(new GridStyle { Visible = true, Color = Colors.Red, LineWidth = 2 }, ownGrid.GridUnder(theme));
            Assert.False(Serializer.FromJson(off).SubPlots[0].GridUnder(theme).Visible);
        }

        Assert.Equal(new GridStyle { Visible = true }, Serializer.FromJson(plain).SubPlots[0].GridUnder(Theme.Dark));
    }

    /// <summary>The figure's spacing (here the tight layout) decides where every subplot sits.</summary>
    [Fact]
    public void AFigureWithItsOwnSpacing_ReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = Plt.Create()
            .WithSubPlotSpacing(s => s with { MarginLeft = 90, HorizontalGap = 70 })
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2], [1.0, 2]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2], [2.0, 1]))
            .Build();

        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    /// <summary>The browser interactions are part of the SVG (the scripts it embeds, the tokens they read, whether it
    /// resizes with its container): read back, the same scripts and tokens are there.</summary>
    [Fact]
    public void AFigureWithBrowserInteraction_ReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = Plt.Create()
            .WithBrowserInteraction()
            .WithInteractionTheme(new InteractionTheme(HighlightOpacity: 0.15, TooltipOffsetX: 20))
            .WithResponsiveSvg(false)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2], s => s.Label = "a").WithLegend())
            .Build();

        string drawn = figure.ToSvg();

        Assert.Contains("data-mpl-highlight-opacity", drawn);
        Assert.Equal(drawn, RoundTrip(figure).ToSvg());
    }

    /// <summary>A figure that routes its interaction through a server names its chart in the SVG and embeds the
    /// dispatcher: a client that renders the published JSON draws the same SVG the server draws.</summary>
    [Fact]
    public void AFigureWithServerInteraction_ReadBackFromJson_DrawsTheSameSvg()
    {
        var figure = Plt.Create()
            .WithServerInteraction("live-1", i => i.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
            .Build();

        string drawn = figure.ToSvg();

        Assert.Contains("data-chart-id=\"live-1\"", drawn);
        Assert.Equal(drawn, RoundTrip(figure).ToSvg());
    }

    /// <summary>A figure that sets none of the figure settings writes the bytes it wrote before they reached the JSON.</summary>
    [Fact]
    public void AFigureThatSetsNoFigureSetting_WritesNoneOfTheirFields()
    {
        string json = Serializer.ToJson(DrawnAxesFixtures.Build(everyPropertySet: false));

        foreach (var field in new[] { "theme", "spacing", "chartId", "serverInteraction", "enableZoomPan", "enableLegendToggle",
                     "enableRichTooltips", "enableHighlight", "enableSelection", "enableDataCursor", "enable3DRotation",
                     "enableTreemapDrilldown", "enableSankeyHover", "interactionTheme", "responsiveSvg", "gridOverridesTheme",
                     "gridChange" })
        {
            Assert.False(Regex.IsMatch(json, $"\"{field}\":"), $"'{field}' was written for a figure that never set it: {json}");
        }
    }

    /// <summary>A JSON written before the figure settings reached it reads back at the settings a new figure has: the
    /// default theme (the very instance), the library spacing, a responsive SVG, no interaction.</summary>
    [Fact]
    public void AJsonWrittenBeforeTheFigureSettings_ReadsBackAtTheDefaults()
    {
        const string older = """{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";
        var fresh = new Figure();

        var read = Serializer.FromJson(older);

        Assert.Same(fresh.Theme, read.Theme);
        Assert.Equal(fresh.Spacing, read.Spacing);
        Assert.Equal(fresh.InteractionTheme, read.InteractionTheme);
        Assert.True(read.ResponsiveSvg);
        Assert.Null(read.ChartId);
        Assert.False(read.ServerInteraction || read.EnableZoomPan || read.EnableLegendToggle || read.EnableRichTooltips
            || read.EnableHighlight || read.EnableSelection || read.EnableDataCursor || read.Enable3DRotation
            || read.EnableTreemapDrilldown || read.EnableSankeyHover);
    }

    /// <summary>A theme written by hand (an agent's spec) may name only what it changes: every other property is the plain
    /// default theme's, the one the JSON writes a theme against.</summary>
    [Fact]
    public void AThemeThatNamesOnlyWhatItChanges_ReadsBackOverThePlainDefaultTheme()
    {
        const string json = """{"width":400,"height":300,"theme":{"axesBackground":"#E5E5E5"},"subPlots":[]}""";

        var theme = Serializer.FromJson(json).Theme;

        Assert.Equal(Color.FromHex("#E5E5E5"), theme.AxesBackground);
        Assert.Equal(Theme.Default.Name, theme.Name);
        Assert.Equal(Theme.Default.Background, theme.Background);
        Assert.Equal(Theme.Default.CycleColors, theme.CycleColors);
        Assert.Equal(Theme.Default.DefaultFont, theme.DefaultFont);
        Assert.Equal(Theme.Default.DefaultGrid, theme.DefaultGrid);
        Assert.Null(theme.PropCycler);
        Assert.Null(theme.DefaultSpacing);
        Assert.Equal(Theme.Default.Alarm, theme.Alarm);
    }

    /// <summary>A cycler written by hand may name only the lists it changes: a list left out reads as the cycler's own
    /// single default for it (the first tab10 colour, a solid line, no marker, 1.5 wide).</summary>
    [Theory]
    [InlineData("""{"markerStyles":["square","diamond"]}""", "None|Diamond")]
    [InlineData("""{"lineStyles":["dashed","dotted"]}""", "Dotted|None")]
    public void ACyclerThatNamesOnlySomeLists_ReadsTheOthersAsTheCyclersDefaults(string propCycler, string secondSlot)
    {
        string json = $$"""{"width":400,"height":300,"theme":{"propCycler":{{propCycler}}},"subPlots":[]}""";

        var cycler = Serializer.FromJson(json).Theme.PropCycler!;

        string[] expected = secondSlot.Split('|');
        var lineStyle = expected[0] == "None" ? LineStyle.Solid : Enum.Parse<LineStyle>(expected[0]);
        var marker = expected[1] == "None" ? MarkerStyle.None : Enum.Parse<MarkerStyle>(expected[1]);
        Assert.Equal(2, cycler.Length);
        Assert.Equal(new CycledProperties(Colors.Tab10Blue, lineStyle, marker, 1.5), cycler[1]);
    }

    /// <summary>A figure read back from JSON writes the same JSON again: the theme, the spacing and a grid switched off
    /// over the theme's are a fixed point of the round trip.</summary>
    [Fact]
    public void AFigureWithEveryFigureSettingSet_IsAFixedPointOfTheRoundTrip()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        figure.SubPlots[0].GridChange = new GridChange(Visible: false, Alpha: 0.3);
        string json = Serializer.ToJson(figure);

        Assert.Equal(json, Serializer.ToJson(Serializer.FromJson(json)));
    }
}
