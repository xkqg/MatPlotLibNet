// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule for stacking bars: in each column a layer stands on the sum of the values the layers before it
/// hold there. The columns are the first layer's; a layer shorter than the first adds nothing past its end. The axes
/// renderer stands every layer on these feet (<see cref="BarSeries.StackedBottoms"/>), and the range of stacked bars takes
/// in every foot and every end they give, so the picture and the range cannot stack differently.</summary>
internal static class StackableExtensions
{
    extension(IReadOnlyList<IStackable> layers)
    {
        /// <summary>Where each layer of a stack stands, column by column.</summary>
        /// <returns>One array per layer, as long as the first layer: the sum of the layers below it in each column.</returns>
        internal double[][] StackedFeet()
        {
            var feet = new double[layers.Count][];
            var cumulative = new double[layers.Count > 0 ? layers[0].Values.Length : 0];
            for (int i = 0; i < layers.Count; i++)
            {
                feet[i] = (double[])cumulative.Clone();
                for (int c = 0; c < Math.Min(cumulative.Length, layers[i].Values.Length); c++)
                    cumulative[c] += layers[i].Values[c];
            }

            return feet;
        }

        /// <summary>Where each layer of a stack ends, column by column: its foot plus its own value, in the columns it has.</summary>
        /// <returns>One array per layer, as long as the columns it holds a value in.</returns>
        internal double[][] StackedEnds()
        {
            var feet = layers.StackedFeet();
            var ends = new double[layers.Count][];
            for (int i = 0; i < layers.Count; i++)
            {
                ends[i] = new double[Math.Min(feet[i].Length, layers[i].Values.Length)];
                for (int c = 0; c < ends[i].Length; c++)
                    ends[i][c] = feet[i][c] + layers[i].Values[c];
            }

            return ends;
        }
    }
}
