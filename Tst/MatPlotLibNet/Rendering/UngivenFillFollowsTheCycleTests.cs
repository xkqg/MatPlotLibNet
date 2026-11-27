// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The fill of a span or a patch that its author gave no colour is the first colour of the theme's cycle (matplotlib's
/// <c>C0</c>, which names the first colour of <c>axes.prop_cycle</c> whatever it holds), not a blue the library keeps for every
/// theme; a colour that was given is drawn as given, a blue that equals the old default included. The JSON writes the colour given and
/// nothing else, so a figure draws the same picture before and after a round trip under every theme.</summary>
public class UngivenFillFollowsTheCycleTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    private static Color FirstColourOf(Theme theme) => theme.SeriesSlot(0).CycleColor;

    private static Color SpanFillUnder(Theme theme, Action<SpanRegion>? configure = null)
    {
        var axes = new Axes();
        configure?.Invoke(axes.AxVSpan(2, 5));
        if (configure is null) axes.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        CartesianParts.PartFactory.Spans(axes, Plot, ctx, theme, new DataTransform(0, 10, 0, 10, Plot), new DataRange(0, 10, 0, 10)).Render();
        return (ctx.Of<DrawRectangleCall>().Single().Fill).GetValueOrDefault();
    }

    private static Color PatchFaceUnder(Theme theme, FilledPatch patch)
    {
        var axes = new Axes();
        axes.AddPatch(patch);
        var ctx = new RecordingRenderContext();
        CartesianParts.PartFactory.Patches(axes, Plot, ctx, theme, new DataTransform(0, 10, 0, 10, Plot), axes.RangeOf(theme).PatchesMayReachPastPlot).Render();
        return (ctx.Of<DrawPathCall>().Single().Shape).Fill.GetValueOrDefault();
    }

    private static Rectangle Rect() => new(new(1, 1), 2, 1);

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void ASpanGivenNoColour_IsFilledInTheFirstColourOfTheThemesCycle(string name)
    {
        var theme = name.AsTheme();

        var fill = SpanFillUnder(theme);

        Assert.Equal(FirstColourOf(theme).WithAlpha(fill.A), fill);
        Assert.Equal(byte.MaxValue.ScaledTruncating(0.2), fill.A);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void ASpanGivenAColour_IsFilledInIt_WhateverTheTheme_BlueOfTheOldDefaultIncluded(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(Colors.Tab10Blue.WithAlpha(byte.MaxValue.ScaledTruncating(0.2)), SpanFillUnder(theme, s => s.Color = Colors.Tab10Blue));
        Assert.Equal(Colors.Red.WithAlpha(byte.MaxValue.ScaledTruncating(0.2)), SpanFillUnder(theme, s => s.Color = Colors.Red));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void ASpanBorderAndLabel_FollowTheSameColour(string name)
    {
        var theme = name.AsTheme();
        var axes = new Axes();
        var span = axes.AxVSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        span.Label = "zone";
        var ctx = new RecordingRenderContext();

        CartesianParts.PartFactory.Spans(axes, Plot, ctx, theme, new DataTransform(0, 10, 0, 10, Plot), new DataRange(0, 10, 0, 10)).Render();

        Assert.All(ctx.Of<DrawLineCall>(), c => Assert.Equal(FirstColourOf(theme), c.Color));
        Assert.Equal(FirstColourOf(theme), (ctx.Of<DrawTextCall>().Single().Font).Color);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void APatchGivenNoFace_IsFilledInTheFirstColourOfTheThemesCycle(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(FirstColourOf(theme), PatchFaceUnder(theme, Rect()));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void APatchGivenAFace_IsFilledInIt_BlueOfTheOldDefaultIncluded(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(Colors.Tab10Blue, PatchFaceUnder(theme, new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Tab10Blue }));
        Assert.Equal(Colors.Red, PatchFaceUnder(theme, new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Red }));
    }

    [Fact]
    public void ThePatchFaceAPatchReadsBack_IsTheFirstColourOfTheStandardCycle_UntilOneIsGiven()
    {
        var patch = Rect();

        Assert.Equal(Colors.Tab10Blue, patch.FaceColor);
        patch.FaceColor = Colors.Red;
        Assert.Equal(Colors.Red, patch.FaceColor);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void APatchDrawsTheSamePictureBeforeAndAfterAJsonRoundTrip(string name)
    {
        var theme = name.AsTheme();
        var ungiven = Rect();
        var given = new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Tab10Blue };

        Assert.Equal(PatchFaceUnder(theme, ungiven), PatchFaceUnder(theme, RoundTrip(ungiven)));
        Assert.Equal(PatchFaceUnder(theme, given), PatchFaceUnder(theme, RoundTrip(given)));
        Assert.Equal(Colors.Tab10Blue, PatchFaceUnder(theme, RoundTrip(given)));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheJsonOfAPatch_CarriesTheFaceItWasGiven_AndNoneWhenItWasGivenNone (withFace)", () => Json(new Rectangle(new(1, 1), 2, 1) { FaceColor = Colors.Tab10Blue }), "faceColor") { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("TheJsonOfAPatch_CarriesTheFaceItWasGiven_AndNoneWhenItWasGivenNone (withoutFace)", () => Json(Rect())) { Absent = ["faceColor"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    private static string Json(Patch patch)
    {
        var figure = new Figure();
        figure.AddSubPlot().AddPatch(patch);
        return new ChartSerializer().ToJson(figure);
    }

    private static Rectangle RoundTrip(Patch patch)
    {
        var figure = new Figure();
        figure.AddSubPlot().AddPatch(patch);
        var back = figure.RoundTripped();
        return (Rectangle)back.SubPlots[0].Patches[0];
    }
}
