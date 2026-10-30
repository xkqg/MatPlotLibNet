// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>One vertical piece of a Kagi chart: the part of a leg that has one width, in the column of the leg. A leg that
/// changes width part way is two pieces that meet at the price where it changes.</summary>
/// <param name="Column">The leg's column, from 0.</param>
/// <param name="From">The price the piece begins at, in the direction the leg is drawn.</param>
/// <param name="To">The price the piece ends at.</param>
/// <param name="Thick">Whether the piece is drawn thick.</param>
internal readonly record struct KagiSegment(int Column, double From, double To, bool Thick);

/// <summary>The short horizontal line that joins a leg to the next one, at the price the first leg turned.</summary>
/// <param name="Column">The column of the leg it leaves; it ends in the next column.</param>
/// <param name="Price">The price of the turn: the shoulder after an up leg, the waist after a down leg.</param>
/// <param name="Thick">Whether it is drawn thick: the width the leg it leaves ended in.</param>
internal readonly record struct KagiConnector(int Column, double Price, bool Thick);

/// <summary>What a series of closing prices draws as a Kagi chart.</summary>
/// <param name="Segments">The vertical pieces, left to right.</param>
/// <param name="Connectors">The horizontal connectors, left to right; one fewer than there are legs.</param>
internal readonly record struct KagiLines(IReadOnlyList<KagiSegment> Segments, IReadOnlyList<KagiConnector> Connectors);

/// <summary>The one owner of how a series of closing prices becomes the lines of a Kagi chart (close only; time is left out,
/// so a leg's place along the chart is its column). A leg is one vertical line from its start to its extreme. The first leg
/// starts at the first close and goes the way the first close that is a reversal amount away from it goes, earlier closes
/// being ignored. An up leg grows while a close is above its extreme; a close a reversal amount or more below the extreme
/// turns it: that extreme is the shoulder, a connector at its price joins the next column, and a down leg starts at the shoulder
/// with the turning close as its extreme. A down leg is the mirror, turning at the waist. A close that is neither beyond the
/// extreme nor a reversal amount back is ignored (<see cref="PriceStepExtensions"/>, tolerant of a decimal amount).
/// <para>The first line is thin. A thin leg that goes strictly above the last shoulder turns thick exactly at that price, in
/// the middle of its line; a thick leg that goes strictly below the last waist turns thin exactly at that price. A leg starts in
/// the width the one before it ended in, so the connector between them has one width, and a leg changes width at most once.
/// The starting price counts as the first shoulder when the chart opens downwards and as the first waist when it opens
/// upwards.</para></summary>
internal static class KagiExtensions
{
    extension(double[] prices)
    {
        /// <summary>The lines the prices draw.</summary>
        /// <param name="reversal">The price a close has to come back from the extreme of a leg to turn it; a finite number above
        /// zero (the verb refuses any other).</param>
        /// <returns>The vertical pieces and the connectors; none when no close is a reversal amount from the first one. A price
        /// that is not a finite number is skipped.</returns>
        internal KagiLines KagiLines(double reversal)
        {
            var segments = new List<KagiSegment>();
            var connectors = new List<KagiConnector>();
            bool started = false, open = false, up = false, thick = false;
            double start = 0, begin = 0, extreme = 0;
            double? shoulder = null, waist = null;
            int column = 0;
            foreach (double close in prices)
            {
                if (!double.IsFinite(close))
                {
                    continue;
                }

                if (!started)
                {
                    start = close;
                    started = true;
                    continue;
                }

                if (!open)
                {
                    // The first leg goes the way the first close a reversal amount away from the start goes.
                    if (Math.Abs(close - start).AtLeast(reversal))
                    {
                        open = true;
                        up = close > start;
                        begin = start;
                        extreme = close;
                        if (up) waist = start; else shoulder = start;
                    }

                    continue;
                }

                bool beyond = up ? close > extreme : close < extreme;
                if (beyond)
                {
                    extreme = close;
                }
                else if ((up ? extreme - close : close - extreme).AtLeast(reversal))
                {
                    // The leg turns at its extreme: a connector there, and the next leg starts at it in the width this one ended in.
                    thick = segments.AddLeg(column, begin, extreme, up, thick, shoulder, waist);
                    connectors.Add(new KagiConnector(column, extreme, thick));
                    if (up) shoulder = extreme; else waist = extreme;
                    up = !up;
                    begin = extreme;
                    extreme = close;
                    column++;
                }
            }

            if (open)
            {
                segments.AddLeg(column, begin, extreme, up, thick, shoulder, waist);
            }

            return new KagiLines(segments, connectors);
        }
    }

    extension(List<KagiSegment> segments)
    {
        /// <summary>Adds the pieces of one leg and says which width it ended in. A thin leg that goes strictly above the last
        /// shoulder is thick from that price on, a thick leg that goes strictly below the last waist is thin from that price on;
        /// one that starts beyond that price is that width all along. A leg with nothing to break keeps its width.</summary>
        /// <param name="column">The leg's column.</param>
        /// <param name="begin">The price the leg starts at.</param>
        /// <param name="extreme">The price the leg reached.</param>
        /// <param name="up">Whether the leg goes up.</param>
        /// <param name="thick">Whether the leg starts thick.</param>
        /// <param name="shoulder">The last shoulder, or <see langword="null"/> when there is none.</param>
        /// <param name="waist">The last waist, or <see langword="null"/> when there is none.</param>
        /// <returns>Whether the leg ended thick.</returns>
        private bool AddLeg(int column, double begin, double extreme, bool up, bool thick, double? shoulder, double? waist)
        {
            double? breakAt = up ? (thick ? null : shoulder) : (thick ? waist : null);
            bool breaks = breakAt is { } price && (up ? extreme > price : extreme < price);
            if (!breaks)
            {
                segments.Add(new KagiSegment(column, begin, extreme, thick));
                return thick;
            }

            double at = breakAt!.Value;
            bool startsBeyond = up ? begin >= at : begin <= at;
            if (startsBeyond)
            {
                segments.Add(new KagiSegment(column, begin, extreme, !thick));
            }
            else
            {
                segments.Add(new KagiSegment(column, begin, at, thick));
                segments.Add(new KagiSegment(column, at, extreme, !thick));
            }

            return !thick;
        }
    }
}
