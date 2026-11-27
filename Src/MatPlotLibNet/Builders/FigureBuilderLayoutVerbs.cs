// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>Where the axes of the figure stand: the grid, the spacing and the layout engines that fit the subplots, and the insets
/// placed in a subplot.</summary>
public static class FigureBuilderLayoutVerbs
{
    extension(FigureBuilder figure)
    {
        /// <summary>Enables tight layout, which computes minimal margins automatically.</summary>
        public FigureBuilder TightLayout() { figure.Spacing = figure.SpacingSoFar with { TightLayout = true }; return figure; }

        /// <summary>Enables tight layout and fits the subplots, their labels and the figure's title into <paramref name="rect"/>, as
        /// matplotlib's <c>tight_layout(rect=(left, bottom, right, top))</c> does: the part of the figure outside the rectangle is kept
        /// free on top of what the layout needs, for a title or a note placed there.</summary>
        /// <param name="rect">The rectangle, in fractions of the figure counted from its bottom-left corner.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <example>
        /// Keep the top 7 % of the figure free above the subplots and their title:
        /// <code>
        /// string svg = Plt.Create()
        ///     .WithTitle("Decay and noise")
        ///     .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [3.0, 1.5, 0.7]))
        ///     .TightLayout(new LayoutRect(0, 0, 1, 0.93))
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder TightLayout(LayoutRect rect)
        {
            figure.Spacing = figure.SpacingSoFar with { TightLayout = true, LayoutRect = rect };
            return figure;
        }

        /// <summary>Enables constrained layout, which adjusts margins to fit tick labels and axis labels.</summary>
        public FigureBuilder ConstrainedLayout() { figure.Spacing = figure.SpacingSoFar with { ConstrainedLayout = true }; return figure; }

        /// <summary>Configures custom subplot spacing (margins and gaps).</summary>
        public FigureBuilder WithSubPlotSpacing(Func<SubPlotSpacing, SubPlotSpacing>? configure = null)
        {
            if (configure is not null) figure.Spacing = configure(figure.SpacingSoFar);
            return figure;
        }

        /// <summary>Sets the grid specification for advanced subplot layouts with ratios and spanning.</summary>
        /// <param name="rows">Number of rows in the grid.</param>
        /// <param name="cols">Number of columns in the grid.</param>
        /// <param name="heightRatios">Relative row heights (length must equal <paramref name="rows"/>); <see langword="null"/> gives equal heights.</param>
        /// <param name="widthRatios">Relative column widths (length must equal <paramref name="cols"/>); <see langword="null"/> gives equal widths.</param>
        public FigureBuilder WithGridSpec(int rows, int cols, double[]? heightRatios = null, double[]? widthRatios = null)
        {
            var gridSpec = new GridSpec { Rows = rows, Cols = cols, HeightRatios = heightRatios, WidthRatios = widthRatios };
            return figure.OnFigure(f => f.GridSpec = gridSpec);
        }

        /// <summary>Adds an inset axes to the subplot at the given zero-based index. This is the
        /// canonical form; the loose-doubles
        /// <see cref="extension(FigureBuilder).AddInset(int,double,double,double,double,System.Action{Axes})"/> overload forwards here.</summary>
        /// <param name="subplotIndex">Zero-based index of the parent subplot.</param>
        /// <param name="bounds">The fractional position and size of the inset within the parent subplot (0–1).</param>
        /// <param name="configure">Optional action to configure the inset axes.</param>
        public FigureBuilder AddInset(int subplotIndex, InsetBounds bounds, Action<Axes>? configure = null) => figure.OnFigure(f =>
        {
            // An index with no subplot behind it places nothing.
            if ((uint)subplotIndex >= (uint)f.SubPlots.Count) return;
            var inset = f.SubPlots[subplotIndex].AddInset(bounds);
            configure?.Invoke(inset);
        });

        /// <summary>Adds an inset axes to the subplot at the given zero-based index.
        /// The bounds are fractional positions within the parent subplot (0–1). Convenience overload
        /// that forwards to <see cref="extension(FigureBuilder).AddInset(int,InsetBounds,System.Action{Axes})"/>.</summary>
        /// <param name="subplotIndex">Zero-based index of the parent subplot.</param>
        /// <param name="x">Horizontal position as a fraction of parent width.</param>
        /// <param name="y">Vertical position as a fraction of parent height.</param>
        /// <param name="width">Width as a fraction of parent width.</param>
        /// <param name="height">Height as a fraction of parent height.</param>
        /// <param name="configure">Optional action to configure the inset axes.</param>
        public FigureBuilder AddInset(int subplotIndex, double x, double y, double width, double height,
            Action<Axes>? configure = null)
            => figure.AddInset(subplotIndex, new InsetBounds(x, y, width, height), configure);

        /// <summary>The spacing the layout calls before this one left, a new one when none did: each layout verb edits it, so it
        /// keeps what the others asked for.</summary>
        private SubPlotSpacing SpacingSoFar => figure.Spacing ?? new();
    }
}
