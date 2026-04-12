// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Builders;

/// <summary>
/// Fluent builder for string-pattern subplot layouts.
/// Use <see cref="Panel"/> to configure each labeled panel, then call <see cref="Build"/> to
/// get the underlying <see cref="FigureBuilder"/>.
/// </summary>
/// <example>
/// <code>
/// Plt.Mosaic("AAB\nCCB", m =>
/// {
///     m.Panel("A", ax => ax.Plot(x, y).WithTitle("Top Left"));
///     m.Panel("B", ax => ax.Bar(labels, vals).WithTitle("Right"));
///     m.Panel("C", ax => ax.Scatter(x, y).WithTitle("Bottom Left"));
/// }).Save("mosaic.svg");
/// </code>
/// </example>
public sealed class MosaicFigureBuilder
{
    private readonly string _pattern;
    private readonly Dictionary<string, Action<AxesBuilder>> _panels = new();
    private readonly FigureBuilder _figure;

    /// <summary>Creates a new mosaic builder for the given pattern string.</summary>
    /// <param name="pattern">Mosaic string (e.g. <c>"AAB\nCCB"</c>).</param>
    public MosaicFigureBuilder(string pattern)
    {
        _pattern = pattern;
        _figure  = new FigureBuilder();
    }

    /// <summary>Configures the panel identified by <paramref name="label"/>.</summary>
    /// <param name="label">Single-character label string matching a character in the mosaic pattern.</param>
    /// <param name="configure">Action to configure the axes for this panel.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="label"/> is not in the pattern.</exception>
    public MosaicFigureBuilder Panel(string label, Action<AxesBuilder> configure)
    {
        _panels[label] = configure;
        return this;
    }

    /// <summary>Allows configuring figure-level properties (size, title, theme, etc.) directly on the mosaic builder.</summary>
    public MosaicFigureBuilder Configure(Action<FigureBuilder> configure)
    {
        configure(_figure);
        return this;
    }

    /// <summary>Builds the underlying <see cref="FigureBuilder"/>, wiring up all panels.</summary>
    /// <returns>A <see cref="FigureBuilder"/> with all subplot positions pre-configured.</returns>
    /// <exception cref="ArgumentException">Thrown when the mosaic pattern is invalid.</exception>
    public FigureBuilder Build()
    {
        var positions = SubplotMosaicParser.Parse(_pattern);
        var (rows, cols) = SubplotMosaicParser.GetDimensions(_pattern);

        _figure.WithGridSpec(rows, cols);

        foreach (var (label, position) in positions)
        {
            var configure = _panels.TryGetValue(label, out var action) ? action : _ => { };
            _figure.AddSubPlot(position, configure);
        }

        return _figure;
    }

    /// <summary>Builds the figure and renders it to an SVG string.</summary>
    public string ToSvg() => Build().ToSvg();

    /// <summary>Builds the figure and saves it to <paramref name="path"/>.</summary>
    public void Save(string path) => Build().Save(path);
}
