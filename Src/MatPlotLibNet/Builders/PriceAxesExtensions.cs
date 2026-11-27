// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>The price verbs of <see cref="AxesBuilder"/>: a series of prices in time order, drawn without its time axis. Each
/// verb takes the prices as one array and builds its chart from series the library already ships, so it adds no series type, no
/// registry key and no serializer path: the chart travels as its parts, and read back it draws the same picture. (The
/// technical indicators take their prices as an <see cref="Indicators.OhlcvBars"/>; these charts need nothing but the closes.)</summary>
public static class PriceAxesExtensions
{
    /// <summary>The width of the thin line of a Kagi chart, in pixels.</summary>
    private const double KagiThinWidth = 1;

    /// <summary>The width of the thick line of a Kagi chart, in pixels.</summary>
    private const double KagiThickWidth = 3;

    extension(AxesBuilder axes)
    {
        /// <summary>Draws a Renko chart: the price as a staircase of bricks of one fixed size, up where it rose and down where it
        /// fell, with no axis for time. Each brick is one column wide, the bricks stand one after the other, and the next brick
        /// begins at the corner of the last one.
        /// <para>Close only. The chart is anchored on the first close, unrounded: every brick edge is that close plus a whole
        /// number of brick sizes. A close that reaches the next brick in the direction of the last one (a brick beyond its far
        /// edge) draws it, and one that reaches the brick the other way (a brick beyond its near edge, so two bricks from the far
        /// edge) reverses it, so a reversing brick never overlaps the one before it. A close on the trigger counts, a close that
        /// covers several bricks draws each of them, and a close that reaches neither draws none. The brick size is a price you
        /// choose; a rule such as a multiple of the average true range is one line of your own with the library's
        /// <c>Atr</c>.</para>
        /// <para>The X axis carries no ticks, since it is only the order of the bricks. The chart is its parts: written to JSON it is
        /// two floating bar series, and read back it draws the same picture. It is not laid out again; change the prices and call
        /// the verb again.</para></summary>
        /// <param name="prices">The closing prices, oldest first; a price that is not a finite number is skipped.</param>
        /// <param name="brickSize">The price one brick covers; a finite number above zero.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the colours or the labels of the two kinds of brick.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prices"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="brickSize"/> is not a finite number above zero.</exception>
        /// <example>
        /// <code>
        /// double[] closes = [10, 10.4, 11.0, 13.2, 12.5, 11.0, 10.4, 7.8, 8.9, 10.0, 9.0, 15.9];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Renko, bricks of 1")
        ///     .WithSize(800, 450)
        ///     .AddSubPlot(1, 1, 1, ax => ax.Renko(closes, 1))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Renko(double[] prices, double brickSize, Action<RenkoParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(prices);
            brickSize.ThrowIfNotFinite(nameof(brickSize)).ThrowIfNotAbove(0, nameof(brickSize));

            // Brick i of the whole chart stands in the slot from i to i + 1 (the bars are a slot wide, so neighbours touch), on the
            // bottom of its brick, as tall as the brick; each direction is one series, so each is one colour and one legend entry.
            var bricks = prices.RenkoBricks(brickSize);
            var risingBricks = new FloatingBars(CategorySlot.Of(0).Width, UpDownColors.Standard.Up);
            var fallingBricks = new FloatingBars(CategorySlot.Of(0).Width, UpDownColors.Standard.Down);
            for (int i = 0; i < bricks.Count; i++)
            {
                var brick = bricks[i];
                (brick.Rising ? risingBricks : fallingBricks).Add(
                    i.InvariantText(), CategorySlot.Of(i).Centre, brick.Bottom, brick.Top - brick.Bottom);
            }

            var up = risingBricks.ToSeries("up");
            var down = fallingBricks.ToSeries("down");
            // The X axis is only the order of the bricks, so it carries no ticks: a number on it would read "2.5" for no brick.
            axes.AddSeries(up).AddSeries(down).SetXTickLocator(new FixedLocator([]));
            configure?.Invoke(new RenkoParts(up, down));
            return axes;
        }

        /// <summary>Draws a Kagi chart: the price as one unbroken path of vertical lines and short horizontal connectors, with
        /// no axis for time, whose line is thick where the price has broken above the last shoulder (the top of the last rise)
        /// and thin where it has broken below the last waist (the bottom of the last fall).
        /// <para>Close only. The first leg starts at the first close and goes the way the first close a reversal amount away goes.
        /// An up leg grows while a close is above its extreme; a close a reversal amount or more below it turns the leg: a
        /// connector one column wide joins it at the price it turned at (the shoulder) to a down leg that starts there, and the
        /// down leg is the mirror (it turns at the waist). A close that is neither beyond the extreme nor a reversal amount back
        /// is ignored, and a close exactly a reversal amount back turns the leg. The first line is thin. A thin leg that goes
        /// strictly above the last shoulder turns thick exactly at that price, in the middle of its line, and a thick leg that
        /// goes strictly below the last waist turns thin exactly at that price; a leg starts in the width the one before it ended
        /// in, and the starting price counts as the first shoulder when the chart opens downwards and as the first waist when it
        /// opens upwards. The reversal is a price you choose, not a share of it.</para>
        /// <para>The chart is two lines of one colour of the theme's cycle, the thick and the thin, each a
        /// <see cref="LineSeries"/> whose separate stretches are parted by a missing value, so a stretch with a corner is one
        /// line and joins cleanly. The X axis carries no ticks, since it is only the order of the legs. Written to JSON it is
        /// those two series, and read back it draws the same picture. It is not laid out again; change the prices and call the
        /// verb again.</para></summary>
        /// <param name="prices">The closing prices, oldest first; a price that is not a finite number is skipped.</param>
        /// <param name="reversal">The price a close has to come back from the extreme of a leg to turn it; a finite number above
        /// zero.</param>
        /// <param name="configure">Optional styling of the parts, run once after the verb has built them, so what it sets wins:
        /// the colour or the width of the two lines.</param>
        /// <returns>The same builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prices"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="reversal"/> is not a finite number above zero.</exception>
        /// <example>
        /// <code>
        /// double[] closes = [10, 10.5, 12, 12, 13, 12, 11, 10, 11, 14, 15, 14, 12, 9, 10];
        ///
        /// string svg = Plt.Create()
        ///     .WithTitle("Kagi, a reversal of 1")
        ///     .WithSize(800, 450)
        ///     .AddSubPlot(1, 1, 1, ax => ax.Kagi(closes, 1))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder Kagi(double[] prices, double reversal, Action<KagiParts>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(axes);
            ArgumentNullException.ThrowIfNull(prices);
            reversal.ThrowIfNotFinite(nameof(reversal)).ThrowIfNotAbove(0, nameof(reversal));

            var lines = prices.KagiLines(reversal);
            var thin = LineOf(lines, thick: false);
            var thick = LineOf(lines, thick: true);
            axes.Axes.InColorGroup(thin, thick);
            // The X axis is only the order of the legs, so it carries no ticks.
            axes.AddSeries(thin).AddSeries(thick).SetXTickLocator(new FixedLocator([]));
            configure?.Invoke(new KagiParts(thick, thin));
            return axes;
        }
    }

    /// <summary>The Kagi path in one width as a line of that width.</summary>
    private static LineSeries LineOf(KagiLines lines, bool thick)
    {
        var path = lines.Path(thick);
        return new LineSeries(path.X, path.Y) { LineWidth = thick ? KagiThickWidth : KagiThinWidth };
    }
}
