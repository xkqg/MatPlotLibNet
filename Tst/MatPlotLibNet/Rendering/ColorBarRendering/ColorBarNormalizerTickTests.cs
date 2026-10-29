// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>A colour bar places its ticks where the chart's normaliser places the colours: the gradient is the colour map laid
/// out evenly along the bar, and a value is drawn in the colour the normaliser sends it to, so the tick of a value stands at
/// the normaliser's fraction of it, not at its share of the range. Measured before the change: a heatmap of 1 to 1000 under a
/// log normaliser had its tick 200 at 0.199 of the bar, where the cells of value 200 are drawn in the colour at 0.767.
/// <para>The bar is read off the calls it makes: its frame (the last rectangle without a fill), its gradient (the filled
/// rectangles) and its labels.</para></summary>
public class ColorBarNormalizerTickTests
{
    private static readonly double[,] Data = { { 1, 250 }, { 500, 1000 } };

    private const double Low = 1, High = 1000;

    private sealed record BarCalls(Rect Frame, List<(string Text, Point At)> Ticks, List<Color> Gradient);

    public static TheoryData<ColorBarOrientation, string> Normalizers
    {
        get
        {
            var rows = new TheoryData<ColorBarOrientation, string>();
            foreach (var orientation in new[] { ColorBarOrientation.Vertical, ColorBarOrientation.Horizontal })
                foreach (var name in new[] { "linear", "log", "power", "symlog", "twoslope", "centered" })
                    rows.Add(orientation, name);
            return rows;
        }
    }

    private static INormalizer? NormalizerNamed(string name) => name switch
    {
        "log" => new LogNormalizer(),
        "power" => new PowerNormNormalizer(0.5),
        "symlog" => new SymLogNormalizer(10),
        "twoslope" => new TwoSlopeNormalizer(100),
        "centered" => new CenteredNormNormalizer(100),
        _ => null,
    };

    private static Figure Chart(INormalizer? normalizer, ColorBarOrientation orientation) =>
        Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Heatmap(Data);
            if (normalizer is not null) ax.WithNormalizer(normalizer);
            ax.WithColorBar(cb => cb with { Orientation = orientation });
        }).Build();

    /// <summary>The calls the axes' colour bar makes: those between its group's start and end.</summary>
    private static BarCalls ReadBar(RecordingRenderContext ctx)
    {
        int start = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && (string)((dynamic)c.Args!).cssClass == "colorbar");
        int end = ctx.Calls.FindIndex(start, c => c.Kind == "EndGroup");
        var bar = ctx.Calls.GetRange(start + 1, end - start - 1);
        var rectangles = bar.Where(c => c.Kind == "DrawRectangle").ToList();
        var frame = (Rect)((dynamic)rectangles.Last(c => ((Color?)((dynamic)c.Args!).fill) is null).Args!).rect;
        var gradient = rectangles.Where(c => ((Color?)((dynamic)c.Args!).fill) is not null)
            .Select(c => (Color)((dynamic)c.Args!).fill).ToList();
        var ticks = bar.Where(c => c.Kind == "DrawText")
            .Select(c => (Text: (string)((dynamic)c.Args!).text, At: (Point)((dynamic)c.Args!).position))
            .Where(t => double.TryParse(t.Text, out _)).ToList();
        return new BarCalls(frame, ticks, gradient);
    }

    private static BarCalls Render(INormalizer? normalizer, ColorBarOrientation orientation)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(Chart(normalizer, orientation), ctx);
        return ReadBar(ctx);
    }

    /// <summary>Where along the bar (0 at its start, 1 at its end) a tick label stands.</summary>
    private static double FractionAlong(BarCalls bar, (string Text, Point At) tick, ColorBarOrientation orientation) =>
        orientation == ColorBarOrientation.Vertical
            ? 1 - (tick.At.Y - 4 - bar.Frame.Y) / bar.Frame.Height
            : (tick.At.X - bar.Frame.X) / bar.Frame.Width;

    // ---- a tick stands at its colour -------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Normalizers))]
    public void ATick_StandsWhereTheNormaliserPutsTheColourOfItsValue(ColorBarOrientation orientation, string name)
    {
        var normalizer = NormalizerNamed(name) ?? LinearNormalizer.Instance;

        var bar = Render(NormalizerNamed(name), orientation);

        Assert.True(bar.Ticks.Count >= 2, $"{name}: {bar.Ticks.Count} ticks");
        foreach (var tick in bar.Ticks)
        {
            double expected = normalizer.Normalize(double.Parse(tick.Text), Low, High);
            Assert.Equal(expected, FractionAlong(bar, tick, orientation), 6);
        }
    }

    /// <summary>The case the row names: log, 1 to 1000, the tick 200.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void ALogBar_PutsTheTick200AtSevenSixtySevenThousandths(ColorBarOrientation orientation)
    {
        var bar = Render(new LogNormalizer(), orientation);

        var tick = bar.Ticks.Single(t => t.Text == "200");
        Assert.Equal(Math.Log(200) / Math.Log(1000), FractionAlong(bar, tick, orientation), 6);
        Assert.Equal(0.767, FractionAlong(bar, tick, orientation), 3);
    }

    // ---- no two labels touch ---------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Normalizers))]
    public void NoTwoLabels_ReadingAnyNormaliser_StandCloserThanALabelTakes(ColorBarOrientation orientation, string name)
    {
        var bar = Render(NormalizerNamed(name), orientation);
        var font = ThemedFontProvider.TickFont(Theme.MatplotlibV2);
        var ctx = new RecordingRenderContext();
        var ordered = bar.Ticks.OrderBy(t => FractionAlong(bar, t, orientation)).ToList();

        for (int i = 1; i < ordered.Count; i++)
        {
            double gap = (FractionAlong(bar, ordered[i], orientation) - FractionAlong(bar, ordered[i - 1], orientation))
                * (orientation == ColorBarOrientation.Vertical ? bar.Frame.Height : bar.Frame.Width);
            double needed = orientation == ColorBarOrientation.Vertical ? font.Size : ctx.MeasureText(ordered[i].Text, font).Width;
            Assert.True(gap >= needed - 1e-9, $"{name}: '{ordered[i - 1].Text}' and '{ordered[i].Text}' stand {gap:F2} px apart, a label takes {needed:F2}");
        }
    }

    /// <summary>The normaliser that clamps its input to [0, 1] sends every value past 1 to the end of the gradient: the ticks
    /// would pile at the end, and the ones that touch are left out.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void ANoNormBar_LeavesOutTheTicksThatPileAtItsEnd(ColorBarOrientation orientation)
    {
        var bar = Render(NoNormNormalizer.Instance, orientation);

        var fractions = bar.Ticks.Select(t => FractionAlong(bar, t, orientation)).ToList();
        Assert.True(bar.Ticks.Count >= 1);
        Assert.Equal(fractions.Count, fractions.Select(f => Math.Round(f, 6)).Distinct().Count());
    }

    // ---- a bar of bands ----------------------------------------------------------------------------------------------------

    /// <summary>A boundary normaliser sends every value to the colour of its bin, and the top boundary itself to the last bin:
    /// the bar is bands, one per bin, each boundary stands where its band begins, and no tick is placed by the range.</summary>
    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void ABoundaryBar_HasABandForEachBin_AndATickOnEachBoundary(ColorBarOrientation orientation)
    {
        double[] boundaries = [1, 10, 100, 500, 1000];

        var bar = Render(new BoundaryNormalizer(boundaries), orientation);

        Assert.Equal(["1", "10", "100", "500", "1000"], bar.Ticks.OrderBy(t => FractionAlong(bar, t, orientation)).Select(t => t.Text).ToArray());
        var fractions = bar.Ticks.OrderBy(t => FractionAlong(bar, t, orientation)).Select(t => FractionAlong(bar, t, orientation)).ToArray();
        for (int k = 0; k < fractions.Length; k++) Assert.Equal(k / 4.0, fractions[k], 6);
        Assert.Equal(4, bar.Gradient.Distinct().Count());
        var map = ColorMaps.Viridis;
        Assert.True(new HashSet<Color> { map.GetColor(0), map.GetColor(0.25), map.GetColor(0.5), map.GetColor(0.75) }.SetEquals(bar.Gradient));
    }

    // ---- the colour bar of a figure ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureColorBar_PutsItsTicksWhereTheNormaliserPutsTheColours(ColorBarOrientation orientation)
    {
        var figure = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => ax.Heatmap(Data).WithNormalizer(new LogNormalizer())).Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().RenderFigureColorBar(figure, [new Rect(80, 40, 600, 400)],
            new ColorBar { Visible = true, Orientation = orientation }, ctx);

        var rectangles = ctx.OfKind("DrawRectangle").ToList();
        var frame = (Rect)((dynamic)rectangles.Last(c => ((Color?)((dynamic)c.Args!).fill) is null).Args!).rect;
        var ticks = ctx.OfKind("DrawText")
            .Select(c => (Text: (string)((dynamic)c.Args!).text, At: (Point)((dynamic)c.Args!).position))
            .Where(t => double.TryParse(t.Text, out _)).ToList();
        var tick = ticks.Single(t => t.Text == "200");
        double fraction = orientation == ColorBarOrientation.Vertical
            ? 1 - (tick.At.Y - 4 - frame.Y) / frame.Height
            : (tick.At.X - frame.X) / frame.Width;
        Assert.Equal(Math.Log(200) / Math.Log(1000), fraction, 6);
    }

    // ---- what the layout reserves ---------------------------------------------------------------------------------------------

    /// <summary>The layout reserves room for the widest label the bar's ticks can have before it knows the bar's length, so no
    /// label the bar draws, under any normaliser (which only ever leaves a tick out), is wider than what is reserved.</summary>
    [Theory]
    [MemberData(nameof(Normalizers))]
    public void TheRoomReserved_IsAtLeastTheWidestLabelDrawn(ColorBarOrientation orientation, string name)
    {
        var ctx = new RecordingRenderContext();
        var font = ThemedFontProvider.TickFont(Theme.MatplotlibV2);
        var bar = Render(NormalizerNamed(name), orientation);

        double reserved = MatPlotLibNet.Rendering.ColorBarRendering.ColorBarScale.WidestLabel(Low, High, ctx, font, NormalizerNamed(name) ?? LinearNormalizer.Instance);

        Assert.All(bar.Ticks, t => Assert.True(ctx.MeasureText(t.Text, font).Width <= reserved + 1e-9));
    }

    // ---- the normaliser of a series ----------------------------------------------------------------------------------------------

    [Fact]
    public void ASeriesWithoutANormaliser_IsReadAsLinear_AndOneWithOne_AsThatNormaliser()
    {
        var series = new HeatmapSeries(Data);
        Assert.Same(LinearNormalizer.Instance, series.NormalizerOrLinear());

        var log = new LogNormalizer();
        series.Normalizer = log;
        Assert.Same(log, series.NormalizerOrLinear());
    }

    // ---- a spectrogram's bar reads the magnitudes its cells are coloured by ----------------------------------------------------

    private static SpectrogramSeries Spectrogram()
    {
        double[] signal = [.. Enumerable.Range(0, 512).Select(i => 3 * Math.Sin(i * 0.4) + 0.5 * Math.Sin(i * 1.7))];
        return new SpectrogramSeries(signal) { SampleRate = 100, WindowSize = 64, Overlap = 32 };
    }

    /// <summary>Measured before the change: the series said its colours ran from 0 to 1 whatever its magnitudes were, so the
    /// bar of a spectrogram named a scale no cell was drawn on.</summary>
    [Fact]
    public void ASpectrogramsColorBarRange_IsTheRangeOfTheMagnitudesItsCellsAreColouredBy()
    {
        var series = Spectrogram();
        var magnitudes = Fft.Stft(series.Signal, series.WindowSize, series.Overlap, series.SampleRate).Magnitudes;

        Assert.Equal(magnitudes.ScanColorBarRange(), series.GetColorBarRange());
        Assert.NotEqual(new MinMaxRange(0, 1), series.GetColorBarRange());
    }

    [Fact]
    public void ASpectrogramsBar_NamesValuesInsideTheMagnitudesItWasDrawnOver()
    {
        var series = Spectrogram();
        var range = series.GetColorBarRange();
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series).WithColorBar()).Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var bar = ReadBar(ctx);
        Assert.True(bar.Ticks.Count >= 2);
        Assert.All(bar.Ticks, t => Assert.InRange(double.Parse(t.Text), range.Min, range.Max));
        Assert.True(range.Max > 2, "the magnitudes of this signal reach past 2, where a bar from 0 to 1 stops");
    }
}
