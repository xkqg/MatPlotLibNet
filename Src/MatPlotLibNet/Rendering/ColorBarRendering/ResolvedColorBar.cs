// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>A colour bar as it is drawn under a theme over the series it reads: the series that backs it, the map of its gradient, the
/// range it covers, the normaliser its ticks are placed by, the steps of its gradient and the lines a contour of lines draws in place of
/// the fill. Made once by <see cref="ResolvedColorBarExtensions.Resolve"/>; the axes' bar, the figure's bar and the room the layout keeps
/// for either read it, so they cannot fetch any of it differently.</summary>
/// <param name="Provider">The series that backs the bar (the first that provides one and has values), or <see langword="null"/>.</param>
/// <param name="Map">The map of the gradient: the bar's own, else its provider's, else the theme's.</param>
/// <param name="Range">The range the bar covers (<see cref="Rendering.ColorBarRendering.ResolvedColorBarExtensions.RangeOf"/>).</param>
/// <param name="Normalizer">The normaliser a tick is placed by (<c>ColorBarNormalizerUnder</c>).</param>
/// <param name="Steps">The steps the gradient is drawn in (<see cref="ColorBarStrip.StepsFor"/>).</param>
/// <param name="Lines">The level lines drawn in place of the fill, or <see langword="null"/> for a filled bar.</param>
internal readonly record struct ResolvedColorBar(IColorBarDataProvider? Provider, IColorMap Map, MinMaxRange Range, INormalizer Normalizer,
    int Steps, ColorBarLines? Lines);

/// <summary>How a colour bar is resolved over the series it reads.</summary>
internal static class ResolvedColorBarExtensions
{
    extension(ColorBar bar)
    {
        /// <summary>This bar as it is drawn under <paramref name="theme"/> over <paramref name="series"/>: the series that backs it is the
        /// first that is a colour bar's data provider and has values (<see cref="ChartSeries.ProvidesColorBar"/>); with none, the bar takes
        /// its own limits and the theme's map.</summary>
        /// <param name="series">The series the bar reads: its axes', or every subplot's for the figure's bar.</param>
        /// <param name="theme">The theme of the figure.</param>
        /// <returns>The resolved bar.</returns>
        internal ResolvedColorBar Resolve(IEnumerable<ISeries> series, Theme theme)
        {
            var provider = series.OfType<IColorBarDataProvider>().FirstOrDefault(p => p is not ChartSeries { ProvidesColorBar: false });
            var map = theme.Looks.ColorMap.Of(bar.ColorMap ?? provider?.ColorMap);
            return new(provider, map, bar.RangeOf(provider, theme), provider.ColorBarNormalizerUnder(theme), ColorBarStrip.StepsFor(map, theme),
                provider.ColorBarLinesUnder(theme));
        }
    }

    extension(ColorBar cb)
    {
        /// <summary>The range the bar covers: that of the series that provides one, when it has a width, otherwise the bar's
        /// own <see cref="ColorBar.Min"/> and <see cref="ColorBar.Max"/>, a range of no width opened as every colour range is
        /// (<see cref="ColorMapping.Opened"/>). The axes' bar, the figure's bar and the layout that reserves
        /// room for either read it, so they cannot disagree.</summary>
        /// <param name="provider">The first series of the chart that provides a range, or <see langword="null"/>.</param>
        /// <param name="theme">The theme of the figure, which a provider's range can depend on (<see cref="IThemedColorBar"/>).</param>
        internal MinMaxRange RangeOf(IColorBarDataProvider? provider, Theme theme)
        {
            double min = cb.Min, max = cb.Max;
            if (provider is not null)
            {
                var (dMin, dMax) = provider.ColorBarRangeUnder(theme);
                if (dMin < dMax) { min = dMin; max = dMax; }
            }
            return ColorMapping.Opened(min, max);
        }
    }

    extension(IColorBarDataProvider provider)
    {
        /// <summary>The range a colour bar backed by this series covers under <paramref name="theme"/>, and the range the series is
        /// coloured over: the provider's own rule for a theme (<see cref="IThemedColorBar"/>, a contour's levels), else the range of its
        /// colour scale (<see cref="IColorScaled"/>, <c>ColourRangeUnder</c>: the values above 0 only under matplotlib's log norm, a range
        /// of no width opened one wide), else its <see cref="IColorBarDataProvider.GetColorBarRange"/>.</summary>
        /// <param name="theme">The theme of the figure.</param>
        internal MinMaxRange ColorBarRangeUnder(Theme theme) => provider switch
        {
            IThemedColorBar themed => themed.ColorBarRangeUnder(theme),
            IColorScaled scaled => scaled.ColourRangeUnder(theme),
            _ => provider.GetColorBarRange(),
        };
    }

    extension(IColorBarDataProvider? provider)
    {
        /// <summary>The normaliser a colour bar backed by this series is drawn with under <paramref name="theme"/>: the one the
        /// provider names for the theme (<see cref="IThemedColorBar"/>, a filled contour's bands), else the one the series colours its
        /// values with, otherwise the linear one, which is also the bar's when no series backs it. A tick stands where this
        /// normaliser sends its value, so the bar and the series cannot read the colour of a value differently. Under the themes that
        /// draw a bar as matplotlib's <c>Colorbar</c> (<see cref="ColorBarLook.StepPerColour"/>) a <see cref="BoundaryNormalizer"/>'s
        /// bar names at most ten boundaries, as a contour's does.</summary>
        /// <param name="theme">The theme of the figure.</param>
        internal INormalizer ColorBarNormalizerUnder(Theme theme)
        {
            if ((provider as IThemedColorBar)?.ColorBarNormalizerUnder(theme) is { } themed) return themed;

            var normalizer = provider is INormalizable normalizable ? normalizable.NormalizerUnder(theme) : LinearNormalizer.Instance;
            return normalizer.Rule.ForColorBarUnder(theme.Looks.ColorBar) ?? normalizer;
        }

        /// <summary>The lines a colour bar backed by this series draws in place of a fill under <paramref name="theme"/>: those the
        /// provider names (<see cref="IThemedColorBar"/>, a contour of lines); <see langword="null"/> for a bar that is filled, which is
        /// every other bar's, and the bar's when no series backs it.</summary>
        /// <param name="theme">The theme of the figure.</param>
        internal ColorBarLines? ColorBarLinesUnder(Theme theme) => (provider as IThemedColorBar)?.ColorBarLinesUnder(theme);
    }
}
