// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A gauge (speedometer) chart: a semi-circular dial with coloured range bands and a
/// value needle. The dial is divided into bands by <see cref="Ranges"/>; the needle points at
/// <see cref="Value"/> within the <c>[Min, Max]</c> range.</summary>
public sealed class GaugeSeries : ChartSeries, ISeriesKind<GaugeSeries>, IGivenTextSize
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>Current value indicated by the needle, within <c>[Min, Max]</c>.</summary>
    public double Value { get; }

    /// <summary>Lower bound of the dial. Defaults to <c>0</c>.</summary>
    public double Min { get; set => Change(ref field, value); }

    /// <summary>Upper bound of the dial. Defaults to <c>100</c>.</summary>
    public double Max { get; set => Change(ref field, value); } = 100;

    /// <summary>Optional coloured bands across the dial. Each <see cref="GaugeBand"/> defines the
    /// upper-bound threshold (in value space) and fill colour of one arc segment. Bands share
    /// endpoints: band <c>i</c>'s arc spans <c>[prevThreshold, bands[i].Threshold]</c>. Pass
    /// <see langword="null"/> (default) to use the built-in green/amber/red 60/80/100 bands.</summary>
    public GaugeBand[]? Ranges { get; set => Change(ref field, value); }

    /// <summary>Needle stroke colour. Defaults to <see cref="Colors.Black"/>.</summary>
    public Color NeedleColor { get; set => Change(ref field, value); } = Colors.Black;

    /// <summary>Creates a new gauge series displaying <paramref name="value"/> with the default
    /// <c>[0, 100]</c> range and black needle.</summary>
    /// <param name="value">Value pointed at by the needle. Clamped to <c>[Min, Max]</c> at render time.</param>
    public GaugeSeries(double value) => Value = value;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<GaugeSeries>.Kind => "gauge";

    /// <summary>The size of the value written under the hub, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the value written under the hub
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size the value takes under a theme that names none.</summary>
    private const double RestingFontSize = 12;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        GaugeValue = Value, GaugeMin = Min, GaugeMax = Max,
        NeedleColor = NeedleColor,
        // Null when the caller never set bands, so a default gauge emits no band bytes and its golden stays
        // byte-identical. An EMPTY array is a different intent ("no bands") and survives as empty.
        BandThresholds = Ranges?.Select(b => b.Threshold).ToArray(),
        BandColors = Ranges?.Select(b => b.Color).ToList()
    };

    /// <summary>The reader of a <see cref="GaugeSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries min/max range and needle colour.</summary>
    internal static GaugeSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Gauge(dto.GaugeValue ?? 0);
        if (dto.GaugeMin is { } gaugeMin) s.Min = gaugeMin;
        if (dto.GaugeMax is { } gaugeMax) s.Max = gaugeMax;
        if (dto.NeedleColor is { } needleColor) s.NeedleColor = needleColor;

        if (dto.BandThresholds is { } thresholds && dto.BandColors is { } colors)
        {
            int count = Math.Min(thresholds.Length, colors.Count);
            var bands = new GaugeBand[count];
            for (int i = 0; i < count; i++)
            {
                bands[i] = new GaugeBand(thresholds[i], colors[i]);
            }

            s.Ranges = bands;
        }

        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The needle's value and the dial it is read against.</remarks>
    public override ChartDataTable? ToDataTable() =>
        new(null,
            [new("label", DataColumnKind.Text), new(Label ?? "value"), new("min"), new("max")],
            [[DataCell.FromText(Label ?? ""), DataCell.FromNumber(Value), DataCell.FromNumber(Min), DataCell.FromNumber(Max)]]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
