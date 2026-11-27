// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What an axis that scales itself draws when its data hold no width: no data at all, one value, or a limit set at one value.
/// Under the matplotlib themes the numbers are matplotlib 3.11.2's, printed with python. An axis that something asks to be scaled
/// (a series asks both of its axes: <c>_request_autoscale_view()</c>; <c>axhline</c> asks Y alone, <c>axvline</c> X alone) and that
/// holds no data starts from its locator's <c>nonsingular(-inf, inf)</c> (<c>axes/_base.py</c> 3170-3182): (-0.05, 0.05) on a linear
/// and a symmetric-log axis (<c>ticker.py</c> 1796), (1, 10) on a log axis (2617), (1e-7, 1 - 1e-7) on a logit axis (2997), 1970-01-01
/// to 1970-01-02 on a date axis (<c>dates.py</c> 1331); one value is widened by the same <c>nonsingular</c>; then the margin and the
/// autolimit mode are applied as to any range. An axis nothing asks for keeps its first view, (0, 1), unless its scale's default
/// limits differ from the linear one's (log, logit), which makes <c>_set_axes_scale</c> scale it at once (<c>axis.py</c> 818-831).
/// A 3-D axis holds the data limits <c>Axes3D.clear</c> seeds (<c>mplot3d/axes3d.py</c> 1588-1592). The library's own themes keep the
/// library's rule: an empty range is 0 to 1, a flat one is opened by half a unit (<see cref="Range1D.Normalized"/>).</summary>
public class EmptyAxisRangeTests
{
    private static void Range(double lo, double hi, double drawnLo, double drawnHi)
    {
        lo.AssertCloseTo(drawnLo, relative: 1e-12, floor: 1);
        hi.AssertCloseTo(drawnHi, relative: 1e-12, floor: 1);
    }

    /// <summary>The row's repro: <c>sns.rugplot(np.array([1., 2, 2, 3, 5]), height=0.3)</c> adds nothing to Y but asks for it to be
    /// scaled (<c>add_collection</c>, autolim): ylim (-0.115, 0.115) default ((-0.05, 0.05) padded by 0.05 + 0.6), (-0.11, 0.11) classic
    /// (tight: seaborn's <c>ax.margins</c>). Before the fix the library drew 0 to 1 padded: (-0.65, 1.65).</summary>
    [Theory]
    [InlineData("v2", -0.115, 0.115)]
    [InlineData("classic", -0.11, 0.11)]
    public void ALoneRug_IsRangedOnYAsAnEmptyAxisThatIsAsked(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.Rugplot(new double[] { 1.0, 2, 2, 3, 5 }).Height = 0.3;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary><c>ax.plot([], [])</c>: both axes asked, no data: (-0.055, 0.055) default, (-0.06, 0.06) classic.</summary>
    [Theory]
    [InlineData("v2", -0.055, 0.055)]
    [InlineData("classic", -0.06, 0.06)]
    public void ASeriesWithNoData_AsksForBothAxes(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.Plot(Array.Empty<double>(), Array.Empty<double>());

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A fresh axes and the axis a reference line does not ask for keep the first view: <c>axhline(3)</c> gives xlim (0, 1) and
    /// ylim (2.835, 3.165) default, (2.85, 3.15) classic; <c>axvline(3)</c> the same the other way round.</summary>
    [Theory]
    [InlineData("v2", 2.835, 3.165)]
    [InlineData("classic", 2.85, 3.15)]
    public void AnAxisNothingAsksFor_KeepsTheFirstView(string theme, double lo, double hi)
    {
        var across = new Axes();
        across.AxHLine(3);
        var along = new Axes();
        along.AxVLine(3);

        var empty = new Axes().ComputeDataRanges(theme.AsTheme());
        var h = across.ComputeDataRanges(theme.AsTheme());
        var v = along.ComputeDataRanges(theme.AsTheme());

        Assert.Equal(new DataRange(0, 1, 0, 1), empty);
        Range(0, 1, h.XMin, h.XMax);
        Range(lo, hi, h.YMin, h.YMax);
        Range(lo, hi, v.XMin, v.XMax);
        Range(0, 1, v.YMin, v.YMax);
    }

    /// <summary>The first view of an inverted axis is drawn high to low: <c>axvline(3); invert_yaxis()</c> gives ylim (1, 0).</summary>
    [Fact]
    public void TheFirstViewOfAnInvertedAxis_RunsHighToLow()
    {
        var axes = new Axes();
        axes.AxVLine(3);
        axes.YAxis.Inverted = true;

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(1, range.YMin);
        Assert.Equal(0, range.YMax);
    }

    /// <summary>One value is widened by 5 % of itself each way (<c>nonsingular</c>, expander 0.05), at 0 to (-0.05, 0.05):
    /// <c>plot([3], [3])</c> (2.835, 3.165) / (2.85, 3.15); <c>plot([0], [0])</c> (-0.055, 0.055) / (-0.06, 0.06);
    /// <c>plot([1, 2], [-4, -4])</c> ylim (-4.22, -3.78) / (-4.2, -3.8).</summary>
    [Theory]
    [InlineData("v2", 3.0, 2.835, 3.165)]
    [InlineData("classic", 3.0, 2.85, 3.15)]
    [InlineData("v2", 0.0, -0.055, 0.055)]
    [InlineData("classic", 0.0, -0.06, 0.06)]
    [InlineData("v2", -4.0, -4.22, -3.78)]
    [InlineData("classic", -4.0, -4.2, -3.8)]
    public void OneValue_IsWidenedByFivePerCentOfItself(string theme, double value, double lo, double hi)
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0], [value, value]);

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A limit set at one value is widened as <c>set_ylim</c> widens it (<c>nonsingular</c>, no margin): <c>set_ylim(3, 3)</c>
    /// gives (2.85, 3.15), <c>set_ylim(0, 0)</c> (-0.05, 0.05), under both styles; a flat range on an inverted axis keeps its turn:
    /// <c>plot([1, 2], [3, 3]); invert_yaxis()</c> (3.165, 2.835).</summary>
    [Theory]
    [InlineData("v2", 3.0, 2.85, 3.15)]
    [InlineData("classic", 3.0, 2.85, 3.15)]
    [InlineData("v2", 0.0, -0.05, 0.05)]
    public void ALimitSetAtOneValue_IsWidenedWithoutAMargin(string theme, double value, double lo, double hi)
    {
        var axes = new Axes();
        axes.Plot([0.0, 1.0], [0.0, 1.0]);
        axes.YAxis.Min = value;
        axes.YAxis.Max = value;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.YMin, range.YMax);
    }

    [Fact]
    public void AFlatRangeOnAnInvertedAxis_IsWidenedAndKeepsItsTurn()
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0], [3.0, 3.0]);
        axes.YAxis.Inverted = true;

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Range(3.165, 2.835, range.YMin, range.YMax);
    }

    /// <summary>Each scale widens by its own locator: an axis put on log or logit is scaled at once from its default limits, even when
    /// nothing asks for it (<c>set_yscale('log'); axvline(3)</c>: ylim (0.8912509381337456, 11.220184543019636) default, (1, 10)
    /// classic; logit: (1.9952624945845213e-08, 0.9999999800473751), (1e-07, 0.9999999)); a symmetric-log axis keeps (0, 1).</summary>
    [Theory]
    [InlineData("v2", AxisScale.Log, 0.8912509381337456, 11.220184543019636)]
    [InlineData("classic", AxisScale.Log, 1.0, 10.0)]
    [InlineData("v2", AxisScale.Logit, 1.9952624945845213e-08, 0.9999999800473751)]
    [InlineData("classic", AxisScale.Logit, 1e-07, 0.9999999)]
    [InlineData("v2", AxisScale.SymLog, 0.0, 1.0)]
    [InlineData("classic", AxisScale.SymLog, 0.0, 1.0)]
    public void AnAxisNothingAsksFor_OnAScaleWithDefaultLimitsOfItsOwn_IsScaledFromThem(string theme, AxisScale scale, double lo, double hi)
    {
        var axes = new Axes();
        axes.AxVLine(0.3);
        axes.YAxis.Scale = scale;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>An empty axis that is asked, per scale: <c>set_yscale(s); plot([], [])</c>: log (0.891.., 11.22..) / (1, 10); symlog
    /// (-0.055, 0.055) / (-0.1, 0.1); logit as above.</summary>
    [Theory]
    [InlineData("v2", AxisScale.Log, 0.8912509381337456, 11.220184543019636)]
    [InlineData("classic", AxisScale.Log, 1.0, 10.0)]
    [InlineData("v2", AxisScale.SymLog, -0.055, 0.055)]
    [InlineData("classic", AxisScale.SymLog, -0.1, 0.1)]
    [InlineData("v2", AxisScale.Logit, 1.9952624945845213e-08, 0.9999999800473751)]
    public void AnEmptyAxisThatIsAsked_StartsFromItsScalesDefaultLimits(string theme, AxisScale scale, double lo, double hi)
    {
        var axes = new Axes();
        axes.Plot(Array.Empty<double>(), Array.Empty<double>());
        axes.YAxis.Scale = scale;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>One value per scale: <c>plot([1, 2], [3, 3])</c> on log (0.891250938134, 11.22018454302) / (1, 10) (a decade each way),
    /// on symlog (2.835773732195, 3.165802651346) / (1, 10); <c>plot([1, 2], [0.3, 0.3])</c> on logit (0.021379559611, 0.978620440389)
    /// default (0.1 of it each side).</summary>
    [Theory]
    [InlineData("v2", AxisScale.Log, 3.0, 0.8912509381337456, 11.220184543019636)]
    [InlineData("classic", AxisScale.Log, 3.0, 1.0, 10.0)]
    [InlineData("v2", AxisScale.SymLog, 3.0, 2.835773732195, 3.165802651346)]
    [InlineData("classic", AxisScale.SymLog, 3.0, 1.0, 10.0)]
    [InlineData("v2", AxisScale.Logit, 0.3, 0.021379559611, 0.978620440389)]
    public void OneValue_IsWidenedByItsScale(string theme, AxisScale scale, double value, double lo, double hi)
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0], [value, value]);
        axes.YAxis.Scale = scale;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Assert.True(Math.Abs(lo - range.YMin) <= 1e-11 && Math.Abs(hi - range.YMax) <= 1e-11, $"expected ({lo}, {hi}), drawn ({range.YMin:R}, {range.YMax:R})");
    }

    /// <summary>A date axis starts at matplotlib's first day, 1970-01-01 (OLE Automation date 25569), and widens one date by two years of
    /// 365 days each way (<c>AutoDateLocator.nonsingular</c>): <c>xaxis_date(); axhline(3)</c> xlim (0, 1) days from 1970;
    /// <c>xaxis_date(); plot([], [])</c> (-0.05, 1.05) default, (0, 1) classic; <c>plot([25, 25], ...)</c> (-778, 828) / (-705, 755).</summary>
    [Theory]
    [InlineData("v2", "line", 0.0, 1.0)]
    [InlineData("classic", "line", 0.0, 1.0)]
    [InlineData("v2", "empty", -0.05, 1.05)]
    [InlineData("classic", "empty", 0.0, 1.0)]
    [InlineData("v2", "one", -778.0, 828.0)]
    [InlineData("classic", "one", -705.0, 755.0)]
    public void ADateAxis_StartsAtMatplotlibsFirstDay(string theme, string kind, double lo, double hi)
    {
        const double Epoch = 25569;
        var axes = new Axes();
        if (kind == "line") axes.AxHLine(3);
        else if (kind == "empty") axes.Plot(Array.Empty<double>(), Array.Empty<double>());
        else axes.Plot([Epoch + 25, Epoch + 25], [1.0, 2.0]);
        axes.XAxis.Scale = AxisScale.Date;

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Assert.True(Math.Abs(Epoch + lo - range.XMin) <= 1e-9 && Math.Abs(Epoch + hi - range.XMax) <= 1e-9,
            $"expected ({Epoch + lo}, {Epoch + hi}), drawn ({range.XMin:R}, {range.XMax:R})");
    }

    /// <summary>A date Y axis brings no date ticks in the library (only an X axis is put on dates), so it opens as a linear axis does: an
    /// empty one nothing asks for keeps (0, 1), one a series asks for is (-0.055, 0.055) under the default style.</summary>
    [Theory]
    [InlineData("line", 0.0, 1.0)]
    [InlineData("empty", -0.055, 0.055)]
    public void ADateYAxis_OpensAsALinearOne(string kind, double lo, double hi)
    {
        var axes = new Axes();
        if (kind == "line") axes.AxVLine(3);
        else axes.Plot(Array.Empty<double>(), Array.Empty<double>());
        axes.YAxis.Scale = AxisScale.Date;

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A twin axis is an axes of its own: <c>ax.twinx()</c> with nothing on it keeps (0, 1); <c>twinx().plot([], [])</c>
    /// (-0.055, 0.055); <c>twinx().plot([0.5], [3])</c> (2.835, 3.165) (default style).</summary>
    [Theory]
    [InlineData("none", 0.0, 1.0)]
    [InlineData("empty", -0.055, 0.055)]
    [InlineData("one", 2.835, 3.165)]
    public void AYSlotPastThePrimary_IsRangedAsATwinAxes(string kind, double lo, double hi)
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1]).WithAddRightYAxis(s =>
        {
            if (kind == "empty") s.Plot(Array.Empty<double>(), Array.Empty<double>());
            else if (kind == "one") s.Plot([0.5], [3.0]);
        })).Build().SubPlots[0];
        var slot = axes.YAxes.Right[0];
        var placement = new CartesianAxisPlacement(axes, new Rect(80, 60, 700, 400), new SvgRenderContext(), Theme.MatplotlibV2);

        var range = placement.ComputeSecondaryDataRanges(0, 1, slot.Axis, slot.Series);

        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A 3-D axes starts from the data limits <c>Axes3D.clear</c> seeds (X and Y 0.05 * 10/11 in from 0 and 1, Z 0 to 1), padded
    /// and rounded as data, then by the view margin of a forty-eighth: empty xlim and zlim (-0.020833333333, 1.020833333333) under both
    /// styles. One point <c>plot([3], [3], [3])</c>: xlim (2.828125, 3.171875) default, (2.84375, 3.15625) classic, zlim (2.84375,
    /// 3.15625).</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void AnEmpty3DAxes_StartsFromItsSeededDataLimits(string theme)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.ThreeD };

        var box = axes.ComputeCubeRange(theme.AsTheme());

        Range(-1.0 / 48, 1 + 1.0 / 48, box.X.Lo, box.X.Hi);
        Range(-1.0 / 48, 1 + 1.0 / 48, box.Y.Lo, box.Y.Hi);
        Range(-1.0 / 48, 1 + 1.0 / 48, box.Z.Lo, box.Z.Hi);
    }

    [Theory]
    [InlineData("v2", 2.828125, 3.171875)]
    [InlineData("classic", 2.84375, 3.15625)]
    public void OnePointIn3D_IsWidenedByFivePerCentOfItself(string theme, double xLo, double xHi)
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot3D([3.0], [3.0], [3.0])).Build().SubPlots[0];

        var box = axes.ComputeCubeRange(theme.AsTheme());

        Range(xLo, xHi, box.X.Lo, box.X.Hi);
        Range(2.84375, 3.15625, box.Z.Lo, box.Z.Hi);
    }

    /// <summary>An axes holding one series of the kind matplotlib's or seaborn's function of that name draws, given no data.</summary>
    private static Axes WithNoData(string kind)
    {
        var axes = new Axes();
        double[] none = [];
        double[][] noGroup = [[]];
        switch (kind)
        {
            case "barbs": axes.AddSeries(new MatPlotLibNet.Models.Series.BarbsSeries(none, none, none, none)); break;
            case "pcolormesh": axes.AddSeries(new MatPlotLibNet.Models.Series.PcolormeshSeries(none, none, new double[0, 0])); break;
            case "stackplot": axes.AddSeries(new MatPlotLibNet.Models.Series.StackedAreaSeries(none, [])); break;
            case "broken_barh": axes.AddSeries(new MatPlotLibNet.Models.Series.BrokenBarSeries([[]])); break;
            case "regplot": axes.AddSeries(new MatPlotLibNet.Models.Series.RegressionSeries(none, none)); break;
            case "residplot": axes.AddSeries(new MatPlotLibNet.Models.Series.ResidualSeries(none, none)); break;
            case "signal": axes.AddSeries(new MatPlotLibNet.Models.Series.SignalSeries(none)); break;
            case "tricontour": axes.AddSeries(new MatPlotLibNet.Models.Series.TricontourSeries(none, none, none)); break;
            case "tripcolor": axes.AddSeries(new MatPlotLibNet.Models.Series.TripcolorSeries(none, none, none)); break;
            case "ecdf": axes.AddSeries(new MatPlotLibNet.Models.Series.EcdfSeries(none)); break;
            case "countplot": axes.AddSeries(new MatPlotLibNet.Models.Series.CountSeries([])); break;
            case "kdeplot": axes.Kde(none); break;
            case "rugplot": axes.Rugplot(none); break;
            case "stripplot": axes.AddSeries(new MatPlotLibNet.Models.Series.StripplotSeries(noGroup)); break;
            case "swarmplot": axes.AddSeries(new MatPlotLibNet.Models.Series.SwarmplotSeries(noGroup)); break;
            case "pointplot": axes.AddSeries(new MatPlotLibNet.Models.Series.PointplotSeries(noGroup)); break;
            case "stripplot-of-no-group": axes.AddSeries(new MatPlotLibNet.Models.Series.StripplotSeries([])); break;
            case "eventplot-of-no-row": axes.AddSeries(new MatPlotLibNet.Models.Series.EventplotSeries([])); break;
            default: axes.AddSeries(new MatPlotLibNet.Models.Series.EventplotSeries(noGroup)); break;
        }

        return axes;
    }

    /// <summary>What a series that holds no data asks of its axes under the matplotlib themes, printed for its matplotlib or seaborn
    /// function: a matplotlib function asks for both axes and adds nothing (<c>barbs</c>, <c>pcolormesh</c>, <c>stackplot</c>,
    /// <c>broken_barh([], (0, 1))</c>, <c>regplot</c>, <c>residplot</c>: (-0.055, 0.055) on both axes); a seaborn function and
    /// <c>eventplot</c> return before they touch the axes (<c>countplot</c>, <c>kdeplot</c>, <c>rugplot</c>, <c>stripplot</c>,
    /// <c>swarmplot</c>, <c>pointplot</c>, <c>eventplot([[]])</c>: (0, 1) on both). Before the fix each was ranged from the library's
    /// default 0 to 1, padded: (-0.05, 1.05).</summary>
    [Theory]
    [InlineData("barbs", true)]
    [InlineData("pcolormesh", true)]
    [InlineData("stackplot", true)]
    [InlineData("broken_barh", true)]
    [InlineData("regplot", true)]
    [InlineData("residplot", true)]
    [InlineData("signal", true)]
    [InlineData("tricontour", true)]
    [InlineData("tripcolor", true)]
    [InlineData("ecdf", true)]
    [InlineData("countplot", false)]
    [InlineData("kdeplot", false)]
    [InlineData("rugplot", false)]
    [InlineData("stripplot", false)]
    [InlineData("swarmplot", false)]
    [InlineData("pointplot", false)]
    [InlineData("eventplot", false)]
    public void ASeriesWithNoData_AsksAsItsFunctionDoes(string kind, bool asks)
    {
        var axes = WithNoData(kind);

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        var (lo, hi) = asks ? (-0.055, 0.055) : (0.0, 1.0);
        Range(lo, hi, range.XMin, range.XMax);
        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A box or a violin of no observation asks for both axes and ranges its position: <c>boxplot([[]])</c> gives xlim (0.5, 1.5),
    /// ylim (-0.055, 0.055) default and (-0.06, 0.06) classic (the classic X axis is held by the box's own rounding, outside this rule).</summary>
    [Theory]
    [InlineData("v2", -0.055, 0.055)]
    [InlineData("classic", -0.06, 0.06)]
    public void ABoxOfNoObservation_RangesItsPositionAndAsksForItsValues(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AddSeries(new MatPlotLibNet.Models.Series.BoxSeries([[]]));

        var range = axes.ComputeDataRanges(theme.AsTheme());

        if (theme == "v2") Range(0.5, 1.5, range.XMin, range.XMax);
        Range(lo, hi, range.YMin, range.YMax);
    }

    /// <summary>A spectrogram of no sample is one window of nothing under the matplotlib themes, as <c>specgram</c> pads a signal shorter
    /// than its window: <c>specgram([])</c> (Fs 2, NFFT 256, noverlap 128) gives xlim (32, 96) default, (30, 100) classic, ylim (0, 1).</summary>
    [Theory]
    [InlineData("v2", 32.0, 96.0)]
    [InlineData("classic", 30.0, 100.0)]
    public void ASpectrogramOfNoSample_IsOneWindowOfNothing(string theme, double lo, double hi)
    {
        var axes = new Axes();
        axes.AddSeries(new MatPlotLibNet.Models.Series.SpectrogramSeries(new double[0]) { SampleRate = 2, WindowSize = 256, Overlap = 128 });

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Range(lo, hi, range.XMin, range.XMax);
        Range(0, 1, range.YMin, range.YMax);
    }

    /// <summary>A series that bins or grids its sample ranges the bins it draws, not the sample, so a sample of one value is no flat range:
    /// numpy's edges span the value and half a unit either side (<c>numpy/lib/_histograms_impl.py</c> 321-323), a bar of a histogram stands
    /// in its bin narrowed by <c>rwidth</c> (<c>axes/_axes.py</c> 7540-7552), and a hexbin's grid spans the sample widened by a tenth
    /// (<c>_nonsingular(expander=0.1)</c>, 5675-5676, its corners the data limits, 5811-5812). matplotlib 3.11.2, python -I:
    /// <c>hist([3, 3, 3])</c> xlim (2.45, 3.55) default, (2.4, 3.6) classic; <c>rwidth=0.5</c> (2.4775, 3.5225) default; <c>hist([0, 0])</c>
    /// (-0.55, 0.55), (-0.6, 0.6); <c>density=True</c> ylim (0, 10.5), (0, 10); <c>hist([1, 2, 2, 3, 5], rwidth=0.5)</c> (0.91, 5.09)
    /// default, (1, 5) classic; <c>hist2d([3, 3, 3], [1, 2, 3])</c> xlim (2.5, 3.5), (2.4, 3.6); <c>hexbin([3, 3, 3], [1, 2, 3],
    /// gridsize=5)</c> (2.67, 3.33), (2.7, 3.3) (matplotlib pads the grid by 1e-9 of its width). Before the fix a histogram of one value drew
    /// (2.835, 3.165) and (2.85, 3.15), the sample opened as a flat range.</summary>
    [Theory]
    [InlineData("v2", "hist", 2.45, 3.55, 0, 3.15)]
    [InlineData("classic", "hist", 2.4, 3.6, 0, 3)]
    [InlineData("v2", "hist rwidth", 2.4775, 3.5225, 0, 3.15)]
    [InlineData("classic", "hist rwidth", 2.4, 3.6, 0, 3)]
    [InlineData("v2", "hist step", 2.45, 3.55, 0, 3.15)]
    [InlineData("v2", "hist at 0", -0.55, 0.55, 0, 2.1)]
    [InlineData("classic", "hist at 0", -0.6, 0.6, 0, 2)]
    [InlineData("v2", "hist density", 2.45, 3.55, 0, 10.5)]
    [InlineData("classic", "hist density", 2.4, 3.6, 0, 10)]
    [InlineData("v2", "hist spread rwidth", 0.91, 5.09, 0, 2.1)]
    [InlineData("classic", "hist spread rwidth", 1, 5, 0, 2)]
    [InlineData("v2", "hist2d", 2.5, 3.5, 1, 3)]
    [InlineData("classic", "hist2d", 2.4, 3.6, 1, 3)]
    [InlineData("v2", "hexbin", 2.67, 3.33, 0.9, 3.1)]
    [InlineData("classic", "hexbin", 2.7, 3.3, 1, 3)]
    public void ASampleOfOneValue_IsRangedByTheBinsItIsDrawnIn(string theme, string kind, double xLo, double xHi, double yLo, double yHi)
    {
        double[] flat = [3.0, 3.0, 3.0];
        var axes = new Axes();
        switch (kind)
        {
            case "hist": axes.Hist(flat); break;
            case "hist rwidth": axes.Hist(flat).RWidth = 0.5; break;
            case "hist step": axes.Hist(flat).HistType = HistType.Step; break;
            case "hist at 0": axes.Hist([0.0, 0.0]); break;
            case "hist density": axes.Hist(flat).Density = true; break;
            case "hist spread rwidth": axes.Hist([1.0, 2, 2, 3, 5]).RWidth = 0.5; break;
            case "hist2d": axes.Histogram2D(flat, [1.0, 2, 3]); break;
            default: axes.Hexbin(flat, [1.0, 2, 3]).GridSize = 5; break;
        }

        var range = axes.ComputeDataRanges(theme.AsTheme());

        // matplotlib's own sums differ in the last bits (a density over numpy's bin widths, a hexbin grid padded by 1e-9 of its width).
        foreach (var (expected, drawn) in new[] { (xLo, range.XMin), (xHi, range.XMax), (yLo, range.YMin), (yHi, range.YMax) })
            Assert.True(Math.Abs(expected - drawn) <= 1e-8, $"{kind}: expected {expected:R}, drawn {drawn:R}");
    }

    /// <summary>The library's own themes keep the library's rule (<see cref="Range1D.Normalized"/>): an empty axis is 0 to 1 and a
    /// flat one is opened by half a unit, before the theme's margin and nice bounds.</summary>
    [Fact]
    public void TheLibrarysOwnThemes_KeepTheLibrarysRule()
    {
        var empty = new Axes();
        var flat = new Axes();
        flat.Plot([1.0, 2.0], [3.0, 3.0]);
        var span = new Axes();
        span.Plot([1.0, 2.0], [2.5, 3.5]);
        var unit = new Axes();
        unit.Plot([0.0, 1.0], [0.0, 1.0]);

        Assert.Equal(unit.ComputeDataRanges(Theme.Default), empty.ComputeDataRanges(Theme.Default));
        Assert.Equal(span.ComputeDataRanges(Theme.Default).YMin, flat.ComputeDataRanges(Theme.Default).YMin);
        Assert.Equal(span.ComputeDataRanges(Theme.Default).YMax, flat.ComputeDataRanges(Theme.Default).YMax);
    }

    /// <summary>Under the library's own themes a series that holds no data ranges its axes as an axes holding no series does, 0 to 1:
    /// a count, a density, a rug, a strip of no group, an event plot of no row and an empirical distribution of no sample on both axes,
    /// and an event plot of rows without an event along its events (its rows still stand at their offsets).</summary>
    [Theory]
    [InlineData("countplot", true)]
    [InlineData("kdeplot", true)]
    [InlineData("rugplot", true)]
    [InlineData("stripplot-of-no-group", true)]
    [InlineData("eventplot-of-no-row", true)]
    [InlineData("ecdf", true)]
    [InlineData("eventplot", false)]
    public void UnderTheLibrarysOwnThemes_ASeriesWithNoData_RangesAsAnEmptyAxes(string kind, bool onBothAxes)
    {
        var empty = new Axes().ComputeDataRanges(Theme.Default);
        var range = WithNoData(kind).ComputeDataRanges(Theme.Default);

        Assert.Equal((empty.XMin, empty.XMax), (range.XMin, range.XMax));
        if (onBothAxes) Assert.Equal((empty.YMin, empty.YMax), (range.YMin, range.YMax));
    }
}
