// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>The axes joined to this one: an axis shared with another subplot, the top X axis, every Y axis stacked beside the
/// plot, every X axis linked below it, and an inset inside it.</summary>
public static class AxesBuilderLinkedAxesVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Shares the X axis with the axes identified by the given key.</summary>
        public AxesBuilder ShareX(string key) { axes.Axes.ShareXKey = key; return axes; }

        /// <summary>Shares the Y axis with the axes identified by the given key.</summary>
        public AxesBuilder ShareY(string key) { axes.Axes.ShareYKey = key; return axes; }

        /// <summary>Configures a secondary X-axis (top edge) and adds series to it.</summary>
        public AxesBuilder WithSecondaryXAxis(Action<SecondaryXAxisBuilder> configure) { configure(new SecondaryXAxisBuilder(axes.Axes)); return axes; }

        /// <summary>Stacks one more Y axis on the right of the plot, past every right axis already there, and configures it.
        /// Call it as often as you want axes: the first call adds the axis nearest the plot, each further call stacks one more
        /// past it, with no cap and nothing to count. This is matplotlib's repeated <c>ax.twinx()</c> with its right spine
        /// moved outward by <c>ax.spines['right'].set_position(('outward', N))</c>, in one verb.</summary>
        /// <param name="configure">Callback that receives the builder of the new axis: label, limits, colour and the series
        /// plotted against it.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
        /// <remarks>The layout makes room for every axis, and each one keeps its own tick labels and title. The first right
        /// axis colours the right spine when it is given a colour; an axis stacked past it draws no spine of its own.</remarks>
        /// <example>
        /// Three units against one time axis:
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(days, price).SetYLabel("Price")
        ///     .WithAddRightYAxis(rate => rate.SetYLabel("Rate (%)").Plot(days, rates))
        ///     .WithAddRightYAxis(load => load.SetYLabel("Load").SetYLim(0, 1).Plot(days, loads)));
        /// </code>
        /// </example>
        public AxesBuilder WithAddRightYAxis(Action<YAxisSlotBuilder> configure) => axes.AddYAxis(axes.Axes.YAxes.Right, configure);

        /// <summary>Stacks one more Y axis on the left of the plot, past the primary Y axis and every left axis already
        /// added, and configures it. The primary Y axis stays the left axis nearest the plot; the first call adds the axis next
        /// to it, each further call stacks one more past that, with no cap and nothing to count. This is matplotlib's repeated
        /// <c>ax.twinx()</c> with its Y axis moved to the left (<c>twin.yaxis.tick_left()</c>) and its left spine moved outward
        /// by <c>twin.spines['left'].set_position(('outward', N))</c>, in one verb.</summary>
        /// <param name="configure">Callback that receives the builder of the new axis: label, limits, colour and the series
        /// plotted against it.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
        /// <remarks>The layout makes room for every axis, and each one keeps its own tick labels and title. An axis stacked
        /// past the primary draws no spine of its own, so colouring it colours its ticks, tick labels and title only.</remarks>
        /// <example>
        /// A queue length next to the primary price axis:
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(days, price).SetYLabel("Price")
        ///     .WithAddLeftYAxis(queue => queue.SetYLabel("Queue").Plot(days, lengths)));
        /// </code>
        /// </example>
        public AxesBuilder WithAddLeftYAxis(Action<YAxisSlotBuilder> configure) => axes.AddYAxis(axes.Axes.YAxes.Left, configure);

        /// <summary>Stacks one more linked X axis below the plot and configures it: matplotlib's
        /// <c>ax.secondary_xaxis('bottom', functions=(forward, inverse))</c>, or at a location below the plot with
        /// <see cref="XAxisSlotBuilder.SetLocation"/>. The axis shows the range the primary X axis shows in other units, through the
        /// link its builder gives it (<see cref="XAxisSlotBuilder.Link"/>), and holds no series of its own. The first call stands it
        /// below the primary X axis' tick labels and title, each further call one more below that.</summary>
        /// <param name="configure">Callback that receives the builder of the new axis: its link, title, colour and location.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
        /// <remarks>The layout makes room for every such axis, and each one keeps its own tick labels and title. An axis given no
        /// link draws nothing.</remarks>
        /// <example>
        /// Fahrenheit below a plot in Celsius:
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(celsius, rate).SetXLabel("°C")
        ///     .WithAddBottomXAxis(fahrenheit => fahrenheit.SetXLabel("°F").Link(AxisTransform.Linear(1.8, 32))));
        /// </code>
        /// </example>
        public AxesBuilder WithAddBottomXAxis(Action<XAxisSlotBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(new XAxisSlotBuilder(axes.Axes, axes.Axes.YAxes.Bottom.Add()));
            return axes;
        }

        /// <summary>Sets the blank gap, in pixels, between the ink of every Y axis title of the subplot and the ink of the
        /// widest tick label beside it: the same for the primary Y axis and every axis added with <see cref="WithAddLeftYAxis"/> or
        /// <see cref="WithAddRightYAxis"/>, on both sides, and between two stacked axes. The default is
        /// <see cref="YAxisContainer.RestingTitleGap"/>, matplotlib's <c>axes.labelpad</c>. A title's own pad (the <c>Pad</c> of the text
        /// style given to <c>SetYLabel</c>) moves that one title further out on top of it.</summary>
        /// <param name="pixels">The gap in pixels: a finite number, 0 or more.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pixels"/> is negative, NaN or infinite.</exception>
        /// <example>
        /// Titles a little further from their tick labels on both sides:
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(days, price).SetYLabel("Price")
        ///     .WithAddRightYAxis(rate => rate.SetYLabel("Rate (%)").Plot(days, rates))
        ///     .WithYAxisTitleGap(8));
        /// </code>
        /// </example>
        public AxesBuilder WithYAxisTitleGap(double pixels)
        {
            axes.Axes.YAxes.TitleGap = YAxisContainer.ValidTitleGap(pixels, nameof(pixels));
            return axes;
        }

        /// <summary>Stacks one more axis on <paramref name="stack"/> and hands it to <paramref name="configure"/>: the one
        /// body both container verbs share.</summary>
        /// <param name="stack">The side's stack of the subplot's Y axes.</param>
        /// <param name="configure">The caller's callback; checked before anything is added.</param>
        private AxesBuilder AddYAxis(YAxisStack stack, Action<YAxisSlotBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(new YAxisSlotBuilder(axes.Axes, stack.Add()));
            return axes;
        }

        /// <summary>Adds an inset axes at the specified bounds within this axes and configures it via
        /// the supplied builder. This is the canonical form; the loose-doubles
        /// <see cref="extension(AxesBuilder).AddInset(double,double,double,double,System.Action{AxesBuilder})"/> overload forwards here.</summary>
        /// <param name="bounds">The fractional position and size of the inset within this axes.</param>
        /// <param name="configure">Configures the inset's series and axes. The callback's builder works on the inset itself,
        /// so everything it sets is what the inset draws with: a tick formatter of your own, a grid change over the grid of
        /// whatever theme the figure is drawn under, and an inset of its own. A share key set on it joins no group, because
        /// an inset is not a subplot of the figure.</param>
        public AxesBuilder AddInset(InsetBounds bounds, Action<AxesBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(new AxesBuilder(axes.Axes.AddInset(bounds)));
            return axes;
        }

        /// <summary>Adds an inset axes at the specified fractional position within this axes.
        /// Convenience overload that forwards to <see cref="extension(AxesBuilder).AddInset(InsetBounds,System.Action{AxesBuilder})"/>.</summary>
        public AxesBuilder AddInset(double x, double y, double width, double height, Action<AxesBuilder> configure)
            => axes.AddInset(new InsetBounds(x, y, width, height), configure);

        /// <summary>Has the parent of this inset draw the zoom it shows (matplotlib's <c>indicate_inset_zoom</c>): a rectangle over
        /// the region the inset shows, in the parent's data coordinates, and the two lines from it to the inset's frame that
        /// matplotlib's rule keeps. Called on the inset's builder, inside the <c>AddInset</c> callback; it sets the inset's
        /// <see cref="Axes.ZoomIndicator"/>, replacing one set before. The rectangle spans the limits the inset draws, so it
        /// follows a limit set on the inset, and the margins of one that scales itself to its data.</summary>
        /// <param name="configure">Sets the indicator's colour, opacity, line width, line style and whether the connectors are
        /// drawn; left out, the indicator is matplotlib's: the theme's neutral grey at half opacity, one pixel, solid, with
        /// connectors.</param>
        /// <returns>This builder, so the inset's configuration goes on.</returns>
        /// <exception cref="InvalidOperationException">This builder is not an inset's: a subplot has no parent to draw its zoom on.</exception>
        /// <example>
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(x, y)
        ///     .AddInset(0.55, 0.05, 0.4, 0.4, inset => inset
        ///         .Plot(x, y)
        ///         .SetXLim(2, 3).SetYLim(0.5, 1)
        ///         .IndicateZoom(zoom => zoom.Color = Colors.Red)));
        /// </code>
        /// </example>
        public AxesBuilder IndicateZoom(Action<ZoomIndicator>? configure = null)
        {
            if (axes.Axes.InsetBounds is null)
                throw new InvalidOperationException("IndicateZoom is called on the builder of an inset (inside the AddInset callback): only an inset has a parent to draw its zoom on.");

            var zoom = new ZoomIndicator();
            configure?.Invoke(zoom);
            axes.Axes.ZoomIndicator = zoom;
            return axes;
        }
    }
}
