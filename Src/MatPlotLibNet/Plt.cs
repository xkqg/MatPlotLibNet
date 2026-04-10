// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Top-level entry point for creating figures, modeled after matplotlib's pyplot interface.</summary>
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
