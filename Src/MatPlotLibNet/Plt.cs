// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Top-level entry point for creating figures, modeled after matplotlib's pyplot interface.</summary>
/// <example>
/// Quickstart — line + scatter, then save:
/// <code>
/// double[] x = [1, 2, 3, 4, 5];
/// double[] y = [1.2, 2.4, 1.8, 3.1, 2.7];
/// Plt.Create()
///     .Plot(x, y, s => s.Label = "Signal")
///     .Scatter(x, y, s => s.Label = "Points")
///     .WithTitle("Demo")
///     .Save("demo.svg");
/// </code>
/// Apply a style for the lifetime of a block:
/// <code>
/// using (Plt.Style.Context("dark_background"))
/// {
///     Plt.Create().Plot(x, y).Save("dark.svg");
/// }
/// </code>
/// </example>
public static class Plt
{
    /// <summary>Creates a new <see cref="Models.Figure"/> with the specified dimensions.</summary>
    /// <param name="width">The figure width in pixels.</param>
    /// <param name="height">The figure height in pixels.</param>
    /// <returns>A new figure instance.</returns>
    public static Figure Figure(double width = 800, double height = 600) =>
        new() { Width = width, Height = height };

    /// <summary>Creates a new <see cref="FigureBuilder"/> for fluent figure construction (e.g., <c>Plt.Create().WithTitle("My Chart").Plot(x, y).Build()</c>).</summary>
    /// <returns>A new fluent figure builder.</returns>
    public static FigureBuilder Create() => new();

    /// <summary>Creates a <see cref="MosaicFigureBuilder"/> for string-pattern subplot layouts.</summary>
    /// <param name="pattern">Mosaic string (rows separated by <c>\n</c>, each character = one panel label).
    /// Repeated characters span multiple cells. Example: <c>"AAB\nCCB"</c>.</param>
    /// <param name="configure">Optional action to configure panels on the returned builder.</param>
    /// <returns>A <see cref="MosaicFigureBuilder"/> ready for <see cref="MosaicFigureBuilder.Panel"/> calls.</returns>
    public static MosaicFigureBuilder Mosaic(string pattern, Action<MosaicFigureBuilder>? configure = null)
    {
        var builder = new MosaicFigureBuilder(pattern);
        configure?.Invoke(builder);
        return builder;
    }

    /// <summary>Creates an <see cref="AnimationController{TState}"/> that calls
    /// <paramref name="frameGenerator"/> with the eased progress <c>t ∈ [0, 1]</c> for each frame.</summary>
    /// <param name="frameCount">Total number of frames.</param>
    /// <param name="frameGenerator">Receives eased <c>t</c> and returns the figure for that moment.</param>
    /// <param name="easing">Easing curve (default: <see cref="EasingKind.Linear"/>).</param>
    /// <param name="intervalMs">Delay between frames in milliseconds (default: 16 ≈ 60fps).</param>
    /// <param name="loop">Whether the animation loops (default: false).</param>
    public static AnimationController<double> Animate(
        int frameCount,
        Func<double, Figure> frameGenerator,
        EasingKind easing = EasingKind.Linear,
        int intervalMs = 16,
        bool loop = false) =>
        new(new EasedFigureAnimation(frameCount, frameGenerator, easing, intervalMs, loop),
            static (_, _) => Task.CompletedTask);

    /// <summary>
    /// Entry point for global style configuration, modeled after <c>matplotlib.pyplot.style</c>.
    /// </summary>
    public static class Style
    {
        /// <summary>
        /// Applies <paramref name="sheet"/> globally by mutating <see cref="RcParams.Default"/>.
        /// Affects all subsequent figure renders in the process.
        /// For scoped changes prefer <see cref="Context(StyleSheet)"/>.
        /// </summary>
        public static void Use(StyleSheet sheet)
        {
            foreach (var kv in sheet.Parameters)
                RcParams.Default.Set(kv.Key, kv.Value);
        }

        /// <summary>
        /// Applies the named style sheet globally.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the style name is not registered.</exception>
        public static void Use(string name)
        {
            var sheet = StyleSheetRegistry.Get(name)
                ?? throw new ArgumentException($"Style sheet '{name}' is not registered.", nameof(name));
            Use(sheet);
        }

        /// <summary>
        /// Returns a scoped <see cref="StyleContext"/> that overrides <see cref="RcParams.Current"/>
        /// for the duration of the <see langword="using"/> block.
        /// </summary>
        public static StyleContext Context(StyleSheet sheet) => new(sheet);

        /// <summary>
        /// Returns a scoped <see cref="StyleContext"/> for the named style sheet.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the style name is not registered.</exception>
        public static StyleContext Context(string name)
        {
            var sheet = StyleSheetRegistry.Get(name)
                ?? throw new ArgumentException($"Style sheet '{name}' is not registered.", nameof(name));
            return new StyleContext(sheet);
        }
    }
}
