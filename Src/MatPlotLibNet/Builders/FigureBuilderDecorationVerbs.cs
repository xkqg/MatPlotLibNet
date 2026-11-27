// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>What the figure draws outside its subplots: its title, texts, legends, images and colour bar.</summary>
public static class FigureBuilderDecorationVerbs
{
    extension(FigureBuilder figure)
    {
        /// <summary>Sets the figure title.</summary>
        public FigureBuilder WithTitle(string title) => figure.OnFigure(f => f.Title = title);

        /// <summary>Sets the figure title and its style (matplotlib's <c>fig.suptitle(title, fontsize=..., fontweight=...)</c>): the
        /// layout measures and reserves the title in that style, and draws it so.</summary>
        /// <param name="title">The title text.</param>
        /// <param name="configure">Function that receives a new <see cref="TextStyle"/> and returns the one the title takes (its
        /// size in pixels, weight, family, slant, colour and the extra room kept below it); <see langword="null"/> leaves the style
        /// as it is.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <example>
        /// A title at matplotlib's 14 points, bold, over two charts laid out by the tight layout:
        /// <code>
        /// string svg = Plt.Create()
        ///     .WithTitle("Decay and noise", style => style with { FontSize = 14 * 100.0 / 72, FontWeight = FontWeight.Bold })
        ///     .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0, 3.0], [3.0, 1.5, 0.7]))
        ///     .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0, 3.0], [0.2, -0.1, 0.3]))
        ///     .TightLayout()
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder WithTitle(string title, Func<TextStyle, TextStyle>? configure)
        {
            if (configure is null) return figure.WithTitle(title);
            var style = configure(new TextStyle());
            return figure.OnFigure(f => { f.Title = title; f.TitleStyle = style; });
        }

        /// <summary>Adds a colour bar for the whole figure, drawn outside all subplots. Under the matplotlib themes the subplots
        /// shrink together to make room for it, as matplotlib's <c>fig.colorbar(mappable, ax=axs)</c> makes them.</summary>
        /// <param name="configure">Optional function to customise the color bar (label, orientation, shrink, etc.).</param>
        public FigureBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
        {
            figure.FigureColorBar = ColorBar.AskedFor(figure.FigureColorBar, configure);
            return figure;
        }

        /// <summary>Places a text on the figure itself rather than in a subplot (matplotlib's <c>figtext</c>): a note, a source
        /// line, a panel letter. The position is a pair of fractions of the figure counted from its bottom-left corner, so
        /// (0.5, 0.97) is centred just under the top edge whatever the subplots show. The text is drawn in the theme's text font
        /// above every subplot and the figure's colour bar, in the SVG and in every raster or native output alike, and it is
        /// drawn on a figure that has no subplot at all. A position outside 0 to 1, or one that is not a number, draws nothing.</summary>
        /// <param name="text">The text to draw.</param>
        /// <param name="x">The horizontal position as a fraction of the figure's width, from the left.</param>
        /// <param name="y">The vertical position as a fraction of the figure's height, from the bottom.</param>
        /// <param name="configure">Optional action that sets the text's font, colour, alignment or rotation.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
        /// <example>
        /// A source line in the bottom-left corner and a centred note across the top:
        /// <code>
        /// string svg = Plt.Create()
        ///     .Plot([1.0, 2.0, 3.0], [2.0, 4.0, 3.0])
        ///     .FigText("Source: field survey", 0.01, 0.01)
        ///     .FigText("Preliminary", 0.5, 0.97, t =>
        ///     {
        ///         t.Alignment = TextAlignment.Center;
        ///         t.VerticalAlignment = TextVerticalAlignment.Top;
        ///         t.Color = Colors.Red;
        ///     })
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder FigText(string text, double x, double y, Action<FigureText>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            return figure.OnFigure(f =>
            {
                var figureText = f.AddText(text, x, y);
                configure?.Invoke(figureText);
            });
        }

        /// <summary>Adds a legend for the figure as a whole (matplotlib's <c>fig.legend()</c>) that lists the labelled series of
        /// every subplot, in subplot order. It stands in the rectangle that is the figure, at the place its
        /// <see cref="Legend.Position"/> names, or the theme's for one that names none (a place that is still <see cref="LegendPosition.Best"/> is the upper right
        /// here, as a figure has no marks to keep clear of), above the subplots, the colour bar and the figure's texts, in the SVG and in every raster or native output
        /// alike. An outside place (<see cref="LegendPosition.OutsideRight"/> and the others) stands it at the middle of that edge of
        /// the figure, and <see cref="FigureBuilderLayoutVerbs.extension(FigureBuilder).ConstrainedLayout"/> narrows the subplots to keep room for it, as matplotlib's constrained
        /// layout does; any other place takes no room. It is static, as in matplotlib: clicking a row hides nothing. Each call adds
        /// another legend.</summary>
        /// <param name="configure">Optional function that changes the legend (its position, title, columns, fonts, frame and
        /// whether the rows carry their last value), given a new default <see cref="Legend"/>.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <example>
        /// A grid of two charts that share one legend, outside on the right, with room kept for it:
        /// <code>
        /// string svg = Plt.Create()
        ///     .ConstrainedLayout()
        ///     .AddSubPlot(1, 2, 1, ax => ax.WithLegend(visible: false)
        ///         .Plot([1.0, 2.0, 3.0], [2.0, 4.0, 3.0], s => s.Label = "Observed"))
        ///     .AddSubPlot(1, 2, 2, ax => ax.WithLegend(visible: false)
        ///         .Plot([1.0, 2.0, 3.0], [3.0, 1.0, 2.0], s => s.Label = "Fitted"))
        ///     .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight })
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder FigLegend(Func<Legend, Legend>? configure = null)
        {
            var legend = new Legend();
            if (configure is not null) legend = configure(legend);
            return figure.OnFigure(f => f.AddLegend(legend));
        }

        /// <summary>Places an image on the figure itself rather than in a subplot (matplotlib's <c>figimage</c>): a logo, a
        /// watermark, a texture behind the charts. Each value of <paramref name="data"/> is one pixel of the figure, drawn at its
        /// native size with no resampling and coloured through a colour map as an image series is (viridis unless the
        /// configuration names another), the first row on top. The offsets are pixels (the units of the figure's size) from the
        /// bottom-left corner of the figure. The image stands under every subplot, in the SVG and in every raster or native output
        /// alike, and it is drawn on a figure that has no subplot at all. A NaN value is a gap, an offset that is not a number draws
        /// nothing, and an image that reaches past the figure is cut at its edge.</summary>
        /// <param name="data">The pixel values, indexed [row, column].</param>
        /// <param name="xOffset">The distance from the left edge of the figure to the left edge of the image, in pixels.</param>
        /// <param name="yOffset">The distance from the bottom edge of the figure to the bottom edge of the image, in pixels.</param>
        /// <param name="configure">Optional action that sets the image's colour map, normalizer, colour range or opacity.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        /// <example>
        /// A small gradient in the lower-left corner, under a chart:
        /// <code>
        /// var gradient = new double[8, 32];
        /// for (int row = 0; row &lt; 8; row++)
        ///     for (int column = 0; column &lt; 32; column++)
        ///         gradient[row, column] = column;
        ///
        /// string svg = Plt.Create()
        ///     .Plot([1.0, 2.0, 3.0], [2.0, 4.0, 3.0])
        ///     .FigImage(gradient, 20, 20, image => image.Alpha = 0.6)
        ///     .ToSvg();
        /// </code>
        /// </example>
        public FigureBuilder FigImage(double[,] data, double xOffset = 0, double yOffset = 0, Action<FigureImage>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(data);
            return figure.OnFigure(f =>
            {
                var image = f.AddImage(data, xOffset, yOffset);
                configure?.Invoke(image);
            });
        }
    }
}
