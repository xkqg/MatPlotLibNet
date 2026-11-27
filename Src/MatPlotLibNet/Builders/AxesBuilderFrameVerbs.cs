// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>The frame of the plot: its shape (the aspect), its spines and its grid.</summary>
public static class AxesBuilderFrameVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets how much longer one unit of Y is than one unit of X on the page (<see cref="Axes.Aspect"/>): 1 makes a
        /// square of data a square on the page. The plot is drawn in the largest rectangle of that shape that fits its cell,
        /// centred in it.</summary>
        /// <param name="aspect">The ratio, positive and finite; <see langword="null"/> leaves the aspect to the series again (an image under
        /// the matplotlib themes and a map ask one; <see cref="SetAspectAuto"/> lets the plot fill its cell whatever they ask).</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="aspect"/> is zero, negative, infinite or not a number.</exception>
        public AxesBuilder SetAspect(double? aspect)
        {
            axes.Axes.Aspect = aspect?.ThrowIfNotFinite(nameof(aspect)).ThrowIfNotAbove(0, nameof(aspect));
            return axes;
        }

        /// <summary>Lets the plot fill its cell whatever aspect its series ask, as matplotlib's <c>set_aspect('auto')</c> (and
        /// <c>imshow(aspect='auto')</c>) does: an image under the matplotlib themes, whose cells are square by default, is stretched over the
        /// plot. The axes holds it as an aspect of 0 (<see cref="Axes.Aspect"/>), which the JSON carries.</summary>
        /// <returns>This builder.</returns>
        /// <example>
        /// <code>
        /// var data = new double[170, 220];
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax.Image(data).SetAspectAuto())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder SetAspectAuto() { axes.Axes.Aspect = 0; return axes; }

        /// <summary>Configures the spine (border line) display for this axes.</summary>
        public AxesBuilder WithSpines(Func<SpinesConfig, SpinesConfig> configure) { axes.Axes.Spines = configure(axes.Axes.Spines); return axes; }

        /// <summary>Hides the top spine.</summary>
        public AxesBuilder HideTopSpine() { axes.Axes.Spines = axes.Axes.Spines.Hiding(SpineEdge.Top); return axes; }

        /// <summary>Hides the right spine.</summary>
        public AxesBuilder HideRightSpine() { axes.Axes.Spines = axes.Axes.Spines.Hiding(SpineEdge.Right); return axes; }

        /// <summary>The open frame every strip and facet panel wears: the top and the right spine hidden, the left and the bottom
        /// kept, so the panel spends its space on the data.</summary>
        /// <returns>This builder.</returns>
        internal AxesBuilder OpenFrame() => axes.HideTopSpine().HideRightSpine();

        /// <summary>Hides every spine, tick and tick label on both the X and Y axis — turns the plot
        /// area into a bare canvas. Useful for non-coordinate charts like Sankey, Treemap, or Sunburst
        /// where the default cartesian axes decoration has no meaning and only adds visual clutter.</summary>
        public AxesBuilder HideAllAxes()
        {
            axes.Axes.Spines = axes.Axes.Spines.Hiding(SpineEdge.Top, SpineEdge.Bottom, SpineEdge.Left, SpineEdge.Right);
            axes.X.Ticks.Hide();
            axes.Y.Ticks.Hide();
            return axes;
        }

        /// <summary>Shows or hides the grid lines of the axes. It is the same change as <c>WithGrid(g =&gt; g with { Visible = visible })</c>:
        /// <c>true</c> draws the grid of the theme the figure is drawn under, <c>false</c> draws none even under a theme that
        /// draws one.</summary>
        public AxesBuilder ShowGrid(bool visible = true) => axes.WithGrid(g => g with { Visible = visible });

        /// <summary>Configures the grid style using a transform function, preserving all other properties.
        /// <para>What a caller means by this on a themed chart is "the theme's grid, but ...", so the properties the function sets
        /// are kept as a change over the grid of whatever theme the figure is drawn under, a second call adds to the first, and
        /// the change survives the JSON. The function should name values (<c>g with { Alpha = 0.4 }</c>); one that computes a
        /// property from the grid it is handed is kept as what it makes of a plain grid. It is run three times.</para></summary>
        public AxesBuilder WithGrid(Func<GridStyle, GridStyle> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            axes.Axes.Grid = configure(axes.Axes.Grid);
            // The CHANGE is kept as well as its result, because the result alone cost the author the theme (the grid arrives with
            // every unmentioned property back at its bare default) and could not tell "no grid, please" from "nothing said".
            axes.Axes.GridChange = (axes.Axes.GridChange ?? default).Then(GridChange.Of(configure));
            return axes;
        }
    }
}
