// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The centre line and the two control limits of a control chart, in the units of its values, given to
/// <see cref="XYAxesExtensions.ControlChart"/> to draw them as they are instead of estimating them from the values: the
/// limits of an earlier period the process is held to, or limits set by a specification.
/// <para>The three are finite numbers with <see cref="Lower"/> at or below <see cref="Center"/> and <see cref="Center"/> at
/// or below <see cref="Upper"/>; the verb refuses any other.</para></summary>
/// <param name="Center">The centre line: where the process is expected to run.</param>
/// <param name="Lower">The lower control limit (LCL): a value below it is a violation.</param>
/// <param name="Upper">The upper control limit (UCL): a value above it is a violation.</param>
public readonly record struct ControlLimits(double Center, double Lower, double Upper);
