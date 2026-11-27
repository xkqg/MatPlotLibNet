// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A style (<see cref="Plt.Style"/>) is a look, and the one thing that gives a figure its look is its
/// <see cref="Theme"/>: a figure built without a theme of its own wears the theme the current style describes. Untouched,
/// the style describes the builder's own default, so a caller who sets no style gets the figure they always got.</summary>
public class StyleToThemeTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [2.0, 1.0, 3.0];

    [Fact]
    public void WithNoStyle_TheFigureWearsTheBuildersDefaultTheme()
    {
        var figure = Plt.Create().Plot(X, Y).Build();

        Assert.Same(Theme.MatplotlibV2, figure.Theme);
    }

    /// <summary>The ggplot sheet is the ggplot theme's (<see cref="Theme.Ggplot"/>), as matplotlib 3.11.2's <c>ggplot</c> style reads
    /// with python: <c>figure.facecolor: 'white'</c>, <c>axes.facecolor: '#E5E5E5'</c>, <c>axes.grid: True</c>,
    /// <c>grid.color: 'white'</c>, and no <c>lines.linewidth</c>.</summary>
    [Fact]
    public void InsideAGgplotContext_TheFigureWearsGgplotsLook()
    {
        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(StyleSheet.Ggplot))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        var theme = figure.Theme;
        Assert.Equal(Colors.White, theme.Background);
        Assert.Equal(Color.FromHex("#E5E5E5"), theme.AxesBackground);
        Assert.True(theme.DefaultGrid.Visible);
        Assert.Equal(Colors.White, theme.DefaultGrid.Color);
        Assert.Equal(Theme.Ggplot.DefaultFont.Size, theme.DefaultFont.Size, 9);

        // The colours of the series stay those of the theme the style was laid over.
        Assert.Same(Theme.MatplotlibV2.PropCycler, theme.PropCycler);
        Assert.Equal(Theme.MatplotlibV2.CycleColors, theme.CycleColors);
    }

    /// <summary>The ggplot sheet names no width of a line (matplotlib's <c>ggplot</c> style names none either), so a line is drawn
    /// as wide as the theme the sheet is laid over draws it.</summary>
    [Fact]
    public void InsideAGgplotContext_ALineIsAsWideAsTheBaseThemeDrawsIt()
    {
        string over, bare;
        using (Plt.Style.Context(StyleSheet.Ggplot))
        {
            over = Plt.Create().Plot(X, Y).ToSvg();
        }

        bare = Plt.Create().Plot(X, Y).ToSvg();

        string Width(string svg) => svg.Elements("polyline").Select(polyline => polyline["stroke-width"]).FirstOrDefault(width => width is not null) ?? string.Empty;
        Assert.NotEmpty(Width(bare));
        Assert.Equal(Width(bare), Width(over));
    }

    /// <summary>The seaborn sheet is the seaborn theme's, whose text is dark grey (matplotlib 3.11.2's <c>seaborn-v0_8</c>:
    /// <c>text.color: '.15'</c>, which is <c>#262626</c>) and 11 pixels high.</summary>
    [Fact]
    public void AStyleThatNamesTheTextColour_ColoursTheText()
    {
        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(StyleSheet.Seaborn))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        Assert.Equal(Color.FromHex("#262626"), figure.Theme.ForegroundText);
        Assert.Equal(Color.FromHex("#262626"), figure.Theme.DefaultFont.Color);
        Assert.Equal(Theme.Seaborn.DefaultFont.Size, figure.Theme.DefaultFont.Size, 9);
        Assert.Equal(Colors.White, figure.Theme.Background);
    }

    [Fact]
    public void AStyleMadeFromATheme_GivesThatThemesLook()
    {
        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(StyleSheet.Dark))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        Assert.Equal(Theme.Dark.Background, figure.Theme.Background);
        Assert.Equal(Theme.Dark.AxesBackground, figure.Theme.AxesBackground);
        Assert.Equal(Theme.Dark.DefaultGrid.Color, figure.Theme.DefaultGrid.Color);
        Assert.Equal(Theme.Dark.DefaultGrid.Alpha, figure.Theme.DefaultGrid.Alpha);

        // The dark sheet names the default family, and a parameter at its default describes the base theme.
        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Family, figure.Theme.DefaultFont.Family);
    }

    [Fact]
    public void AnExplicitTheme_WinsOverTheStyle()
    {
        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(StyleSheet.Ggplot))
        {
            figure = Plt.Create().WithTheme(Theme.Dark).Plot(X, Y).Build();
        }

        Assert.Same(Theme.Dark, figure.Theme);
    }

    [Fact]
    public void AStyleThatRestatesTheDefaults_ChangesNothing()
    {
        var sheet = new StyleSheet("defaults", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = 10.0,
            [RcParamKeys.LinesLineWidth] = 1.5,
            [RcParamKeys.AxesGrid] = false,
        });

        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(sheet))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        Assert.Same(Theme.MatplotlibV2, figure.Theme);
    }

    [Fact]
    public void AKeyOfTheWrongType_IsLeftToTheTheme()
    {
        // A sheet is a dictionary anyone can fill; a value that is not what the key holds says nothing about the look.
        var sheet = new StyleSheet("odd", new Dictionary<string, object>
        {
            [RcParamKeys.FontSize] = "large",
            [RcParamKeys.AxesFaceColor] = Color.FromHex("#123456"),
        });

        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(sheet))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Size, figure.Theme.DefaultFont.Size);
        Assert.Equal(Color.FromHex("#123456"), figure.Theme.AxesBackground);
    }

    [Fact]
    public void AWholeNumber_IsANumber()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.LinesLineWidth, 3);
        parameters.Set(RcParamKeys.GridAlpha, 0.25);

        var theme = parameters.ThemeOver(Theme.MatplotlibV2);

        Assert.Equal(3.0.PointsInPixels, theme.Looks.Lines.Width);
        Assert.Equal(0.25, theme.DefaultGrid.Alpha);
    }

    /// <summary>A style's line width is the theme's, not the cycler's: the cycler is left as it is, colours, dashes and markers,
    /// and a series of a slot whose cycler names no width is drawn as wide as the style says.</summary>
    [Fact]
    public void ALineWidthOverAThemeWithACycler_KeepsTheCyclerAndWidensTheLine()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Blue)
            .WithLineStyles(LineStyle.Dashed)
            .WithMarkerStyles(MarkerStyle.Circle)
            .Build();
        var baseTheme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.LinesLineWidth, 2.5);

        var theme = parameters.ThemeOver(baseTheme);

        Assert.Same(cycler, theme.PropCycler);
        Assert.Equal(2.5.PointsInPixels, theme.SeriesSlot(1).LineWidthOf(LineWidthRule.Line));
    }

    [Fact]
    public void AStyleThatNamesAFontFamily_SetsTheFamily()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.FontFamily, "Georgia, serif");

        Assert.Equal("Georgia, serif", parameters.ThemeOver(Theme.MatplotlibV2).DefaultFont.Family);
    }

    [Fact]
    public void TheTypedSetters_WriteTheirKeys()
    {
        var parameters = new RcParams { FontSize = 12.0, LinesLineWidth = 2.0 };

        Assert.Equal(12.0, parameters.FontSize);
        Assert.Equal(2.0, parameters.LinesLineWidth);
        Assert.Equal(12.0.PointsInPixels, parameters.ThemeOver(Theme.MatplotlibV2).DefaultFont.Size);
    }

    /// <summary>A style states a size and a width in points, as matplotlib's rcParams do (<c>font.size</c>,
    /// <c>grid.linewidth</c>), and the theme it describes holds pixels, the unit everything is drawn in, at 100 / 72 to the
    /// point, as the matplotlib themes turn their own rcParams. Read as pixels, the default 10 stood for the base theme's 13.89
    /// pixels while 12 gave 12 pixels, smaller text for a larger size; and a grid of 1 was thinner than the v2 grid of 0.8.</summary>
    [Fact]
    public void AStylesSizesAndWidths_AreMatplotlibsPoints_InPixels()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.FontSize, 12.0);
        parameters.Set(RcParamKeys.GridLineWidth, 1.0);

        var theme = parameters.ThemeOver(Theme.MatplotlibV2);

        Assert.Equal(12.0.PointsInPixels, theme.DefaultFont.Size);
        Assert.True(theme.DefaultFont.Size > Theme.MatplotlibV2.DefaultFont.Size);
        Assert.Equal(1.0.PointsInPixels, theme.DefaultGrid.LineWidth);
        Assert.True(theme.DefaultGrid.LineWidth > Theme.MatplotlibV2.DefaultGrid.LineWidth);
    }

    /// <summary>A style made from a theme states that theme's size and grid width in points, so laid over another theme it
    /// gives back the widths of the theme it was made from: the plain theme's 0.8 pixel grid, not the v2 theme's 1.11.</summary>
    [Fact]
    public void AStyleMadeFromATheme_GivesBackThatThemesGridWidthAndFontSize()
    {
        var sheet = StyleSheet.FromTheme(Theme.Default);

        Assert.Equal(Theme.Default.DefaultGrid.LineWidth.PixelsInPoints, sheet.Parameters[RcParamKeys.GridLineWidth]);
        Assert.Equal(Theme.Default.DefaultFont.Size.PixelsInPoints, sheet.Parameters[RcParamKeys.FontSize]);

        MatPlotLibNet.Models.Figure figure;
        using (Plt.Style.Context(sheet))
        {
            figure = Plt.Create().Plot(X, Y).Build();
        }

        Assert.Equal(Theme.Default.DefaultGrid.LineWidth, figure.Theme.DefaultGrid.LineWidth);
        Assert.Equal(Theme.Default.DefaultFont.Size, figure.Theme.DefaultFont.Size, 12);
    }

    /// <summary>A style made from a theme that names a line width (<see cref="SeriesLineLook.Width"/>) carries it as
    /// <c>lines.linewidth</c>, in points, so laid over another theme it gives that width back: the classic theme's 1 pt (1.39 px), and
    /// a theme a style of 4 pt was laid over (5.56 px). Before, the sheet left the key out and the width of the theme it was laid over
    /// stood.</summary>
    [Theory]
    [InlineData("classic", "Default")]
    [InlineData("classic", "Dark")]
    [InlineData("classic", "MatplotlibV2")]
    [InlineData("4 pt", "MatplotlibV2")]
    [InlineData("4 pt", "MatplotlibClassic")]
    public void AStyleMadeFromAThemeThatNamesALineWidth_GivesItBack_LaidOverAnotherTheme(string from, string over)
    {
        var fourPoints = new RcParams();
        fourPoints.Set(RcParamKeys.LinesLineWidth, 4.0);
        var source = from == "classic" ? Theme.MatplotlibClassic : fourPoints.ThemeOver(Theme.Default);
        var baseTheme = over.AsTheme();
        var parameters = new RcParams();
        foreach (var (key, value) in StyleSheet.FromTheme(source).Parameters) parameters.Set(key, value);

        var theme = parameters.ThemeOver(baseTheme);

        Assert.NotNull(theme.Looks.Lines.Width);
        Assert.Equal(source.Looks.Lines.Width!.Value, theme.Looks.Lines.Width.Value, 12);
    }

    /// <summary>The sheet states a theme's line width in matplotlib's points, and only where the theme names one: a library theme,
    /// which names none, leaves the key out, so laid over a theme it leaves that theme's width.</summary>
    [Fact]
    public void AStyleMadeFromATheme_SaysItsLineWidthInPoints_OnlyWhereTheThemeNamesOne()
    {
        Assert.Equal(1.5, (double)StyleSheet.FromTheme(Theme.MatplotlibV2).Parameters[RcParamKeys.LinesLineWidth], 12);
        Assert.Equal(1.0, (double)StyleSheet.FromTheme(Theme.MatplotlibClassic).Parameters[RcParamKeys.LinesLineWidth], 12);
        Assert.False(StyleSheet.FromTheme(Theme.Default).Parameters.ContainsKey(RcParamKeys.LinesLineWidth));
    }

    [Fact]
    public void AnEmptySetOfParameters_DescribesTheBaseTheme()
    {
        Assert.Same(Theme.Dark, new RcParams().ThemeOver(Theme.Dark));
    }

    [Fact]
    public void TwoBuildsUnderOneStyle_WearOneTheme()
    {
        // The parameters did not change between the builds, so the theme they describe is laid over the base once and
        // every later build wears it again: reading the parameters is not paid per figure.
        MatPlotLibNet.Models.Figure first, second;
        using (Plt.Style.Context(StyleSheet.Ggplot))
        {
            first = Plt.Create().Plot(X, Y).Build();
            second = Plt.Create().Plot(X, Y).Build();
        }

        Assert.NotSame(Theme.MatplotlibV2, first.Theme);
        Assert.Same(first.Theme, second.Theme);
    }

    [Fact]
    public void AParameterSetBetweenTwoBuilds_DressesTheSecond()
    {
        MatPlotLibNet.Models.Figure first, second, third;
        using (Plt.Style.Context(StyleSheet.Ggplot))
        {
            first = Plt.Create().Plot(X, Y).Build();
            RcParams.Current.Set(RcParamKeys.GridAlpha, (object)0.25);
            second = Plt.Create().Plot(X, Y).Build();
            RcParams.Current.FontSize = 13.0;
            third = Plt.Create().Plot(X, Y).Build();
        }

        Assert.NotEqual(0.25, first.Theme.DefaultGrid.Alpha);
        Assert.Equal(0.25, second.Theme.DefaultGrid.Alpha);
        Assert.Equal(13.0.PointsInPixels, third.Theme.DefaultFont.Size);
        Assert.Equal(0.25, third.Theme.DefaultGrid.Alpha);
    }

    [Fact]
    public void OneSetOfParametersOverTwoBaseThemes_DescribesATheme_OverEachBase()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.AxesFaceColor, Color.FromHex("#123456"));

        var overDefault = parameters.ThemeOver(Theme.MatplotlibV2);
        var overDark = parameters.ThemeOver(Theme.Dark);

        Assert.Equal(Theme.MatplotlibV2.Background, overDefault.Background);
        Assert.Equal(Theme.Dark.Background, overDark.Background);
        Assert.Equal(Color.FromHex("#123456"), overDark.AxesBackground);
        Assert.Same(overDark, parameters.ThemeOver(Theme.Dark));
    }
}

