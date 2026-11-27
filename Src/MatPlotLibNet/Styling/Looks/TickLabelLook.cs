// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How the labels of the ticks are written where their author says nothing.</summary>
internal sealed record TickLabelLook : IThemeKeys<TickLabelLook>
{
    /// <summary>The library's own labels: in full, with an ASCII hyphen.</summary>
    internal static TickLabelLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.LogLabelPowersThreshold = LogThresholds?.Powers;
        keys.LogLabelDecadesThreshold = LogThresholds?.Decades;
        keys.FormatterLimitLower = FormatterLimits?.Lower;
        keys.FormatterLimitUpper = FormatterLimits?.Upper;
        keys.OffsetThreshold = OffsetThreshold;
        keys.UnicodeMinus = UnicodeMinus.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static TickLabelLook ReadKeys(ThemeDto keys) => new()
    {
        LogThresholds = keys.LogLabelPowersThreshold is { } powers && keys.LogLabelDecadesThreshold is { } decades
            ? new LogLabelThresholds(powers, decades)
            : null,
        FormatterLimits = keys.FormatterLimitLower is { } lower && keys.FormatterLimitUpper is { } upper ? new PowerLimits(lower, upper) : null,
        OffsetThreshold = keys.OffsetThreshold,
        UnicodeMinus = keys.UnicodeMinus ?? false,
    };

    /// <summary>Which ticks of a log axis and of a colour bar of matplotlib's log norm are named (<c>LogFormatter.minor_thresholds</c>):
    /// (0, 0) under the classic style. Unset: the default style's (<see cref="Rendering.TickLocators.LogLabelThresholds.Default"/>).</summary>
    public LogLabelThresholds? LogThresholds { get; init; }

    /// <summary>The powers of ten beyond which the labels of a linear axis are written in scientific notation:
    /// matplotlib's <c>axes.formatter.limits</c>, -5 and 6 under the default style, -7 and 7 under the classic one. Unset: every
    /// label in full. <c>TickLabelExtensions.OrderOfMagnitudeIn</c> holds the rule.</summary>
    public PowerLimits? FormatterLimits { get; init; }

    /// <summary>How many leading digits the ticks of a linear axis must share before a round offset is taken off every label and
    /// written once at the end of the axis: matplotlib's <c>axes.formatter.offset_threshold</c>, 4 under the default style and 2
    /// under the classic one. Unset: every label in full and no offset.</summary>
    public int? OffsetThreshold { get; init; }

    /// <summary>Whether the hyphen of a negative number a tick label, an offset text or a colour bar's label writes is the true minus
    /// sign (U+2212), as matplotlib's <c>axes.unicode_minus</c> has it. Otherwise the ASCII hyphen. <see cref="Signed"/> holds the
    /// rule.</summary>
    public bool UnicodeMinus { get; init; }

    /// <summary>The true minus sign (U+2212): what matplotlib's <c>Formatter.fix_minus</c> writes for a hyphen, and what its math text
    /// and its log and logit formatters draw for a minus.</summary>
    internal const char TrueMinusSign = '\u2212';

    /// <summary>The number <paramref name="text"/> with the hyphen of its signs written as the true minus sign where this look says so
    /// (<see cref="UnicodeMinus"/>), as matplotlib's <c>fix_minus</c> does for the text of a <c>ScalarFormatter</c>: a tick label, the
    /// offset text ("1e-5") and a colour bar's label. The one rule; a text without a hyphen is returned as it is.</summary>
    /// <param name="text">The number as written.</param>
    /// <returns>The text.</returns>
    internal string Signed(string text) => UnicodeMinus && text.Contains('-') ? text.Replace('-', TrueMinusSign) : text;

    /// <summary>The labels of <paramref name="ticks"/> written in one number format (the same number of decimals for all,
    /// <c>TickLabelExtensions.UniformFormat</c>), as an axis writes its own, each signed by this look (<see cref="Signed"/>).</summary>
    /// <param name="ticks">The tick values.</param>
    /// <returns>One label per tick.</returns>
    internal string[] Labelled(double[] ticks)
    {
        var format = ticks.UniformFormat();
        string[] labels = new string[ticks.Length];
        for (int i = 0; i < labels.Length; i++) labels[i] = Signed(format(ticks[i]));
        return labels;
    }

    /// <summary>The decades a log axis labels: this look's thresholds, else the library's.</summary>
    /// <returns>The thresholds.</returns>
    internal Rendering.TickLocators.LogLabelThresholds DrawnLogThresholds => LogThresholds ?? Rendering.TickLocators.LogLabelThresholds.Default;
}
