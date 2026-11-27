// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a progress bar showing a value as a fraction of a track (0.0 to 1.0).</summary>
public sealed class ProgressBarSeries : ChartSeries, ISeriesKind<ProgressBarSeries>, IGivenTextSize
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The fraction of the track that the bar fills, from 0 to 1 (the constructor clamps what it is given to
    /// that range); it is also printed on the bar as a percentage.</summary>
    public double Value { get; }

    /// <summary>The value as the bar prints it: a whole percentage with the sign straight after the number (<c>62%</c>), in every culture.</summary>
    internal string ValueText => string.Invariant($"{Value:#,0%}");

    /// <summary>The colour of the filled part of the bar. Default Tab10 blue.</summary>
    public Color FillColor { get; set => Change(ref field, value); } = CyclePair.Standard.First;

    /// <summary>The colour of the unfilled track behind the bar. Default light gray.</summary>
    public Color TrackColor { get; set => Change(ref field, value); } = Colors.LightGray;

    /// <summary>Height of the bar as a fraction of the height of the plot, centred vertically. Default 0.3.</summary>
    public double BarHeight { get; set => Change(ref field, value); } = 0.3;

    /// <summary>Creates a new progress bar series with the given value (0.0 to 1.0).</summary>
    public ProgressBarSeries(double value) => Value = value.Saturate();

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<ProgressBarSeries>.Kind => "progressbar";

    /// <summary>The size of the percentage on the bar, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the percentage on the bar
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size the percentage takes under a theme that names none.</summary>
    private const double RestingFontSize = 11;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        GaugeValue = Value, FillColor = FillColor,
        TrackColor = TrackColor, BarHeight = BarHeight
    };

    /// <summary>The reader of a <see cref="ProgressBarSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries fill and track colours.</summary>
    internal static ProgressBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.ProgressBar(dto.GaugeValue ?? 0);
        if (dto.FillColor is { } fillColor) s.FillColor = fillColor;
        if (dto.TrackColor is { } trackColor) s.TrackColor = trackColor;
        if (dto.BarHeight is { } barHeight) s.BarHeight = barHeight;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>One row: the fraction the bar fills.</remarks>
    public override ChartDataTable? ToDataTable() =>
        new(null,
            [new("label", DataColumnKind.Text), new(Label ?? "value")],
            [[DataCell.FromText(Label ?? ""), DataCell.FromNumber(Value)]]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
