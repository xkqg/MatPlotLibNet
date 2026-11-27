// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The ridgeline chart in one call: one filled density per group, each on a baseline of its own, the first group at
/// the top. Every ridge is estimated on ONE shared x grid (the pooled data range padded by three bandwidths) with ONE joint
/// bandwidth (the mean of the groups' own Silverman bandwidths, ggridges' rule, or the one given), and drawn to ONE height
/// scale, so the tallest ridge reaches the
/// next baseline: the ridges can be compared with each other, and their heights do not depend on the data's units. One ridge
/// is one AreaSeries, which draws its own outline, so it wears one colour. The group names stand on the Y axis.</summary>
public class RidgelineTests
{
    private static readonly NamedSample[] Groups =
    [
        new("North", [1.2, 1.9, 2.3, 2.4, 2.8, 3.1, 3.3, 3.6, 4.0, 4.4, 5.1, 5.9]),
        new("South", [3.5, 4.2, 4.6, 4.8, 5.0, 5.3, 5.9, 6.6]),
        new("East", [0.4, 0.9, 1.1, 1.3, 1.4, 1.6, 1.8, 2.2, 2.6, 3.4]),
    ];

    private static Axes Ridgeline(IReadOnlyList<NamedSample> groups, double? bandwidth = null, Action<AreaSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.Ridgeline(groups, bandwidth, configure));

    private static AreaSeries[] Ridges(Axes axes) => [.. axes.Series.Select(s => Assert.IsType<AreaSeries>(s))];

    /// <summary>How far each point of a ridge's top stands above its baseline.</summary>
    private static double[] Heights(AreaSeries ridge) => [.. ridge.YData.Select((y, j) => y - ridge.YData2![j])];

    private static double[] Pooled(IEnumerable<NamedSample> groups) => [.. groups.SelectMany(g => g.Values).Where(double.IsFinite).Order()];

    private static double[] Sorted(NamedSample group) => [.. group.Values.Where(double.IsFinite).Order()];

    private static double Joint(IEnumerable<NamedSample> groups) => GaussianKde.JointBandwidth([.. groups.Select(Sorted)]);

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>One AreaSeries per group, filled between the ridge's top and its baseline, and nothing else: no line on top
    /// for the outline (the area draws its own) and no label series. The ridges carry no legend label: the group names stand
    /// on the Y axis.</summary>
    [Fact]
    public void TheChart_IsOneAreaSeriesPerGroup_AndNothingElse()
    {
        var axes = Ridgeline(Groups);

        var ridges = Ridges(axes);
        Assert.Equal(3, ridges.Length);
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        Assert.All(ridges, r => Assert.NotNull(r.YData2));
        Assert.All(ridges, r => Assert.Null(r.Label));
    }

    /// <summary>Every ridge is estimated on the SAME grid: 200 evenly spaced points from the smallest observation of all the
    /// groups minus three bandwidths to the largest plus three (ggridges' whole-panel rule), so the ridges share their x
    /// values and can be compared point by point.</summary>
    [Fact]
    public void EveryRidge_StandsOnOneSharedGrid_OverThePooledDataPaddedByThreeBandwidths()
    {
        var ridges = Ridges(Ridgeline(Groups));
        double[] pooled = Pooled(Groups);
        double h = Joint(Groups);

        Assert.All(ridges, r => Assert.Equal(ridges[0].XData, r.XData));
        Assert.Equal(200, ridges[0].XData.Length);
        Assert.Equal(pooled[0] - 3 * h, ridges[0].XData[0]);
        Assert.Equal(pooled[^1] + 3 * h, ridges[0].XData[^1], 1e-12);
        Assert.Equal(GaussianKde.Grid(pooled[0], pooled[^1], h, 200), ridges[0].XData);
    }

    /// <summary>The grid stands on the smallest and the largest observation of the groups, taken from the ends of the groups
    /// (each is sorted already), bit for bit what the pooled and sorted observations give: here the smallest observation is in
    /// the last group, the largest in the middle one, and a group sits wholly inside the others.</summary>
    [Fact]
    public void TheGrid_StandsOnTheEndsOfTheGroups_NotOnAPooledSort()
    {
        NamedSample[] groups =
        [
            new("inside", [4.0, 5.0, 5.5, 6.0]),
            new("widest", [2.0, 3.0, 9.5, 21.25]),
            new("lowest", [-7.5, 1.0, 8.0, 8.5]),
        ];
        double[] pooled = Pooled(groups);
        double h = Joint(groups);

        var ridges = Ridges(Ridgeline(groups));

        Assert.Equal(-7.5, pooled[0]);
        Assert.Equal(21.25, pooled[^1]);
        Assert.Equal(PooledGrid(pooled, h, 200), ridges[0].XData);
    }

    /// <summary>The grid the verb has always drawn on, worked out from the pooled and sorted observations as before.</summary>
    private static double[] PooledGrid(double[] pooled, double bandwidth, int numPoints)
    {
        double lo = pooled[0] - 3 * bandwidth;
        double hi = pooled[^1] + 3 * bandwidth;
        double step = (hi - lo) / (numPoints - 1);
        double[] xs = new double[numPoints];
        for (int j = 0; j < numPoints; j++) xs[j] = lo + step * j;
        return xs;
    }

    /// <summary>A ridgeline of eight groups of 300 observations pools nothing: the shared grid needs only the smallest and the
    /// largest observation, which sit at the ends of the sorted groups, so the 2,400 observations are not copied into one array
    /// and sorted again (pooled, the verb allocated 243,504 bytes on this data; now 195,128).</summary>
    [Fact]
    public void TheVerb_DoesNotPoolAndSortTheObservations()
    {
        var random = new Random(7);
        NamedSample[] groups = [.. Enumerable.Range(0, 8).Select(g => new NamedSample($"g{g}", [.. Enumerable.Range(0, 300).Select(_ => g + random.NextDouble() * 10)]))];
        var warm = new AxesBuilder(new Axes());
        warm.Ridgeline(groups);

        var axes = new AxesBuilder(new Axes());
        long before = GC.GetAllocatedBytesForCurrentThread();
        axes.Ridgeline(groups);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 220_000, $"allocated {allocated}");
    }

    /// <summary>The bandwidth is ONE, joint: the mean of the groups' own Silverman bandwidths (ggridges' rule), used for every
    /// ridge. Each ridge is the group's density on the shared grid with that bandwidth, divided by the tallest density of
    /// all.</summary>
    [Fact]
    public void EveryRidge_IsItsGroupsDensity_WithOneJointBandwidth()
    {
        var ridges = Ridges(Ridgeline(Groups));
        double[] grid = ridges[0].XData;
        double h = Joint(Groups);
        double[][] densities = [.. Groups.Select(g => GaussianKde.DensityAt(Sorted(g), h, grid))];
        double tallest = densities.Max(d => d.Max());

        for (int g = 0; g < Groups.Length; g++)
        {
            Assert.NotEqual(h, GaussianKde.Bandwidth(Sorted(Groups[g]), GaussianKde.SilvermanFactor));
            double[] expected = [.. densities[g].Select(d => d / tallest)];
            Assert.Equal(expected, Heights(ridges[g]), (a, b) => Math.Abs(a - b) <= 1e-12);
        }
    }

    /// <summary>A bandwidth given by the caller is used for every ridge instead of the pooled one, and pads the grid.</summary>
    [Fact]
    public void AGivenBandwidth_IsUsedForEveryRidge_AndPadsTheGrid()
    {
        var ridges = Ridges(Ridgeline(Groups, bandwidth: 0.5));
        double[] pooled = Pooled(Groups);
        double[] grid = ridges[0].XData;
        double[][] densities = [.. Groups.Select(g => GaussianKde.DensityAt(Sorted(g), 0.5, grid))];
        double tallest = densities.Max(d => d.Max());

        Assert.Equal(pooled[0] - 1.5, grid[0]);
        Assert.Equal(pooled[^1] + 1.5, grid[^1], 1e-12);
        for (int g = 0; g < Groups.Length; g++)
        {
            Assert.Equal([.. densities[g].Select(d => d / tallest)], Heights(ridges[g]), (a, b) => Math.Abs(a - b) <= 1e-12);
        }
    }

    /// <summary>The shift between the groups is not spread (M6): the same sample at 0, 50 and 100 draws three ridges of the
    /// same shape, each as wide as the one sample's own bandwidth. Silverman over the groups pooled would count the shift as
    /// spread and smooth every ridge about ten times wider; measured on three months of daily highs, January's ridge (its
    /// observations between 0.9 and 3.4) ran from -10 to 15.</summary>
    [Fact]
    public void AShiftBetweenTheGroups_IsNotSmoothedIntoTheRidges()
    {
        double[] sample = [1.0, 1.5, 2.5, 3.0, 4.5];
        NamedSample[] shifted = [new("a", sample), new("b", [.. sample.Select(v => v + 50)]), new("c", [.. sample.Select(v => v + 100)])];

        var ridges = Ridges(Ridgeline(shifted));

        double own = GaussianKde.Bandwidth(sample, GaussianKde.SilvermanFactor);
        double[] grid = ridges[0].XData;
        Assert.Equal(1.0 - 3 * own, grid[0], 1e-12);
        Assert.Equal(104.5 + 3 * own, grid[^1], 1e-12);
        double[][] densities = [.. shifted.Select(g => GaussianKde.DensityAt(g.Values, own, grid))];
        double tallest = densities.Max(d => d.Max());
        for (int g = 0; g < shifted.Length; g++)
        {
            Assert.Equal([.. densities[g].Select(d => d / tallest)], Heights(ridges[g]), (a, b) => Math.Abs(a - b) <= 1e-12);
        }
    }

    // ---- one height scale -------------------------------------------------------------------------------------------

    /// <summary>ONE height scale for all the ridges: the tallest ridge reaches the next baseline exactly, and no ridge reaches
    /// further, so a ridge never runs into the one above it and the heights compare across groups.</summary>
    [Fact]
    public void TheTallestRidge_ReachesTheNextBaselineExactly_AndNoRidgeReachesFurther()
    {
        var ridges = Ridges(Ridgeline(Groups));

        double tallest = ridges.Max(r => Heights(r).Max());
        Assert.Equal(1.0, tallest);
        Assert.All(ridges, r => Assert.All(Heights(r), height => Assert.InRange(height, 0, 1)));
        var top = ridges.Single(r => Heights(r).Max() == 1.0);
        Assert.Contains(top.YData2![0] + 1, top.YData);
    }

    /// <summary>The heights do not depend on the data's units (council B7): the raw density of the same data in units 100
    /// times smaller is 100 times lower (0.2449 against 0.002449 in the council's measurement), but every ridge is drawn to
    /// the same height, and the grid is the same grid in the new units.</summary>
    [Fact]
    public void TheHeights_DoNotDependOnTheDataUnits()
    {
        NamedSample[] scaled = [.. Groups.Select(g => new NamedSample(g.Label, [.. g.Values.Select(v => v * 100)]))];
        var ridges = Ridges(Ridgeline(Groups));
        var scaledRidges = Ridges(Ridgeline(scaled));

        double h = Joint(Groups);
        double h100 = Joint(scaled);
        double rawPeak = GaussianKde.DensityAt(Sorted(Groups[0]), h, ridges[0].XData).Max();
        double rawPeak100 = GaussianKde.DensityAt(Sorted(scaled[0]), h100, scaledRidges[0].XData).Max();
        Assert.Equal(100, rawPeak / rawPeak100, 1e-9);

        for (int g = 0; g < Groups.Length; g++)
        {
            Assert.Equal(Heights(ridges[g]), Heights(scaledRidges[g]), (a, b) => Math.Abs(a - b) <= 1e-9);
            Assert.Equal(ridges[g].XData.Select(x => x * 100), scaledRidges[g].XData, (a, b) => Math.Abs(a - b) <= 1e-9 * Math.Abs(a));
        }
    }

    // ---- where the ridges stand -----------------------------------------------------------------------------------

    /// <summary>The first group is at the TOP: each ridge stands on the centre of a category slot, the first group on the
    /// highest, the last on the lowest (0.5).</summary>
    [Fact]
    public void TheFirstGroup_IsAtTheTop_EachRidgeOnTheCentreOfASlot()
    {
        var ridges = Ridges(Ridgeline(Groups));

        Assert.Equal([2.5, 1.5, 0.5], ridges.Select(r => r.YData2![0]));
        Assert.All(ridges, r => Assert.All(r.YData2!, b => Assert.Equal(r.YData2![0], b)));
        Assert.Equal(CategorySlot.Of(2).Centre, ridges[0].YData2![0]);
    }

    /// <summary>The group names name the baselines on the Y axis, the first at the top: a built-in locator and formatter made
    /// as one pair by the category-ticks owner, so they survive JSON.</summary>
    [Fact]
    public void TheGroupNames_NameTheBaselines_OnTheYAxis()
    {
        var axis = Ridgeline(Groups).YAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, Groups.Length);
        Assert.Equal([0.5, 1.5, 2.5], ticks);
        Assert.Equal(["East", "South", "North"], ticks.Select(axis.TickFormatter!.Format));
    }

    /// <summary>Group names wider than the theme's usual left margin stand whole in the picture: the layout measures the names
    /// the verb sets as ticks, so the margin grows to hold them, with the Y axis title past them.</summary>
    [Fact]
    public void LongGroupNames_StandWholeInsideTheFigure()
    {
        string[] names = ["Northern hemisphere, high latitudes", "Equatorial belt", "Southern hemisphere"];
        NamedSample[] named = [.. Groups.Select((g, i) => new NamedSample(names[i], g.Values))];

        string svg = Plt.Create().WithSize(900, 600).AddSubPlot(1, 1, 1, ax => ax.Ridgeline(named).SetYLabel("Region")).ToSvg();

        var drawn = svg.Elements("text").Where(text => text["text-anchor"] == "end" && names.Contains(text.Text)).ToList();
        Assert.Equal(names.Length, drawn.Count);
        Assert.All(drawn, text =>
        {
            double start = text.Number("x")
                - FontMetricsDefault.Current.Measure(text.Text, new MatPlotLibNet.Styling.Font { Size = text.Number("font-size") }).Width;
            Assert.True(start >= 0, $"'{text.Text}' starts at {start}, left of the figure");
        });
    }

    // ---- the picture: order, colour, outline ----------------------------------------------------------------------

    /// <summary>The picture with the legend toggle on: that is what gives every series group its <c>data-series-index</c>.</summary>
    private static string Svg(IReadOnlyList<NamedSample> groups)
    {
        var figure = Plt.Create().WithSize(900, 600).AddSubPlot(1, 1, 1, ax => ax.Ridgeline(groups)).Build();
        figure.EnableLegendToggle = true;
        return figure.ToSvg();
    }

    /// <summary>Lower ridges are drawn in FRONT (plan Q6): the ridges are painted from the top one down, so each lower ridge
    /// is painted over the one above it where the two meet. Read on the picture: in the order the polygons are written, their
    /// baselines (the lowest pixel of each) go down the plot, and the first is the first group's.</summary>
    [Fact]
    public void LowerRidges_AreDrawnInFront()
    {
        string svg = Svg(Groups);

        double[] baselines = [.. Polygons(svg).Select(p => p.Max(point => point.Y))];
        Assert.Equal(3, baselines.Length);
        Assert.True(baselines[0] < baselines[1] && baselines[1] < baselines[2],
            $"the ridges were painted with baselines at {string.Join(", ", baselines)} px: not top to bottom");
        Assert.Equal([0, 1, 2], SeriesIndices(svg));
    }

    /// <summary>One ridge, one colour: three ridges wear three colours, and each ridge's fill and outline are that one colour
    /// (the area draws its own outline, right after its fill, so no ridge's outline is painted before another ridge's fill).</summary>
    [Fact]
    public void EveryRidge_WearsOneColourOfItsOwn_ItsFillAndOutlineTogether()
    {
        string svg = Svg(Groups);

        var colours = new List<string>();
        for (int g = 0; g < Groups.Length; g++)
        {
            string group = svg.SeriesGroup(g);
            string fill = group.Elements("polygon").Select(polygon => polygon["fill"]).FirstOrDefault(colour => colour?.StartsWith('#') == true) ?? "";
            string stroke = group.Elements("polyline").Select(polyline => polyline["stroke"]).FirstOrDefault(colour => colour?.StartsWith('#') == true) ?? "";
            Assert.NotEqual("", fill);
            Assert.Equal(fill, stroke);
            Assert.True(group.IndexOf("<polygon", StringComparison.Ordinal) < group.IndexOf("<polyline", StringComparison.Ordinal));
            colours.Add(fill);
        }

        Assert.Equal(3, colours.Distinct().Count());
    }

    /// <summary>A group without a name is drawn as a ridge without a name: its tick on the Y axis reads nothing.</summary>
    [Fact]
    public void AGroupWithoutAName_IsARidgeWithoutAName()
    {
        NamedSample[] groups = [Groups[0], new(null!, Groups[1].Values)];

        var axes = Ridgeline(groups);
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Ridgeline(groups)).ToSvg();

        Assert.Equal(2, axes.Series.Count);
        Assert.True(string.IsNullOrEmpty(axes.YAxis.TickFormatter!.Format(0.5)));
        Assert.Equal("North", axes.YAxis.TickFormatter.Format(1.5));
        Assert.Contains(">North<", svg);
    }

    // ---- missing values -------------------------------------------------------------------------------------------

    /// <summary>A missing observation (NaN) is not there, and an infinity cannot be placed: both are left out of the
    /// estimate, as a box, a violin and a density curve leave them out. The ridges are exactly the ridges without them.</summary>
    [Fact]
    public void AMissingOrInfiniteObservation_IsLeftOut()
    {
        NamedSample[] gappy =
        [
            new("North", [double.NaN, .. Groups[0].Values]),
            new("South", [.. Groups[1].Values[..3], double.PositiveInfinity, .. Groups[1].Values[3..], double.NegativeInfinity]),
            new("East", [.. Groups[2].Values, double.NaN]),
        ];

        var ridges = Ridges(Ridgeline(Groups));
        var gappyRidges = Ridges(Ridgeline(gappy));

        for (int g = 0; g < Groups.Length; g++)
        {
            Assert.Equal(ridges[g].XData, gappyRidges[g].XData);
            Assert.Equal(ridges[g].YData, gappyRidges[g].YData);
            Assert.Equal(ridges[g].YData2, gappyRidges[g].YData2);
        }

        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Ridgeline(gappy)).ToSvg().AssertNoNonFiniteNumber();
    }

    /// <summary>Data without a spread (every observation of every group the same value) has no σ; the size of the value
    /// stands in for it (R's <c>bw.nrd0</c> rule, <see cref="GaussianKde.Bandwidth"/>), so the ridges are a bump of
    /// that width around the value, and the same data in other units draws the same ridges on the same grid in those units.
    /// A fixed width would draw 5 as 5 ± 3 and 500 as 500 ± 3.</summary>
    [Fact]
    public void DataWithoutSpread_IsDrawnAtTheScaleOfItsValue()
    {
        NamedSample[] flat = [new("a", [5.0, 5.0, 5.0]), new("b", [5.0, 5.0])];
        NamedSample[] flat100 = [new("a", [500.0, 500.0, 500.0]), new("b", [500.0, 500.0])];

        var ridges = Ridges(Ridgeline(flat));
        var ridges100 = Ridges(Ridgeline(flat100));

        double h = 1.06 * 5 * Math.Pow(5, -0.2);
        Assert.Equal(5 - 3 * h, ridges[0].XData[0], 1e-12);
        Assert.Equal(5 + 3 * h, ridges[0].XData[^1], 1e-12);
        for (int g = 0; g < flat.Length; g++)
        {
            Assert.Equal(1.0, Heights(ridges[g]).Max(), 1e-12);
            Assert.Equal(Heights(ridges[g]), Heights(ridges100[g]), (a, b) => Math.Abs(a - b) <= 1e-9);
            Assert.Equal(ridges[g].XData.Select(x => x * 100), ridges100[g].XData, (a, b) => Math.Abs(a - b) <= 1e-9 * Math.Abs(a));
        }
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>A density is estimated from two observations or more: a group with fewer that can be drawn (NaN and the
    /// infinities are left out) is refused, naming the group.</summary>
    [Theory]
    [InlineData(new double[0], "0 observations")]
    [InlineData(new[] { double.NaN, 4.2 }, "1 observation")]
    public void AGroupWithFewerThanTwoObservations_IsRefused_NamingTheGroup(double[] values, string held)
    {
        NamedSample[] groups = [Groups[0], new("South", values), Groups[2]];

        var error = Assert.Throws<ArgumentException>(() => Ridgeline(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal($"Group 'South' holds {held}; a density estimate needs at least 2; NaN and the infinities are not counted. (Parameter 'groups')", error.Message);
    }

    /// <summary>A group whose values are missing altogether is refused by name, not with a null reference.</summary>
    [Fact]
    public void AGroupWithoutValues_IsRefused_NamingTheGroup()
    {
        NamedSample[] groups = [Groups[0], new("South", null!)];

        var error = Assert.Throws<ArgumentException>(() => Ridgeline(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal("Group 'South' carries no values. (Parameter 'groups')", error.Message);
    }

    /// <summary>A bandwidth is a width: a finite number above 0.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABandwidthThatIsNotAFiniteNumberAbove0_IsRefused(double bandwidth)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Ridgeline(Groups, bandwidth));

        Assert.Equal("bandwidth", error.ParamName);
    }

    /// <summary>Observations spread so far apart that their spread does not fit in a double give no bandwidth and no grid:
    /// refused, rather than drawing nothing.</summary>
    [Fact]
    public void ObservationsSpreadBeyondADouble_AreRefused()
    {
        NamedSample[] groups = [new("wide", [-1e308, 1e308]), new("narrow", [0.0, 1.0])];

        var error = Assert.Throws<ArgumentException>(() => Ridgeline(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(error);
        Assert.EndsWith(" of these observations is beyond what a double holds (got Infinity). (Parameter 'groups')", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A bandwidth so narrow that one kernel divided by it is beyond a double (the smallest positive double) gives
    /// no finite density: refused by name, rather than drawing nothing.</summary>
    [Fact]
    public void ABandwidthTooNarrowForADouble_IsRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Ridgeline(Groups, double.Epsilon));

        Assert.IsNotType<ArgumentOutOfRangeException>(error);
        Assert.Equal("bandwidth", error.ParamName);
    }

    // ---- the public type ------------------------------------------------------------------------------------------

    /// <summary>A named sample is a public readonly record struct of a name and a sample of free length, made like a
    /// <see cref="BarGroup"/>.</summary>
    [Fact]
    public void NamedSample_IsAPublicReadonlyRecordStruct_OfALabelAndItsValues()
    {
        var type = typeof(NamedSample);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute)));
        var sample = new NamedSample("a", [1.0, 2.0, 3.0]);
        var (label, values) = sample;
        Assert.Equal("a", label);
        Assert.Equal([1.0, 2.0, 3.0], values);
        Assert.Equal(["Label", "Values"], type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).Order());
    }

    // ---- reading the picture ----------------------------------------------------------------------------------------
    /// <summary>The series indices of the series groups, in the order they are written.</summary>
    private static int[] SeriesIndices(string svg) =>
        [.. svg.Elements("g").Where(group => group["class"] == "series").Select(group => (int)group.Number("data-series-index"))];

    /// <summary>The filled polygons of the picture, in the order they are written, each as its points in pixels.</summary>
    private static Point[][] Polygons(string svg) =>
        [.. svg.Elements("polygon").Select(polygon => polygon.Points())];
}
