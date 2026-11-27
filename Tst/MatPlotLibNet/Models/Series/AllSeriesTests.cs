// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies common <see cref="ISeries"/> behavior across all series types: one case per type the library ships, the instance
/// read from the one roster, <see cref="DrawnSeriesFixtures"/>, which <see cref="SeriesDrawnPropertiesRoundTripTests"/> holds to every
/// shipped type.</summary>
public class AllSeriesTests
{
    /// <summary>Verifies that exactly one series kind draws its own label (and so is never a legend entry): the stat
    /// tile. Every other kind lists its label in the legend, so none of them may answer true.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void DrawsOwnLabel_IsTrueForTheStatTileOnly(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        Assert.Equal(series is StatTileSeries, series is ChartSeries { DrawsOwnLabel: true });
    }

    /// <summary>Verifies that Accept dispatches to the correct visitor method for each series type.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void Accept_DispatchesToCorrectVisitorMethod(string type)
    {
        var visitor = new TestSeriesVisitor();
        DrawnSeriesFixtures.Instance(type).Accept(visitor, null!);
        Assert.Equal(type, visitor.LastVisited);
    }

    /// <summary>A base that holds the data of the kinds made on it: the arguments every such kind is made from, and the members that keep
    /// them, in the order given.</summary>
    private readonly record struct DataBase(Type Base, object[] Arguments, string[] Members);

    private static readonly double[] Ones = [1, 2, 3], Fours = [4, 5, 6], Sevens = [7, 8, 9], Tens = [10, 11, 12];
    private static readonly double[,] Grid = { { 1, 2, 3 }, { 4, 5, 6 } };

    private static readonly DataBase[] DataBases =
    [
        new(typeof(XYSeries), [Ones, Fours], [nameof(XYSeries.XData), nameof(XYSeries.YData)]),
        new(typeof(XYZSeries), [(Vec)Ones, (Vec)Fours, (Vec)Sevens], [nameof(XYZSeries.X), nameof(XYZSeries.Y), nameof(XYZSeries.Z)]),
        new(typeof(PolarSeries), [Ones, Fours], [nameof(PolarSeries.R), nameof(PolarSeries.Theta)]),
        new(typeof(DatasetSeries), [new[] { Ones, Fours }], [nameof(DatasetSeries.Datasets)]),
        new(typeof(HierarchicalSeries), [new TreeNode { Label = "Root" }], [nameof(HierarchicalSeries.Root)]),
        new(typeof(OhlcSeries), [Ones, Fours, Sevens, Tens], [nameof(OhlcSeries.Open), nameof(OhlcSeries.High), nameof(OhlcSeries.Low), nameof(OhlcSeries.Close)]),
        new(typeof(GridSeries3D), [Ones, Fours[..2], Grid], [nameof(GridSeries3D.X), nameof(GridSeries3D.Y), nameof(GridSeries3D.Z)]),
        new(typeof(GridContourSeries), [Ones, Fours[..2], Grid], [nameof(GridContourSeries.XData), nameof(GridContourSeries.YData), nameof(GridContourSeries.ZData)]),
        new(typeof(TernarySeries), [Ones, Fours, Sevens], [nameof(TernarySeries.Top), nameof(TernarySeries.Left), nameof(TernarySeries.Right)]),
    ];

    /// <summary>One row per base of <see cref="DataBases"/>, by name.</summary>
    public static TheoryData<string> DataBaseNames { get; } = new(DataBases.Select(data => data.Base.Name));

    /// <summary>Every kind of the roster made on a base that holds its data keeps, in the base's members, what its own constructor is
    /// given, in the order given: one case per base, each kind on it whose constructor takes exactly the base's data made through
    /// that constructor, and each member a kind does not keep named. A kind made from more than its base holds (a bubble's sizes, an
    /// error bar's arms, a signal's rate) says what it keeps in its own tests.</summary>
    [Theory]
    [MemberData(nameof(DataBaseNames))]
    public void EveryKindOnADataBase_KeepsWhatItIsMadeFrom_InTheBasesMembers(string baseName)
    {
        var data = DataBases.Single(b => b.Base.Name == baseName);
        Type[] signature = [.. data.Arguments.Select(argument => argument.GetType())];
        var kinds = DrawnSeriesFixtures.DrawnTypes.Where(type => data.Base.IsAssignableFrom(type) && type.GetConstructor(signature) is not null).ToList();

        var lost = kinds.SelectMany(type =>
        {
            var series = type.GetConstructor(signature)!.Invoke(data.Arguments);
            return data.Members.Where((member, i) => !DrawnSeriesFixtures.SameValue(data.Arguments[i], type.GetProperty(member)!.GetValue(series)))
                .Select(member => $"{type.Name}.{member}");
        }).ToList();

        Assert.NotEmpty(kinds);
        Assert.True(lost.Count == 0, $"{baseName}: {string.Join(", ", lost)} not kept as given.");
    }

    /// <summary>Every series writes its DTO under the type tag the registry reads it back by: the tag of each kind is held to its
    /// spelling by the golden corpus (<see cref="ChartSerializerGoldenCorpusTests"/>), and the kind it reads back as by
    /// <see cref="SeriesDrawnPropertiesRoundTripTests"/>, so no kind states its own tag or its registry row again.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ToSeriesDto_IsWrittenUnderATagTheRegistryReads(string type)
    {
        var dto = DrawnSeriesFixtures.Instance(type).ToSeriesDto();

        Assert.Contains(dto.Type, SeriesRegistry.Discriminators);
    }

    /// <summary>Every series with non-empty data produces a finite (no NaN, no infinity) <see cref="DataRangeContribution"/>, under
    /// axes that give no limits and under axes that do (the <c>context.XAxisMin ?? default</c> reads of the candle, Gantt and 3-D
    /// kinds take the other branch then). A member may be null when the series does not contribute to that axis (a polar series, a
    /// voxel series with an empty mask); a null is skipped, not failed on, and the first member that is not finite is named.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ComputeDataRange_UnderAxesWithAndWithoutLimits_IsFinite(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        TestAxesContext[] contexts = [new(), new(-100.0, 100.0, -100.0, 100.0)];

        foreach (var context in contexts)
        {
            foreach (var (name, value) in GivenMembers(series.ComputeDataRange(context)))
            {
                value.AssertFinite($"{type}.{name} under {(context.XAxisMin is null ? "no limits" : "limits")}");
            }
        }
    }

    /// <summary>The members of <paramref name="range"/> that are given (not null), by name.</summary>
    private static IEnumerable<(string Name, double Value)> GivenMembers(DataRangeContribution range) =>
        new (string Name, double? Value)[]
        {
            (nameof(range.XMin), range.XMin), (nameof(range.XMax), range.XMax), (nameof(range.YMin), range.YMin),
            (nameof(range.YMax), range.YMax), (nameof(range.ZMin), range.ZMin), (nameof(range.ZMax), range.ZMax),
            (nameof(range.StickyXMin), range.StickyXMin), (nameof(range.StickyXMax), range.StickyXMax),
            (nameof(range.StickyYMin), range.StickyYMin), (nameof(range.StickyYMax), range.StickyYMax),
            (nameof(range.StickyZMin), range.StickyZMin), (nameof(range.StickyZMax), range.StickyZMax),
        }.Where(member => member.Value.HasValue).Select(member => (member.Name, member.Value!.Value));

    /// <summary>Every series alone, with finite data, draws through the whole pipeline and writes no NaN or Infinity into its SVG (the
    /// scripts, which test <c>isNaN</c>, left out).</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void RendersToSvg_WithoutANonFiniteCoordinate(string type) =>
        AssertDrawnWithoutANonFiniteCoordinate(DrawnSeriesFixtures.Instance(type));

    /// <summary>A 3-D scatter of one point draws no non-finite coordinate: its projection fitted a box of no width and wrote
    /// <c>cx="NaN" cy="NaN"</c>. The roster's 3-D scatter holds three points, so this one is made here.</summary>
    [Fact]
    public void AOnePointScatter3D_RendersToSvg_WithoutANonFiniteCoordinate() =>
        AssertDrawnWithoutANonFiniteCoordinate(new Scatter3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }));

    private static void AssertDrawnWithoutANonFiniteCoordinate(ISeries series)
    {
        var svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series).WithTooltips())
            .Build()
            .ToSvg();

        Assert.StartsWith("<svg", svg);
        svg.AssertNoNonFiniteNumber();
    }
}
