// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ichimoku Cloud (Ichimoku Kinko Hyo) indicator. Shows support/resistance, trend direction, and momentum.</summary>
/// <remarks><see cref="Compute"/> gives five lines: Tenkan-sen (conversion), Kijun-sen (base), Senkou Span A and B, and Chikou Span
/// (lagging). <see cref="Apply"/> draws all five, as pandas-ta's <c>ichimoku</c> returns them, and the cloud filled between Span A and B.
/// The Tenkan line is <see cref="Indicator.Color"/>, the Kijun line <see cref="KijunColor"/>, the cloud and the two spans that bound it
/// <see cref="CloudColor"/> and the lagging close <see cref="ChikouColor"/>; a part given no colour is drawn
/// <c>#0496ff</c> (Tenkan), <c>#991515</c> (Kijun), green (Span A and the cloud), red (Span B) and purple (Chikou) under the library's own themes, and
/// in the colours of the cycle under the matplotlib themes, as matplotlib draws a line and a fill that name no colour. The width and
/// the dash pattern the indicator was given reach every line.</remarks>
public sealed class Ichimoku : CandleIndicator<IchimokuResult>
{
    /// <summary>The <c>tenkanPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultTenkanPeriod = 9;

    /// <summary>The <c>kijunPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultKijunPeriod = 26;

    /// <summary>The <c>senkouBPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSenkouBPeriod = 52;

    /// <summary>The <c>displacement</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultDisplacement = 26;

    private readonly int _tenkanPeriod, _kijunPeriod, _senkouBPeriod, _displacement;

    /// <summary>The opacity of the cloud between the two spans.</summary>
    private const double CloudAlpha = 0.2;

    /// <summary>The colour of the Kijun (base) line. <see langword="null"/> (the default) draws it <c>#991515</c> under the library's own
    /// themes and in the next colour of the cycle under the matplotlib themes.</summary>
    public Color? KijunColor { get; set; }

    /// <summary>The colour of the cloud and of the two spans that bound it, which are one colour group. <see langword="null"/> (the default)
    /// draws them green under the library's own themes and in the next colour of the cycle under the matplotlib themes.</summary>
    public Color? CloudColor { get; set; }

    /// <summary>The colour of the Chikou (lagging close) line. <see langword="null"/> (the default) draws it purple under the library's own
    /// themes and in the next colour of the cycle under the matplotlib themes.</summary>
    public Color? ChikouColor { get; set; }

    /// <summary>Creates a new Ichimoku Cloud indicator with standard parameters.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="tenkanPeriod">Tenkan-Sen period (default <see cref="DefaultTenkanPeriod"/>).</param>
    /// <param name="kijunPeriod">Kijun-Sen period (default <see cref="DefaultKijunPeriod"/>).</param>
    /// <param name="senkouBPeriod">Senkou Span B period (default <see cref="DefaultSenkouBPeriod"/>).</param>
    /// <param name="displacement">Cloud displacement in bars (default <see cref="DefaultDisplacement"/>).</param>
    public Ichimoku(OhlcvBars bars,
        int tenkanPeriod = DefaultTenkanPeriod, int kijunPeriod = DefaultKijunPeriod, int senkouBPeriod = DefaultSenkouBPeriod, int displacement = DefaultDisplacement)
        : base(bars, Spec(tenkanPeriod, kijunPeriod, senkouBPeriod))
    {
        _tenkanPeriod = tenkanPeriod; _kijunPeriod = kijunPeriod;
        _senkouBPeriod = senkouBPeriod; _displacement = displacement;
    }

    internal static IndicatorSpec Spec(int tenkanPeriod, int kijunPeriod, int senkouBPeriod)
    {
        string label = "Ichimoku";
        return IndicatorSpec.Of(Math.Max(Math.Max(tenkanPeriod, kijunPeriod), senkouBPeriod) - 1, label, "Tenkan", "Kijun", "Span A", "Span B", "Chikou");
    }

    /// <inheritdoc />
    public override IchimokuResult Compute()
    {
        var tenkan = ComputeDonchianMid(_tenkanPeriod);
        var kijun = ComputeDonchianMid(_kijunPeriod);

        // Senkou Span A = (Tenkan + Kijun) / 2
        int kijunOffset = _kijunPeriod - _tenkanPeriod;
        var spanA = new double[Math.Max(0, Math.Min(kijun.Length, tenkan.Length - kijunOffset))];   // a value for every bar the base line has one
        for (int i = 0; i < spanA.Length; i++)
            spanA[i] = (tenkan[i + kijunOffset] + kijun[i]) / 2;

        // Senkou Span B = Donchian mid of senkouB period
        var spanB = ComputeDonchianMid(_senkouBPeriod);

        // Chikou Span = close (lagging line, plotted shifted back by displacement)
        var chikou = Close;

        return new IchimokuResult(tenkan, kijun, spanA, spanB, chikou);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        if (Close.Length < _senkouBPeriod) return;

        var result = Compute();

        // The lines in the order pandas-ta's ichimoku returns and matplotlib draws them (Span A, Span B, Tenkan, Kijun, Chikou), so that the
        // cycle gives each the colour matplotlib gives it, then the cloud filled between the two spans. The spans stand the displacement
        // ahead of the bar each value is worked out at, the lagging line the displacement behind.
        PlotLine(axes, result.SenkouSpanA, LineLabel(2), CloudColor, IndicatorLook.Library.Ichimoku.SpanA, _displacement);
        PlotLine(axes, result.SenkouSpanB, LineLabel(3), CloudColor, IndicatorLook.Library.Ichimoku.SpanB, _displacement);
        PlotLine(axes, result.TenkanSen, LineLabel(0), Color, IndicatorLook.Library.Ichimoku.Tenkan);
        PlotLine(axes, result.KijunSen, LineLabel(1), KijunColor, IndicatorLook.Library.Ichimoku.Kijun);
        if (Close.Length > _displacement)
            PlotLine(axes, Close[_displacement..], LineLabel(4), ChikouColor, IndicatorLook.Library.Ichimoku.Chikou, -_displacement);

        // The cloud lies between the spans where both have a value: their last values, as many as the shorter has.
        int cloudLen = Math.Min(result.SenkouSpanA.Length, result.SenkouSpanB.Length);
        if (cloudLen <= 0) return;

        var cloud = axes.AddSeries(AreaSeries.Band(Positions(cloudLen, _displacement), result.SenkouSpanA[^cloudLen..], result.SenkouSpanB[^cloudLen..], CloudAlpha));
        WearColor(cloud, CloudColor, IndicatorLook.Library.Ichimoku.SpanA);
        cloud.Label = "Cloud";
    }

}
