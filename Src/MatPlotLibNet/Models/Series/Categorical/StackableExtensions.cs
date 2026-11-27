// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>The one rule for stacking bars: in each column a layer stands on the sum of the layers before it that point
/// the same way, so the layers above zero grow up from zero and the layers below zero grow down from zero, each on its own
/// running sum (a diverging stack, a Likert chart). A value of zero stands with the layers above zero; a value that is not a
/// finite number adds to neither sum, so the layers after it still stand on finite feet, and its own bar has no end. The
/// columns are the first layer's; a layer shorter than the first adds nothing past its end. The bars stand on these feet
/// (<see cref="BarArrangementExtensions.ArrangeBars"/>), and the range of stacked bars takes in every foot and every end
/// they give, so the picture and the range cannot stack differently.</summary>
internal static class StackableExtensions
{
    extension(IEnumerable<ISeries> series)
    {
        /// <summary>The layers of a stack, in the order they were added: the stackable series that have no foot of their own. A
        /// series with a <see cref="IStackable.StackBaseline"/> the user set floats on that foot, as matplotlib's
        /// <c>bottom=</c> lifts a bar, so it is neither stood on the layers before it nor stood on by the layers after it. The
        /// one rule for a floating bar, read by the arrangement of the bars and by the range that sums the stack.</summary>
        /// <returns>The series that stack.</returns>
        internal List<IStackable> StackLayers() => [.. series.OfType<IStackable>().Where(layer => layer.StackBaseline is null)];
    }

    extension(IAxesContext context)
    {
        /// <summary>The stack of the context's stacking series: the one a draw's context keeps (<see cref="IKeepsBarStack"/>),
        /// so every bar that asks for its range reads the same stack, or one worked out on the spot for a context that keeps
        /// none.</summary>
        internal BarStack BarStack => context is IKeepsBarStack kept ? kept.BarStack : context.AllSeries.StackLayers().Stack();
    }

    extension(IReadOnlyList<IStackable> layers)
    {
        /// <summary>Where each layer of a stack stands, column by column.</summary>
        /// <returns>One array per layer, as long as the first layer: in each column the sum of the layers below it that point
        /// the same way (the sum of those at or above zero for a layer at or above zero, of those below zero for one below).</returns>
        internal double[][] StackedFeet() => layers.Columns().StackedFeet();

        /// <summary>The stack of the layers: where each stands and where each ends.</summary>
        /// <returns>The feet and the ends.</returns>
        internal BarStack Stack() => layers.Columns().Stack();

        /// <summary>The values of each layer, read once.</summary>
        private double[][] Columns()
        {
            var columns = new double[layers.Count][];
            for (int i = 0; i < columns.Length; i++)
                columns[i] = layers[i].Values;
            return columns;
        }
    }

    extension(IReadOnlyList<double[]> layers)
    {
        /// <summary>Where each layer of a stack of plain columns stands (a stack the series model does not hold, the layers of
        /// a polar axes' bars): the same rule as <see cref="StackedFeet(IReadOnlyList{IStackable})"/>.</summary>
        /// <returns>One array per layer, as long as the first layer.</returns>
        internal double[][] StackedFeet()
        {
            int columns = layers.Count > 0 ? layers[0].Length : 0;
            var feet = new double[layers.Count][];
            var atOrAboveZero = new double[columns];
            var belowZero = new double[columns];
            for (int i = 0; i < layers.Count; i++)
            {
                feet[i] = new double[columns];
                for (int c = 0; c < columns; c++)
                {
                    double value = c < layers[i].Length ? layers[i][c] : 0;
                    feet[i][c] = value < 0 ? belowZero[c] : atOrAboveZero[c];
                    if (!double.IsFinite(value)) continue;
                    if (value < 0) belowZero[c] += value;
                    else atOrAboveZero[c] += value;
                }
            }

            return feet;
        }

        /// <summary>The stack of plain columns: where each layer stands, and where it ends, its foot plus its own value in the
        /// columns it has.</summary>
        /// <returns>The feet, one array per layer as long as the first layer, and the ends, one array per layer as long as the
        /// columns it holds a value in.</returns>
        internal BarStack Stack()
        {
            var feet = layers.StackedFeet();
            var ends = new double[layers.Count][];
            for (int i = 0; i < layers.Count; i++)
            {
                ends[i] = new double[Math.Min(feet[i].Length, layers[i].Length)];
                for (int c = 0; c < ends[i].Length; c++)
                    ends[i][c] = feet[i][c] + layers[i][c];
            }

            return new BarStack(feet, ends);
        }
    }
}
