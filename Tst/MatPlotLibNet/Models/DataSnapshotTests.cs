// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Data changed inside an array or a list the series holds passes no setter, so no change mark sees it: a
/// <see cref="DataSnapshot"/> copies every collection a series holds when it is drawn and tells, the next time, whether any
/// of it changed. Held for every series type under the drawn-property contract: a snapshot matches the series it was taken
/// of, drawing changes nothing it holds, and a value changed in place in any collection it holds is seen.</summary>
public sealed class DataSnapshotTests
{
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ASnapshot_MatchesTheSeriesItWasTakenOf_AndDrawingChangesNothingInIt(string type)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true);
        var series = DrawnSeriesFixtures.TheSeries(figure);
        _ = figure.ToSvg();

        var snapshot = DataSnapshot.Of(series);
        Assert.True(snapshot.Matches(series), $"{type}: a snapshot does not match the series it was just taken of.");

        _ = figure.ToSvg();
        Assert.True(snapshot.Matches(series), $"{type}: drawing the series changed what it holds.");
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void AValueChangedInPlace_InAnyCollectionTheSeriesHolds_IsSeen(string type)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true);
        var series = DrawnSeriesFixtures.TheSeries(figure);

        foreach (var field in DataSnapshot.Plan(series.GetType()))
        {
            var snapshot = DataSnapshot.Of(series);
            if (!InPlaceChange.Apply(field.GetValue(series))) continue;

            Assert.False(snapshot.Matches(series), $"{type}.{field.Name}: a value changed in place was not seen.");
        }
    }

    [Fact]
    public void EverySeriesType_HoldsItsDataInCollectionsTheSnapshotSees()
    {
        // The contract above is only as good as the fields it walks. Across the series types under the contract, 173 collection
        // fields can be changed in place by the fixture and are seen (measured when the snapshot was written); a snapshot that
        // stops walking some of them drops below that. A series type added later only adds to it.
        int changed = 0;
        foreach (var type in DrawnSeriesFixtures.DrawnTypes)
        {
            var series = DrawnSeriesFixtures.TheSeries(DrawnSeriesFixtures.Build(type.Name, everyPropertySet: true));
            foreach (var field in DataSnapshot.Plan(type))
            {
                var snapshot = DataSnapshot.Of(series);
                if (InPlaceChange.Apply(field.GetValue(series)) && !snapshot.Matches(series)) changed++;
            }
        }

        Assert.True(changed >= 173, $"Only {changed} collection fields were changed in place and seen across the series types.");
    }

    [Fact]
    public void AnItemAddedToAPublicList_IsSeen()
    {
        var series = new Text3DSeries([new Text3DAnnotation(1, 2, 3, "a")]);
        var snapshot = DataSnapshot.Of(series);

        series.Annotations.Add(new Text3DAnnotation(4, 5, 6, "b"));

        Assert.False(snapshot.Matches(series));
    }

    [Fact]
    public void ANodeAddedDeepInATree_IsSeen()
    {
        var leafs = new List<TreeNode> { new() { Label = "a", Value = 1 } };
        var root = new TreeNode { Label = "root", Children = [new TreeNode { Label = "branch", Children = leafs }] };
        var series = new TreemapSeries(root);
        var snapshot = DataSnapshot.Of(series);
        Assert.True(snapshot.Matches(series));

        leafs.Add(new TreeNode { Label = "b", Value = 2 });

        Assert.False(snapshot.Matches(series));
    }

    [Fact]
    public void AReadOnlyListTheCallerStillHolds_IsSeenWhenTheCallerChangesIt()
    {
        var trend = new List<double> { 1, 2, 3 };
        var tile = new StatTileSeries(42) { Trend = trend };
        var snapshot = DataSnapshot.Of(tile);

        trend[1] = 2.5;

        Assert.False(snapshot.Matches(tile));
    }

    [Fact]
    public void ACollectionReplacedByAnEqualOne_StillMatches_AndOneOfAnotherLength_DoesNot()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var line = new LineSeries(x, y);
        var snapshot = DataSnapshot.Of(line);

        y[2] = 6;   // the value it held
        Assert.True(snapshot.Matches(line));

        line.RowKeys = ["a", "b", "c"];
        Assert.False(snapshot.Matches(line));
    }

    [Fact]
    public void ANaNStaysANaN_AndANegativeZeroIsNotAZero()
    {
        // The comparison is bit for bit: NaN against NaN is no change, 0 against -0 is one (the SVG writes "-0").
        double[] y = [double.NaN, 0];
        var line = new LineSeries([1.0, 2], y);
        var snapshot = DataSnapshot.Of(line);
        Assert.True(snapshot.Matches(line));

        y[1] = -0.0;

        Assert.False(snapshot.Matches(line));
    }

    [Fact]
    public void ASnapshotOfOneObject_DoesNotMatchAnotherOfAnotherType()
    {
        var snapshot = DataSnapshot.Of(new LineSeries([1.0], [1.0]));

        Assert.False(snapshot.Matches(new ScatterSeries([1.0], [1.0])));
    }

    [Fact]
    public void AGridOfTheSameSizeInAnotherShape_OrAnArrayOfAnotherType_IsAChange()
    {
        var holder = new Holder { Values = new double[2, 3] };
        var snapshot = DataSnapshot.Of(holder);
        Assert.True(snapshot.Matches(holder));

        holder.Values = new double[3, 2];
        Assert.False(snapshot.Matches(holder));

        holder.Values = new long[2, 3];
        Assert.False(snapshot.Matches(holder));

        holder.Values = new double[2, 3];
        Assert.True(snapshot.Matches(holder));

        holder.Values = new double[2, 4];
        Assert.False(snapshot.Matches(holder));

        holder.Values = null;
        Assert.False(snapshot.Matches(holder));
    }

    [Fact]
    public void AListOfAnotherKind_OrOfAnotherLength_IsAChange()
    {
        var holder = new Holder { Items = new List<string> { "a", "b" } };
        var snapshot = DataSnapshot.Of(holder);

        holder.Items = new[] { "a", "b" };
        Assert.False(snapshot.Matches(holder));

        holder.Items = new List<string> { "a" };
        Assert.False(snapshot.Matches(holder));

        holder.Items = new List<string> { "a", "b", "c" };
        Assert.False(snapshot.Matches(holder));

        holder.Items = new List<string> { "a", "b" };
        Assert.True(snapshot.Matches(holder));
    }

    [Fact]
    public void ATreeDeeperThanTheCopyFollows_AlwaysCountsAsChanged()
    {
        // Past 32 levels the copy stops following and the deepest node counts as changed: drawn again, never stale.
        var node = new TreeNode { Label = "leaf" };
        for (int i = 0; i < 40; i++) node = new TreeNode { Label = $"level {i}", Children = [node] };
        var series = new TreemapSeries(node);

        Assert.False(DataSnapshot.Of(series).Matches(series));
    }

    [Fact]
    public void AStampedList_EnumeratesWithoutItsItemType_Too()
    {
        var list = new StampedList<string>();
        list.Add("a");

        var items = ((IEnumerable)list).GetEnumerator();

        Assert.True(items.MoveNext());
        Assert.Equal("a", items.Current);
        Assert.False(items.MoveNext());
    }

    /// <summary>An object outside the library with an array and a list of no fixed kind: what a snapshot of it compares is the
    /// same as for a series, whatever object it is taken of.</summary>
    private sealed class Holder
    {
        public Array? Values { get; set; }

        public IEnumerable<string>? Items { get; set; }
    }

    /// <summary>A draw copies a series' data for the kept drawing of its subplot, and a series that keeps an estimate (a density
    /// curve, violin bodies, a pair grid's diagonal densities) reads a copy of it for that too: such a series asked again while it
    /// holds what it held is handed the copy it was handed before, so the two keeps of one draw share one copy instead of taking
    /// two.</summary>
    [Theory]
    [MemberData(nameof(SeriesThatKeepAnEstimate))]
    public void ASeriesThatKeepsAnEstimate_AskedAgainWhileItHoldsTheSameData_IsHandedTheSameCopy(string type)
    {
        var series = KeepingAnEstimate(type, out _);

        var first = DataSnapshot.Of(series);

        Assert.Same(first, DataSnapshot.Of(series));
    }

    [Theory]
    [MemberData(nameof(SeriesThatKeepAnEstimate))]
    public void ASeriesThatKeepsAnEstimate_AskedAgainAfterAValueChangedInPlace_IsHandedANewCopyOfWhatItHoldsNow(string type)
    {
        var series = KeepingAnEstimate(type, out var values);
        var first = DataSnapshot.Of(series);

        values[1] = 7;
        var second = DataSnapshot.Of(series);

        Assert.NotSame(first, second);
        Assert.False(first.Matches(series));
        Assert.True(second.Matches(series));
        Assert.Same(second, DataSnapshot.Of(series));
    }

    /// <summary>A series nothing else reads a copy of in a draw keeps none: it is copied afresh each time, so a line changed in
    /// place is never compared twice before its copy is taken, and holds no copy past the kept drawing that holds one.</summary>
    [Fact]
    public void ASeriesThatKeepsNoEstimate_IsCopiedAfreshEachTime()
    {
        var line = new LineSeries([1.0, 2, 3], [4.0, 5, 6]);

        Assert.NotSame(DataSnapshot.Of(line), DataSnapshot.Of(line));
    }

    [Fact]
    public void AnObjectThatIsNoSeries_IsCopiedAfreshEachTime()
    {
        var holder = new Holder { Values = new double[2, 3] };

        Assert.NotSame(DataSnapshot.Of(holder), DataSnapshot.Of(holder));
    }

    public static TheoryData<string> SeriesThatKeepAnEstimate { get; } = new([nameof(KdeSeries), nameof(ViolinSeries), nameof(PairGridSeries)]);

    /// <summary>A series of <paramref name="type"/> over <paramref name="values"/>, an array it holds and reads its estimate from.</summary>
    private static ChartSeries KeepingAnEstimate(string type, out double[] values)
    {
        values = [1.0, 2, 2.5, 3, 7];
        return type switch
        {
            nameof(KdeSeries) => new KdeSeries(values),
            nameof(ViolinSeries) => new ViolinSeries([values]),
            _ => new PairGridSeries([values, [4.0, 1, 2, 6, 5]]) { DiagonalKind = PairGridDiagonalKind.Kde },
        };
    }
    [Fact]
    public void ARenderCacheOnASeries_IsNoPartOfItsData()
    {
        Assert.DoesNotContain(DataSnapshot.Plan(typeof(SurfaceSeries)), f => f.Name.Contains(nameof(SurfaceSeries.DrawnFaces), StringComparison.Ordinal));
    }

    /// <summary>The density a series keeps between draws is made from its data, not data of its own: estimating it again
    /// (here after a setting changed) leaves a snapshot of the series' data matching, so a kept drawing is not thrown away for it.</summary>
    [Fact]
    public void AKeptDensityEstimate_IsNoPartOfTheSeriesData()
    {
        var kde = new KdeSeries([1.0, 2, 2.5, 3, 7]);
        var violin = new ViolinSeries([[1.0, 2, 2.5, 3, 7]]);
        var grid = new PairGridSeries([[1.0, 2, 2.5, 3, 7], [4.0, 1, 2, 6, 5]]) { DiagonalKind = PairGridDiagonalKind.Kde, HueGroups = [0, 1, 0, 1, 0] };
        _ = kde.CurveUnder(ThemeLooks.Library);
        _ = violin.BodiesUnder(ThemeLooks.Library);
        _ = grid.DiagonalDensitiesUnder(ThemeLooks.Library);
        var kdeData = DataSnapshot.Of(kde);
        var violinData = DataSnapshot.Of(violin);
        var gridData = DataSnapshot.Of(grid);

        kde.Bandwidth = 0.5;
        violin.Widths = 0.9;
        grid.MarkerSize = 4;
        _ = kde.CurveUnder(ThemeLooks.Library);
        _ = violin.BodiesUnder(ThemeLooks.Library);
        _ = grid.DiagonalDensitiesUnder(ThemeLooks.Library);

        Assert.True(kdeData.Matches(kde));
        Assert.True(violinData.Matches(violin));
        Assert.True(gridData.Matches(grid));
    }
}
