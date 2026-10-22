// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The figure's own settings travel through the spec reader as its axes do: a theme, a spacing, the interaction
/// settings. An agent writes a theme by hand, so its colours may be names and its enum values may be misspelt; the
/// reader takes the first and refuses the second with what it accepts, as it does for every other part.</summary>
[Collection("ChartServicesGlobalState")]
public class SpecFigureSettingsTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    /// <summary>A spec with <paramref name="theme"/> (a JSON object) as its theme, and one line.</summary>
    private static string Spec(string theme) =>
        $$"""{"width":400,"height":300,"theme":{{theme}},"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

    [Theory]
    [InlineData("""{"defaultFont":{"weight":"heavy"}}""", "heavy", "Bold")]
    [InlineData("""{"defaultFont":{"slant":"slanted"}}""", "slanted", "Oblique")]
    [InlineData("""{"defaultGrid":{"visible":true,"lineStyle":"wavy"}}""", "wavy", "Dotted")]
    [InlineData("""{"defaultGrid":{"visible":true,"axis":"z"}}""", "z", "Both")]
    [InlineData("""{"propCycler":{"lineStyles":["solid","wavy"]}}""", "wavy", "Dashed")]
    [InlineData("""{"propCycler":{"markerStyles":["circle","blob"]}}""", "blob", "Diamond")]
    public void AMisspelledValueInATheme_IsRefusedWithTheAcceptedValues(string theme, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(theme))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    [Fact]
    public void AMisspelledValueInACyclerList_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"propCycler":{"markerStyles":["circle","blob"]}}"""))));

        Assert.Contains("theme.propCycler.markerStyles[1]", refusal.Message);
    }

    /// <summary>An item that is not a string is left to the strict pass, as a single value of the wrong shape is: it is
    /// refused there by path.</summary>
    [Fact]
    public void ACyclerListItemThatIsNotAString_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"propCycler":{"markerStyles":["circle",true]}}"""))));

        Assert.Contains("theme.propCycler.markerStyles[1]", refusal.Message);
    }

    [Fact]
    public void AcceptedCyclerValues_ReachTheTheme_InAnyCase()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"propCycler":{"lineStyles":["DASHED","dotted"],"markerStyles":["square"]}}""")));

        var cycler = figure.Theme.PropCycler!;
        Assert.Equal(LineStyle.Dashed, cycler[0].LineStyle);
        Assert.Equal(LineStyle.Dotted, cycler[1].LineStyle);
        Assert.Equal(MarkerStyle.Square, cycler[0].MarkerStyle);
    }

    /// <summary>Every colour of a theme takes a CSS4 name or a short hex, wherever it sits: the theme's own grounds, its
    /// cycle, its alarm palette.</summary>
    [Fact]
    public void ColourNamesInATheme_AreNormalised()
    {
        var figure = Reader.Read(new ChartSpec(Spec(
            """{"background":"black","foregroundText":"white","axesBackground":"#333","cycleColors":["red","#00f"],"alarm":{"resting":"grey","warning":"orange","critical":"red","unknown":"silver"}}""")));

        var theme = figure.Theme;
        Assert.Equal(Color.FromHex("#000000"), theme.Background);
        Assert.Equal(Color.FromHex("#FFFFFF"), theme.ForegroundText);
        Assert.Equal(Color.FromHex("#333333"), theme.AxesBackground);
        Assert.Equal([Color.FromHex("#FF0000"), Color.FromHex("#0000FF")], theme.CycleColors);
        Assert.Equal(Color.FromHex("#FFA500"), theme.Alarm.Warning);
        Assert.Equal(Color.FromHex("#C0C0C0"), theme.Alarm.Unknown);
    }

    [Fact]
    public void AnUnknownColourInAThemesCycle_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"cycleColors":["red","reddish"]}"""))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("theme.cycleColors[1]", refusal.Message);
    }

    /// <summary>A built-in theme travels as the data it is: what a figure under it writes, the reader reads back to the
    /// same theme.</summary>
    [Fact]
    public void ABuiltInThemeWrittenByAFigure_PassesTheSpecReader()
    {
        var figure = Plt.Create().WithTheme(Theme.Ggplot).Plot([1.0, 2], [1.0, 2]).Build();

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    [Fact]
    public void TheSchema_ListsTheThemeAndSpacingFields()
    {
        string schema = new ChartSchemaDescription(new ChartTypeCatalog()).Describe(null);

        foreach (var (section, field) in new[]
                 {
                     ("Theme fields", "cycleColors: string (colour)[]"),
                     ("Theme fields", "propCycler: object"),
                     ("Spacing fields", "tightLayout: boolean"),
                     ("Interaction theme fields", "highlightOpacity: number"),
                 })
        {
            int at = schema.IndexOf(section, StringComparison.Ordinal);
            Assert.True(at >= 0, $"'{section}' is not in the schema.");
            Assert.Contains(field, schema[at..]);
        }

        Assert.Contains(schema.Split(Environment.NewLine), line => line.TrimStart().StartsWith("theme:", StringComparison.Ordinal) && line.Contains("default", StringComparison.Ordinal));
    }

    /// <summary>The fixture this project links is the core contract's: its theme rows reach this reader too.</summary>
    [Fact]
    public void TheLinkedContract_HoldsTheFigureAndItsTheme()
    {
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "Figure");
        Assert.Contains(DrawnAxesFixtures.Parts, part => part.Name == "Theme");
    }
}
