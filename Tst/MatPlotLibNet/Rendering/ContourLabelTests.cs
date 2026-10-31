// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A contour level's label, as matplotlib's <c>clabel</c> places it: at the middle of its line by arc length, on a
/// line at least 1.2 times as long as the label is wide (a shorter line gets none), turned to the chord across the label's
/// width and folded into [-90, 90) so it never reads upside down, in the line's colour, on a turned box in the theme's
/// plot background. Every line is drawn before any label, so no line runs over a label. The level is written the same on
/// every machine. Before, the label stood at the middle vertex, upright on a white rectangle, and a German machine wrote
/// <c>0,5</c> where the runner wrote <c>0.5</c>.</summary>
public class ContourLabelTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>Data 0..10 on both axes: 40 pixels per unit across, 30 down.</summary>
    private static DataTransform Linear() => new(0, 10, 0, 10, Bounds);

    /// <summary>The same, with the Y axis drawn upside down (larger values lower).</summary>
    private static DataTransform InvertedY() => new(0, 10, 10, 0, Bounds);

    private static T Arg<T>(CallRecord call, string name) =>
        (T)call.Args!.GetType().GetProperty(name)!.GetValue(call.Args)!;

    private static RecordingRenderContext Render(ContourSeries series, DataTransform? transform = null, Theme? theme = null)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(transform ?? Linear(), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))
        {
            Theme = theme ?? Theme.Default,
        };
        new ContourSeriesRenderer(context).Render(series);
        return ctx;
    }

    /// <summary>z = 2x over x 0..5, four rows: the iso-line of level L is the vertical line x = L / 2, 90 pixels long.</summary>
    private static ContourSeries Ramp(params double[] levels)
    {
        double[] x = [0, 1, 2, 3, 4, 5], y = [0, 1, 2, 3];
        var z = new double[y.Length, x.Length];
        for (int r = 0; r < y.Length; r++)
            for (int c = 0; c < x.Length; c++)
                z[r, c] = 2.0 * c;
        return new ContourSeries(x, y, z) { LevelValues = levels, ShowLabels = true };
    }

    /// <summary>z = x - y over 0..6: the iso-line of level 0.5 is the diagonal x - y = 0.5, one polyline from (0.5, 0) to
    /// (6, 5.5), running (1, 1) in data and (40, -30) in pixels.</summary>
    private static ContourSeries Diagonal()
    {
        double[] axis = [0, 1, 2, 3, 4, 5, 6];
        var z = new double[axis.Length, axis.Length];
        for (int r = 0; r < axis.Length; r++)
            for (int c = 0; c < axis.Length; c++)
                z[r, c] = c - r;
        return new ContourSeries(axis, axis, z) { LevelValues = [0.5], ShowLabels = true };
    }

    /// <summary>z = 12 - 2(|x - 3| + |y - 3|) over 0..6: the iso-line of level L is the closed diamond
    /// |x - 3| + |y - 3| = (12 - L) / 2.</summary>
    private static ContourSeries Cone(params double[] levels)
    {
        double[] axis = [0, 1, 2, 3, 4, 5, 6];
        var z = new double[axis.Length, axis.Length];
        for (int r = 0; r < axis.Length; r++)
            for (int c = 0; c < axis.Length; c++)
                z[r, c] = 12.0 - 2.0 * (Math.Abs(c - 3.0) + Math.Abs(r - 3.0));
        return new ContourSeries(axis, axis, z) { LevelValues = levels, ShowLabels = true };
    }

    /// <summary>The angle of the pixel vector (dx, dy), counter-clockwise from the x axis with y up, in degrees.</summary>
    private static double ScreenAngle(double dx, double dy) => Math.Atan2(-dy, dx) * 180 / Math.PI;

    // ---- the text ---------------------------------------------------------------------------------------------------

    [Fact]
    public void ALevel_IsWrittenTheSameUnderAGermanCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var text = Arg<string>(Assert.Single(Render(Ramp(0.5)).OfKind("DrawText")), "text");

            Assert.Equal("0.5", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ---- where it stands and how it is turned -------------------------------------------------------------------------

    /// <summary>The ramp's lines are vertical, 90 pixels from y = 300 up to y = 210: the label stands at the middle, x = L/2
    /// at 40 pixels a unit and y = 255, and a vertical chord folds to -90.</summary>
    [Fact]
    public void AVerticalLine_IsLabelledAtItsMiddle_TurnedToMinus90()
    {
        var label = Assert.Single(Render(Ramp(5)).OfKind("DrawText"));

        Assert.Equal(-90, Arg<double>(label, "rotation"), 1e-6);
        var at = Arg<Point>(label, "position");
        // the baseline stands 0.35 of the font size below the middle, in the text's own frame: to the left when turned -90
        Assert.Equal(100 - 0.35 * 10, at.X, 1e-6);
        Assert.Equal(255, at.Y, 1e-6);
    }

    /// <summary>A diagonal running (1, 1) in data runs (40, -30) in pixels: its label is turned to that chord, 36.87
    /// degrees, not to the data's 45.</summary>
    [Fact]
    public void ADiagonal_IsTurnedToItsPixelChord()
    {
        var label = Assert.Single(Render(Diagonal()).OfKind("DrawText"));

        Assert.Equal(ScreenAngle(40, -30), Arg<double>(label, "rotation"), 1e-6);
    }

    /// <summary>With the Y axis upside down the same diagonal runs (40, 30) in pixels, so the label turns the other way:
    /// the angle is the one the line has on the screen.</summary>
    [Fact]
    public void AnInvertedYAxis_TurnsTheLabelAsTheLineStandsOnTheScreen()
    {
        var label = Assert.Single(Render(Diagonal(), InvertedY()).OfKind("DrawText"));

        Assert.Equal(ScreenAngle(40, 30), Arg<double>(label, "rotation"), 1e-6);
    }

    /// <summary>A bend at the middle: the turn is the chord from 10 pixels before the middle to 10 after (label width 20),
    /// not the direction of either leg.</summary>
    [Fact]
    public void TheTurn_IsTheChordAcrossTheLabelWidth_FoldedUpright()
    {
        Point[] bend = [new(0, 0), new(100, 0), new(100, 100)];

        var placement = ContourLabelPlacement.Along(bend, 20)!.Value;

        Assert.Equal(new Point(100, 0), placement.Centre);
        // chord (90, 0) → (100, 10): (10, 10) on the screen, -45 degrees
        Assert.Equal(-45, placement.Rotation, 1e-6);
    }

    public static TheoryData<Point[]> Polylines { get; } = new(
        [new(0, 0), new(100, 0), new(100, 100)],
        [new(10, 10), new(60, 80), new(90, 20), new(200, 50)],
        [new(5, 300), new(5, 200)],
        [new(0, 0), new(100, 0), new(100, 100), new(0, 100), new(0, 0)]);

    /// <summary>The label belongs to the line, not to the way it was traced: the same line run backwards puts it at the
    /// same place, turned the same way.</summary>
    [Theory]
    [MemberData(nameof(Polylines))]
    public void TheSameLineRunBackwards_GetsTheSameLabel(Point[] line)
    {
        var forward = ContourLabelPlacement.Along(line, 20)!.Value;
        var backward = ContourLabelPlacement.Along([.. line.Reverse()], 20)!.Value;

        Assert.Equal(forward.Centre.X, backward.Centre.X, 1e-9);
        Assert.Equal(forward.Centre.Y, backward.Centre.Y, 1e-9);
        Assert.Equal(forward.Rotation, backward.Rotation, 1e-6);
        Assert.InRange(forward.Rotation, -90, 90 - 1e-12);
    }

    /// <summary>A closed loop has its label half way round from where it was traced: a square of side 100 traced from a
    /// corner has it at the opposite corner, on the chord across it.</summary>
    [Fact]
    public void AClosedLoop_IsLabelledHalfWayRound()
    {
        Point[] square = [new(0, 0), new(100, 0), new(100, 100), new(0, 100), new(0, 0)];

        var placement = ContourLabelPlacement.Along(square, 20)!.Value;

        Assert.Equal(new Point(100, 100), placement.Centre);
        // chord (100, 90) → (90, 100) points down-left; folded upright it reads up-right, 45 degrees
        Assert.Equal(45, placement.Rotation, 1e-6);
    }

    /// <summary>The cone's level 11 is a closed diamond of data radius 0.5 around (3, 3): it gets one label, standing on
    /// the ring.</summary>
    [Fact]
    public void AClosedContour_GetsOneLabelOnItsRing()
    {
        var ctx = Render(Cone(11));

        var box = Arg<Point[]>(Assert.Single(ctx.OfKind("DrawPolygon")), "points");
        var centre = new Point(box.Average(p => p.X), box.Average(p => p.Y));
        var data = Linear().PixelToData(centre);
        Assert.Single(ctx.OfKind("DrawText"));
        // the box is centred near the line: within a third of the font size of the ring
        Assert.InRange(Math.Abs(Math.Abs(data.X - 3) + Math.Abs(data.Y - 3) - 0.5), 0, 0.15);
    }

    /// <summary>A line shorter than 1.2 times the label's measured width gets no label; one exactly that long gets one.</summary>
    [Theory]
    [InlineData(0.99, false)]
    [InlineData(1.0, true)]
    [InlineData(5.0, true)]
    public void ALine_IsLabelledOnlyWhenItIsAtLeast1point2TimesTheLabelWidth(double timesTheMinimum, bool labelled)
    {
        Assert.Equal(1.2, ContourLabelPlacement.MinimumLengthRatio);
        Point[] line = [new(0, 0), new(timesTheMinimum * ContourLabelPlacement.MinimumLengthRatio * 20, 0)];

        Assert.Equal(labelled, ContourLabelPlacement.Along(line, 20).HasValue);
    }

    /// <summary>A line of no length carries no label, even an empty one.</summary>
    [Fact]
    public void ALineOfNoLength_GetsNoLabel() =>
        Assert.Null(ContourLabelPlacement.Along([new(5, 5), new(5, 5)], 0));

    /// <summary>The cone's level 11.9 is a ring of 10 pixels round; its label "11.9" is 24 wide, so the ring is drawn and
    /// carries none.</summary>
    [Fact]
    public void AContourTooShortForItsLabel_IsDrawnWithoutOne()
    {
        var ctx = Render(Cone(11.9, 7));

        Assert.Equal(2, ctx.CountOf("DrawLines"));
        Assert.Equal(["7"], ctx.OfKind("DrawText").Select(c => Arg<string>(c, "text")));
    }

    /// <summary>matplotlib's fold, <c>(angle + 90) % 180 - 90</c>: every direction reads left to right.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(45, 45)]
    [InlineData(89.5, 89.5)]
    [InlineData(90, -90)]
    [InlineData(-90, -90)]
    [InlineData(135, -45)]
    [InlineData(-135, 45)]
    [InlineData(180, 0)]
    [InlineData(-180, 0)]
    public void TheTurn_IsFoldedIntoMinus90To90(double angle, double folded) =>
        Assert.Equal(folded, ContourLabelPlacement.Upright(angle), 1e-9);

    // ---- colour and background ----------------------------------------------------------------------------------------

    /// <summary>The label wears its line's colour: the colour map's colour of the level when there is a map, the theme's
    /// neutral grey when there is none.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALabel_WearsTheColourOfItsLine(bool colourMap)
    {
        var series = Ramp(1, 5, 9);
        if (colourMap) series.ColorMap = ColorMaps.Plasma;
        var ctx = Render(series);

        var lines = ctx.OfKind("DrawLines").Select(c => Arg<Color>(c, "color")).ToList();
        var texts = ctx.OfKind("DrawText").Select(c => Arg<Font>(c, "font").Color).ToList();
        Assert.Equal(3, texts.Count);
        Assert.Equal(lines.Cast<Color?>(), texts);
        if (colourMap) Assert.Equal(3, lines.Distinct().Count());
    }

    public static TheoryData<string> Themes { get; } = new("default", "dark", "seaborn");

    private static Theme ThemeNamed(string name) => name switch
    {
        "dark" => Theme.Dark,
        "seaborn" => Theme.Seaborn,
        _ => Theme.Default,
    };

    /// <summary>The box behind a label is the theme's plot background, not white: on the dark theme a white box was a
    /// patch of light under every label.</summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void TheBoxBehindALabel_IsTheThemesPlotBackground(string themeName)
    {
        var theme = ThemeNamed(themeName);

        var boxes = Render(Ramp(1, 5, 9), theme: theme).OfKind("DrawPolygon");

        Assert.Equal(3, boxes.Count);
        Assert.All(boxes, b => Assert.Equal(theme.AxesBackground, Arg<Color?>(b, "fill")));
        Assert.All(boxes, b => Assert.Null(Arg<Color?>(b, "stroke")));
    }

    /// <summary>The box is turned with its text: four corners, its long side along the label's turn, and the label's
    /// point inside it.</summary>
    [Fact]
    public void TheBoxBehindALabel_IsTurnedWithIt()
    {
        var ctx = Render(Diagonal());
        var label = Assert.Single(ctx.OfKind("DrawText"));
        var box = Arg<Point[]>(Assert.Single(ctx.OfKind("DrawPolygon")), "points");

        Assert.Equal(4, box.Length);
        double rotation = Arg<double>(label, "rotation");
        Assert.Equal(rotation, ScreenAngle(box[1].X - box[0].X, box[1].Y - box[0].Y), 1e-6);
        Assert.Equal(rotation, ScreenAngle(box[2].X - box[3].X, box[2].Y - box[3].Y), 1e-6);
        // the box is wider than the text: the measured width ("0.5" = 18 pixels) plus 5 pixels each side
        Assert.Equal(18 + 2 * ContourLabelPlacement.InlineSpacing,
            Math.Sqrt(Math.Pow(box[1].X - box[0].X, 2) + Math.Pow(box[1].Y - box[0].Y, 2)), 1e-6);
    }

    // ---- order --------------------------------------------------------------------------------------------------------

    /// <summary>Every line is drawn first, then every label: a line traced after a label would run across its box.</summary>
    [Fact]
    public void EveryLine_IsDrawnBeforeAnyLabel()
    {
        var calls = Render(Ramp(1, 3, 5, 7, 9)).Calls;

        int lastLine = calls.FindLastIndex(c => c.Kind == "DrawLines");
        int firstLabel = calls.FindIndex(c => c.Kind is "DrawPolygon" or "DrawText");
        Assert.Equal(5, calls.Count(c => c.Kind == "DrawLines"));
        Assert.Equal(5, calls.Count(c => c.Kind == "DrawText"));
        Assert.True(lastLine < firstLabel, $"a line at call {lastLine} was drawn after a label at call {firstLabel}");
    }
}
