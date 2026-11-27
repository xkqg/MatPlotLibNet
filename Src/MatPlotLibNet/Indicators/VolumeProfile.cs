// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Indicators;

/// <summary>Volume profile ("volume by price"): where, in price, the volume traded. The range of the closes is cut into equal
/// price bins and each bar's volume is added to the bin its close falls in (StockCharts' "Volume by Price"; binning by close,
/// not by every price a bar traded through).</summary>
/// <remarks>A bin takes the closes from its lower edge up to, but not including, its upper edge; the last bin includes its upper
/// edge, so the highest close is counted. A bar whose close or volume is missing (NaN) or infinite is not counted and does not
/// move the range; with no bar to count the profile is empty and nothing is drawn. When every close is the same there is no
/// range to cut, and the bins cover one unit centred on the price.
/// <para><see cref="Apply"/> draws the profile as horizontal bars, one for each bin, as thick as the bin, on the secondary X
/// axis (the top edge): the axis is inverted so the bars grow from the right edge of the plot, and spans four times the longest
/// bar so that bar covers a quarter of the plot. The profile therefore takes the secondary X axis for itself, and the price axis
/// it shares with the chart it is drawn on.</para></remarks>
public sealed class VolumeProfile : CandleIndicator<VolumeProfileResult>
{
    /// <summary>The <c>bins</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultBins = 12;

    /// <summary>The share of the plot's width the longest bar covers.</summary>
    internal const double LongestBarShare = 0.25;

    /// <summary>How opaque the bars are: translucent, so the price line shows through them.</summary>
    private const double BarAlpha = 0.4;

    private readonly int _bins;

    /// <summary>Creates a new volume profile.</summary>
    /// <param name="bars">Close and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="bins">The number of equal price bins, 1 or more. Default <see cref="DefaultBins"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bins"/> is below 1.</exception>
    public VolumeProfile(OhlcvBars bars, int bins = DefaultBins) : base(bars)
    {
        _bins = bins.ThrowIfBelow(1, nameof(bins));
        Label = "Volume profile";
    }

    /// <inheritdoc />
    public override VolumeProfileResult Compute()
    {
        int[] counted = [.. Enumerable.Range(0, Math.Min(BarCount, Volume.Length)).Where(i => double.IsFinite(Close[i]) && double.IsFinite(Volume[i]))];
        if (counted.Length == 0) return new VolumeProfileResult([], [], []);

        // The range of the closes; with none (every close the same, or a rounding error apart) a unit centred on the price.
        var (low, high) = new Range1D(counted.Min(i => Close[i]), counted.Max(i => Close[i])).OpenedWhenFlat();

        // Equal bins from the lowest close to the highest, numpy's: the edges its linspace lays, the last exactly the highest.
        var equal = new EqualWidthBins(low, high, _bins);
        double[] edges = equal.Edges();
        double[] centres = [.. Enumerable.Range(0, _bins).Select(k => (edges[k] + edges[k + 1]) / 2)];

        // A close on an edge belongs to the bin that starts there; the highest close, on the last edge, to the last bin. The
        // bin is settled against the very edges the result reports, so membership and edges cannot disagree.
        double[] volumes = new double[_bins];
        foreach (int i in counted) volumes[equal.IndexOf(Close[i])] += Volume[i];

        return new VolumeProfileResult(centres, edges, volumes);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var profile = Compute();
        if (profile.Volumes.Length == 0) return;

        var bars = new BarSeries(profile.BinCentres, profile.Volumes)
        {
            Orientation = BarOrientation.Horizontal,
            BarWidth = profile.BinEdges[1] - profile.BinEdges[0],
            Alpha = BarAlpha,
            Label = Label,
        };
        if (Color.HasValue) bars.Color = Color.Value;
        axes.AddXSecondarySeries(bars);

        // From 0 at the right edge to four times the longest bar at the left, so the longest bar covers a quarter of the plot.
        double longest = profile.Volumes.Max();
        var axis = axes.SecondaryXAxis!;
        axis.Min = 0;
        axis.Max = longest > 0 ? longest / LongestBarShare : 1;
        axis.Inverted = true;
    }
}
