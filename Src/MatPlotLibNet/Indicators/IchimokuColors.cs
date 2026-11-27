// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>The colours an Ichimoku chooses for the parts its caller gave none: the two spans and the cloud between them, the
/// conversion and base lines, and the lagging close.</summary>
/// <param name="SpanA">The first leading span, and the cloud it bounds.</param>
/// <param name="SpanB">The second leading span.</param>
/// <param name="Tenkan">The conversion line.</param>
/// <param name="Kijun">The base line.</param>
/// <param name="Chikou">The lagging close.</param>
internal readonly record struct IchimokuColors(Color SpanA, Color SpanB, Color Tenkan, Color Kijun, Color Chikou)
{
    /// <summary>The colours the library's own themes draw an Ichimoku in: the cycle's green and red for the spans, a bright blue and a
    /// dark red for the two lines (which are no colours of the cycle), the cycle's purple for the lagging close.</summary>
    internal static IchimokuColors Library { get; } = new(
        Colors.Tab10Green, Colors.Tab10Red, Color.FromHex("#0496ff"), Color.FromHex("#991515"), Colors.Tab10Purple);
}
