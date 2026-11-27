// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for an <see cref="AxisBreak"/>: an entry of an axes' <c>xBreaks</c> or <c>yBreaks</c>. The
/// style travels as a lower-case name; a name that is missing or unknown (the unknown one announced) reads back as the zigzag, the style a new break has.</summary>
internal sealed record AxisBreakDto(double From, double To, string? Style)
{
    /// <summary>The JSON's form of <paramref name="axisBreak"/>. Named <c>Of</c> because <c>From</c> is the record's own lower
    /// bound.</summary>
    /// <param name="axisBreak">The break.</param>
    internal static AxisBreakDto Of(AxisBreak axisBreak) =>
        new(axisBreak.From, axisBreak.To, axisBreak.Style.ToWireName());

    /// <summary>The break this record describes.</summary>
    internal AxisBreak ToAxisBreak() =>
        new(From, To, Style.WireEnum<BreakStyle>() ?? BreakStyle.Zigzag);
}
