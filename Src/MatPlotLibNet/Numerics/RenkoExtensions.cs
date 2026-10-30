// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>One brick of a Renko chart: a price range exactly one brick size tall, drawn one column to the right of the brick
/// before it.</summary>
/// <param name="Rising"><see langword="true"/> when the price rose to complete the brick, <see langword="false"/> when it fell.</param>
/// <param name="Bottom">The lowest price of the brick.</param>
/// <param name="Top">The highest price of the brick, one brick size above <paramref name="Bottom"/>.</param>
/// <param name="SourceIndex">The index of the closing price that completed the brick; one close can complete several bricks.</param>
internal readonly record struct RenkoBrick(bool Rising, double Bottom, double Top, int SourceIndex);

/// <summary>The one owner of how a series of closing prices becomes the bricks of a Renko chart (close only; time is left out,
/// so the position of a brick along the chart is its index). The chart is anchored on the first close, unrounded, and every
/// brick edge is that anchor plus a whole number of brick sizes (<see cref="PriceStepExtensions"/>, tolerant of a decimal
/// size). Before the first brick a close at or above the anchor plus a brick, or at or below the anchor minus a brick, draws
/// one brick for every whole brick it covers. After it, a close continues the direction of the last brick when it is a brick
/// beyond that brick's far edge, and reverses it when it is a brick beyond its near edge, which is two bricks from the far
/// edge, so a reversing brick never overlaps the one before it and its corner touches that brick's corner; a close that
/// reaches neither is ignored. A brick is reached when the price equals its trigger.</summary>
internal static class RenkoExtensions
{
    extension(double[] prices)
    {
        /// <summary>The bricks the prices draw.</summary>
        /// <param name="brickSize">The price one brick covers; a finite number above zero (the verb refuses any other).</param>
        /// <returns>The bricks left to right; none when the prices never move a brick from the first one. A price that is not a
        /// finite number is skipped.</returns>
        internal IReadOnlyList<RenkoBrick> RenkoBricks(double brickSize)
        {
            var bricks = new List<RenkoBrick>();
            bool anchored = false;
            double anchor = 0;
            // The last brick's edges as whole bricks from the anchor; both 0 until the first brick is drawn.
            int top = 0, bottom = 0;
            for (int i = 0; i < prices.Length; i++)
            {
                double close = prices[i];
                if (!double.IsFinite(close))
                {
                    continue;
                }

                if (!anchored)
                {
                    anchor = close;
                    anchored = true;
                    continue;
                }

                int reaches = close.FloorStep(anchor, brickSize), falls = close.CeilingStep(anchor, brickSize);
                if (reaches > top)
                {
                    for (int edge = top; edge < reaches; edge++)
                    {
                        bricks.Add(new RenkoBrick(true, edge.LevelFrom(anchor, brickSize), (edge + 1).LevelFrom(anchor, brickSize), i));
                    }

                    top = reaches;
                    bottom = reaches - 1;
                }
                else if (falls < bottom)
                {
                    for (int edge = bottom; edge > falls; edge--)
                    {
                        bricks.Add(new RenkoBrick(false, (edge - 1).LevelFrom(anchor, brickSize), edge.LevelFrom(anchor, brickSize), i));
                    }

                    bottom = falls;
                    top = falls + 1;
                }
            }

            return bricks;
        }
    }
}
