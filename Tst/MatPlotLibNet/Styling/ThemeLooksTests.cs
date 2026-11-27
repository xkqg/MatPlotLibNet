// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A theme's looks: one value per concern (ticks, patches, colour bars, dots and so on), each with the value the
/// library's own themes wear, and the rules that read them.</summary>
public class ThemeLooksTests
{
    private static readonly Color Red = Colors.Red;

    /// <summary>The library's theme is the answer of a drawing that has no theme of its own: every question a member used to answer
    /// for a null theme (no matplotlib z-order, no matplotlib autolimit, no ticks at the boxes, no marks on a colour bar, markers that do
    /// not fill their box, text not measured like Agg, the library's tick and label looks) the default theme answers alike.</summary>
    [Fact]
    public void TheDefaultTheme_AnswersEveryQuestionANullThemeAnswered()
    {
        var looks = Theme.Default.Looks;

        Assert.Same(ThemeLooks.Library, looks);
        Assert.Null(looks.Frame.AxisZOrder);
        Assert.Null(looks.Range.Autolimit);
        Assert.False(looks.Box.Standing.TicksAtBoxes);
        Assert.False(looks.ColorBar.TickMarks);
        Assert.False(looks.Markers.FillTheirBox);
        Assert.False(looks.Text.MeasuresLikeAgg);
    }

    [Fact]
    public void TheLibraryLooks_AreTheLibraryValueOfEveryConcern()
    {
        var looks = ThemeLooks.Library;

        foreach (var property in typeof(ThemeLooks).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var library = property.PropertyType.GetProperty("Library", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (library is null || property.Name == nameof(ThemeLooks.Scatter))
            {
                continue;
            }

            Assert.Same(library.GetValue(null), property.GetValue(looks));
        }

        Assert.Null(looks.Scatter);
    }

    [Theory]
    [MemberData(nameof(LibraryThemes))]
    public void EveryLibraryTheme_WearsTheLibraryLooks(Theme theme) => Assert.Same(ThemeLooks.Library, theme.Looks);

    public static TheoryData<Theme> LibraryThemes => new() { Theme.Default, Theme.Dark, Theme.Seaborn, Theme.OpsNight, Theme.Nord };

    [Fact]
    public void TheMatplotlibThemes_ShareEveryLookTheClassicStyleDoesNotChange()
    {
        var v2 = Theme.MatplotlibV2.Looks;
        var classic = Theme.MatplotlibClassic.Looks;

        foreach (string name in new[] { "Dots", "Verbs", "Density", "Spectrogram", "Violin", "Wedges", "Table", "ColorBar", "VectorField", "Treemap", "Map", "Raster", "Figure" })
        {
            var property = typeof(ThemeLooks).GetProperty(name)!;
            Assert.Same(property.GetValue(v2), property.GetValue(classic));
        }

        Assert.NotEqual(v2.Ticks, classic.Ticks);
        Assert.NotEqual(v2.Patch, classic.Patch);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void ATheme_ReadsBackFromItsJsonForm_WithTheSameForm(Theme theme)
    {
        var written = ThemeDto.From(theme);

        var read = written.ToTheme();

        Assert.Equal(written, ThemeDto.From(read));
        // A tick ladder carries a staircase it works out from its steps, a new array for each ladder made, so it is compared by
        // what it was made of: the steps, bins and least count the written form holds, compared above.
        Assert.Equal(theme.Looks with { Ticks = theme.Looks.Ticks with { Ladder = null } }, read.Looks with { Ticks = read.Looks.Ticks with { Ladder = null } });
    }

    public static TheoryData<Theme> AllThemes => new() { Theme.Default, Theme.Dark, Theme.MatplotlibV2, Theme.MatplotlibClassic };

    // ── patches ──

    [Fact]
    public void ASurfaceOutline_IsDrawnAsAsked_ElseWhereAnEdgeIsNamedOrTheLookOutlines()
    {
        var notForced = new PatchLook { LikeCollections = true, ForceEdge = false, EdgeColor = Colors.Black, EdgeWidth = 2 };

        Assert.Null(PatchLook.Library.SurfaceEdgeOver(shown: false, given: Red));
        Assert.Null(notForced.SurfaceEdgeOver(shown: null, given: null));
        Assert.Equal(new StrokeStyle(Red, 2, LineStyle.Solid), notForced.SurfaceEdgeOver(shown: null, given: Red));
        Assert.Equal(new StrokeStyle(Colors.Black, 2, LineStyle.Solid), notForced.SurfaceEdgeOver(shown: true, given: null));
        Assert.Equal(new StrokeStyle(Colors.Black.WithAlpha(80), 0.5, LineStyle.Solid), PatchLook.Library.SurfaceEdgeOver(shown: null, given: null));
    }

    [Fact]
    public void APatchEdge_IsDecidedOnlyByALookThatFillsLikeCollections()
    {
        var v2 = new PatchLook { LikeCollections = true, ForceEdge = false, EdgeColor = Colors.Black, EdgeWidth = 1.5 };
        var classic = v2 with { ForceEdge = true };

        Assert.Null(PatchLook.Library.EdgeOver(new(Red, 2)));
        Assert.Equal(PatchEdge.None, v2.EdgeOver(new(null, null)));
        Assert.Equal(new PatchEdge(Red, 1.5), v2.EdgeOver(new(Red, null)));
        Assert.Equal(new PatchEdge(Colors.Black, 1.5), classic.EdgeOver(new(null, null)));
        Assert.Equal(PatchEdge.None, classic.EdgeOver(new(null, 0)));
        Assert.Equal(new PatchEdge(Red, 0.5), (v2 with { EdgeWidth = null }).EdgeOver(new(Red, null)));
    }

    [Fact]
    public void APatchOutlineDrawnAsALine_IsAsWideAsTheLooksEdge_ElseTheKindsOwn()
    {
        Assert.Equal(1.25, new PatchLook { EdgeWidth = 1.25 }.OutlineWidthOf(LineWidthRule.LineCollection));
        Assert.Equal(LineWidthRule.LineCollection.Resting, PatchLook.Library.OutlineWidthOf(LineWidthRule.LineCollection));
    }

    [Fact]
    public void AHatch_IsInItsGivenColour_ElseByTheLooksRule_ElseLeftToTheBackend()
    {
        var follows = new PatchLook { Hatch = new HatchColorRule(FollowsEdge: true, Colors.Black) };
        var black = new PatchLook { Hatch = new HatchColorRule(FollowsEdge: false, Colors.Black) };

        static HatchFill Cross(Color? colour) => new(HatchPattern.Cross, colour);

        Assert.Equal(Cross(Red), PatchLook.Library.HatchOver(Cross(Red), new HatchGround(Colors.Blue)));
        Assert.Equal(Cross(Colors.Blue), follows.HatchOver(Cross(null), new HatchGround(Colors.Blue)));
        Assert.Equal(Cross(Colors.Black), follows.HatchOver(Cross(null), new HatchGround(Colors.Transparent)));
        Assert.Equal(Cross(Colors.Black), follows.HatchOver(Cross(null), new HatchGround(null)));
        Assert.Equal(Cross(Colors.Black), black.HatchOver(Cross(null), new HatchGround(Colors.Blue)));
    }

    /// <summary>The library's own rule, for a bar and a patch alike: the edge its author gave, else none over a fill that can be seen
    /// (the backend draws the colour that reads best against it), else black; at the opacity given, as every hatch colour is.</summary>
    [Fact]
    public void UnderTheLibrarysLook_AHatchFollowsTheEdgeGiven_ElseReadsAgainstAVisibleFill_ElseIsBlack()
    {
        static HatchFill Cross(Color? colour) => new(HatchPattern.Cross, colour);

        Assert.Equal(Cross(Colors.Green), PatchLook.Library.HatchOver(Cross(null), new HatchGround(Colors.Blue, Colors.Green, Colors.Blue)));
        Assert.Equal(Cross(null), PatchLook.Library.HatchOver(Cross(null), new HatchGround(Colors.Blue, null, Colors.Blue)));
        Assert.Equal(Cross(null), PatchLook.Library.HatchOver(Cross(null), new HatchGround(null, Colors.Transparent, Colors.Blue)));
        Assert.Equal(Cross(Colors.Black), PatchLook.Library.HatchOver(Cross(null), new HatchGround(null, null, Colors.Transparent)));
        Assert.Equal(Cross(Colors.Black.WithOpacity(0.5)), PatchLook.Library.HatchOver(Cross(null), new HatchGround(null, null, Colors.Transparent, 0.5)));
        Assert.Equal(Cross(Red.WithOpacity(0.5)), PatchLook.Library.HatchOver(Cross(Red), new HatchGround(null, null, Colors.Blue, 0.5)));
    }

    [Fact]
    public void ThePatchLookOfTheLibrary_OutlinesASurfaceAtRestAndDecidesNoPatchEdge()
    {
        Assert.False(PatchLook.Library.LikeCollections);
        Assert.True(PatchLook.Library.ForceEdge);
    }

    // ── lines, dots, images, colour maps ──

    [Fact]
    public void AnErrorBarCap_ReachesAsFarAsGiven_ElseAsTheLook_ElseTheSeriesOwn()
    {
        Assert.Equal(2, SeriesLineLook.Library.CapSizeOver(2, 5));
        Assert.Equal(0, new SeriesLineLook { ErrorBarCapSize = 0 }.CapSizeOver(null, 5));
        Assert.Equal(5, SeriesLineLook.Library.CapSizeOver(null, 5));
    }

    [Fact]
    public void ASeriesText_IsAsLargeAsGiven_ElseTheFontWhereTheLookSaysSo_ElseTheSeriesOwn()
    {
        Assert.Equal(9, Theme.MatplotlibV2.SeriesTextSizeOver(9, 11));
        Assert.Equal(Theme.MatplotlibV2.DefaultFont.Size, Theme.MatplotlibV2.SeriesTextSizeOver(null, 11));
        Assert.Equal(11, Theme.Default.SeriesTextSizeOver(null, 11));
    }

    [Fact]
    public void AnImage_IsScaledAsGiven_ElseAsTheLook_ElseNearest()
    {
        Assert.Equal("bicubic", ImageLook.Library.InterpolationOver("bicubic"));
        Assert.Equal("bilinear", new ImageLook { Interpolation = "bilinear" }.InterpolationOver(null));
        Assert.Equal(NearestInterpolation.Instance.Name, ImageLook.Library.InterpolationOver(null));
    }

    [Fact]
    public void AColourMap_IsTheOneGiven_ElseTheLooks_ElseViridis()
    {
        Assert.Same(ColorMaps.Plasma, ColorMapLook.Library.Of(ColorMaps.Plasma));
        Assert.Same(ColorMaps.Jet, new ColorMapLook { Default = ColorMaps.Jet }.Of(null));
        Assert.Same(ColorMaps.Viridis, ColorMapLook.Library.Of(null));
    }

    /// <summary>Every look the matplotlib themes switch on, by the name of the property that reads it.</summary>
    public static TheoryData<string, Func<Theme, bool>> Looks { get; } = new()
    {
        { "ColorBar.ExtendTriangles", theme => theme.Looks.ColorBar.ExtendTriangles },
        { "ColorBar.StepPerColour", theme => theme.Looks.ColorBar.StepPerColour },
        { "ColorBar.TickMarks", theme => theme.Looks.ColorBar.TickMarks },
        { "ColorMap.CountsFromTheLeastDrawn", theme => theme.Looks.ColorMap.CountsFromTheLeastDrawn },
        { "ColorMap.LikeLogNorm", theme => theme.Looks.ColorMap.LikeLogNorm },
        { "Cycle.GroupsWearTheCycle", theme => theme.Looks.Cycle.GroupsWearTheCycle },
        { "Dots.LikeSeaborn", theme => theme.Looks.Dots.LikeSeaborn },
        { "Figure.LegendListsEveryEntry", theme => theme.Looks.Figure.LegendListsEveryEntry },
        { "Image.LikeImshow", theme => theme.Looks.Image.LikeImshow },
        { "Map.LikeCartopy", theme => theme.Looks.Map.LikeCartopy },
        { "Markers.FillTheirBox", theme => theme.Looks.Markers.FillTheirBox },
        { "Range.PatchesLikeAddPatch", theme => theme.Looks.Range.PatchesLikeAddPatch },
        { "Raster.StrokesOnPixelGrid", theme => theme.Looks.Raster.StrokesOnPixelGrid },
        { "Spectrogram.LikeSpecgram", theme => theme.Looks.Spectrogram.LikeSpecgram },
        { "Text.SeriesTextAtFontSize", theme => theme.Looks.Text.SeriesTextAtFontSize },
        { "TickLabels.UnicodeMinus", theme => theme.Looks.TickLabels.UnicodeMinus },
        { "Ticks.MinorGridLikeMajor", theme => theme.Looks.Ticks.MinorGridLikeMajor },
        { "Ticks.MinorsByLocator", theme => theme.Looks.Ticks.MinorsByLocator },
        { "Treemap.LikeSquarify", theme => theme.Looks.Treemap.LikeSquarify },
        { "VectorField.QuiverAutoScale", theme => theme.Looks.VectorField.QuiverAutoScale },
    };

    [Theory]
    [MemberData(nameof(Looks))]
    public void ALook_IsOnForTheMatplotlibThemesOnly_AndSurvivesAClone_TheDtoAndTheJson(string name, Func<Theme, bool> look)
    {
        Assert.True(look(Theme.MatplotlibV2), name);
        Assert.True(look(Theme.MatplotlibClassic), name);
        Assert.False(look(Theme.Default), name);
        Assert.False(look(Theme.Dark), name);
        Assert.True(look(Theme.MatplotlibV2.Cloned()), name);
        Assert.True(look(Theme.MatplotlibV2.ThroughDto()), name);
        Assert.True(look(Theme.MatplotlibClassic.ThroughJson()), name);
        Assert.False(look(Theme.Default.ThroughDto()), name);
        Assert.False(look(Theme.Dark.ThroughJson()), name);
    }

    /// <summary>The distances, in points, the matplotlib themes name for a look: the default style's and the classic style's. The library's own
    /// themes name none.</summary>
    public static TheoryData<string, Func<Theme, double?>, double, double> Distances { get; } = new()
    {
        { "Text.TitlePad", theme => theme.Looks.Text.TitlePad, 6.0, 5.0 },
        { "Text.LabelPad", theme => theme.Looks.Text.LabelPad, 4.0, 5.0 },
        { "Ticks.MinorWidth", theme => theme.Looks.Ticks.MinorWidth, 0.6, 0.5 },
        { "Ticks.MinorPad", theme => theme.Looks.Ticks.MinorPad, 3.4, 4.0 },
    };

    [Theory]
    [MemberData(nameof(Distances))]
    public void ADistance_IsWhatEachMatplotlibThemeNames_TheLibrarysNone_AndSurvivesAClone_TheDtoAndTheJson(
        string name, Func<Theme, double?> distance, double v2Points, double classicPoints)
    {
        double v2 = v2Points.PointsInPixels, classic = classicPoints.PointsInPixels;

        Assert.True(distance(Theme.MatplotlibV2).HasValue, $"the default style names no {name}");
        Assert.Equal(v2, distance(Theme.MatplotlibV2)!.Value, 9);
        Assert.Equal(classic, distance(Theme.MatplotlibClassic)!.Value, 9);
        Assert.Null(distance(Theme.Default));
        Assert.Equal(v2, distance(Theme.MatplotlibV2.Cloned())!.Value, 9);
        Assert.Equal(v2, distance(Theme.MatplotlibV2.ThroughDto())!.Value, 9);
        Assert.Equal(classic, distance(Theme.MatplotlibClassic.ThroughDto())!.Value, 9);
        Assert.Equal(classic, distance(Theme.MatplotlibClassic.ThroughJson())!.Value, 9);
        Assert.Null(distance(Theme.Default.ThroughDto()));
    }
}
