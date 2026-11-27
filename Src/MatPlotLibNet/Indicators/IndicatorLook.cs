// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>The colours the indicators choose for a part their caller gave no colour: the first line or band, the second line, the
/// lines of an Ichimoku cloud. An indicator does not know the theme when it draws, so it states a choice
/// (<see cref="Indicator.WearColor{TSeries}"/>) and never picks between the library's colour and the cycle's itself: the library's own themes
/// draw the choice, and the matplotlib themes draw the colour of the series' slot. Every indicator reads its colours here, one path:
/// its lines (<see cref="Lines"/>), a rise and a fall (<see cref="Moves"/>) and an Ichimoku's (<see cref="Ichimoku"/>).</summary>
internal sealed record IndicatorLook
{
    private IndicatorLook() { }

    /// <summary>The opacity of the fill of a band indicator (Bollinger bands, a Keltner channel) between its upper and its lower line
    /// unless the caller gives one: the default of their <c>Alpha</c> and of <see cref="Indicator.PlotBands"/>.</summary>
    internal const double BandOpacity = 0.15;

    /// <summary>The look the library's own themes draw an indicator in.</summary>
    internal static IndicatorLook Library { get; } = new();

    /// <summary>The colours of an indicator's lines: <see cref="CyclePair.First"/> for its first line, or its band and its midline;
    /// <see cref="CyclePair.Second"/> for its second line (a signal line, a %D line, a lead sine). The standard cycle's pair.</summary>
    internal CyclePair Lines { get; } = CyclePair.Standard;

    /// <summary>The colours of a rise and a fall an indicator draws (a +DI and a -DI, a stop below and above, a gain and a loss, a
    /// drawdown): the library's pair.</summary>
    internal UpDownColors Moves { get; } = UpDownColors.Standard;

    /// <summary>The colours of the lines and the cloud of an Ichimoku.</summary>
    internal IchimokuColors Ichimoku { get; } = IchimokuColors.Library;
}
