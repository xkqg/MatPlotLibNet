// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The powers of ten beyond which the tick labels of a linear axis are written in scientific notation (matplotlib's
/// <c>axes.formatter.limits</c>): when the largest tick in view has an order of magnitude at or below <paramref name="Lower"/> or at or
/// above <paramref name="Upper"/>, every label is written divided by that power and the power is written once at the end of the axis
/// (<c>ScalarFormatter._set_order_of_magnitude</c>). Held by <see cref="TickLabelLook.FormatterLimits"/>.</summary>
/// <param name="Lower">The order of magnitude at or below which the labels are scaled: -5 under the default style, -7 under the classic one.</param>
/// <param name="Upper">The order of magnitude at or above which the labels are scaled: 6 under the default style, 7 under the classic one.</param>
internal readonly record struct PowerLimits(int Lower, int Upper);