// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A stack of bars worked out once: where each layer stands and where it ends, column by column
/// (<see cref="StackableExtensions"/> holds the rule).</summary>
/// <param name="Feet">One array per layer, as long as the first layer: the sum of the layers below it that point the same way.</param>
/// <param name="Ends">One array per layer, as long as the columns it holds a value in: its foot plus its own value.</param>
internal readonly record struct BarStack(double[][] Feet, double[][] Ends);
