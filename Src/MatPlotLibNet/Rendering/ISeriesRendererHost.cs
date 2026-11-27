// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering;

/// <summary>The series pass a series of a package is drawn through: the public <see cref="ISeriesVisitor"/> has a <c>Visit</c> for each
/// series of the library and none for a package's (a map of the Geo package), so such a series hands itself here with the renderer it is
/// drawn by, and the pass makes that renderer over its context and draws with it as it draws every renderer of its own, collecting marks
/// included.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="ISeriesVisitor"/>: an internal member of a public
/// interface cannot be implemented outside this assembly, so a proxy or a mock of the visitor would fail to load.</remarks>
internal interface ISeriesRendererHost
{
    /// <summary>Draws <paramref name="series"/> through the renderer <paramref name="renderer"/> makes over this pass' context.</summary>
    /// <typeparam name="TSeries">The kind of series.</typeparam>
    /// <param name="series">The series.</param>
    /// <param name="renderer">Makes the series' renderer over the context of the pass.</param>
    void Draw<TSeries>(TSeries series, Func<SeriesRenderContext, SeriesRenderer<TSeries>> renderer) where TSeries : ISeries;
}

/// <summary>How a series of a package hands itself to the visitor it is given.</summary>
internal static class SeriesRendererHostExtensions
{
    extension(ISeriesVisitor visitor)
    {
        /// <summary>Hands <paramref name="series"/> to the library's series pass with the renderer it is drawn by; a visitor that is not
        /// the library's is handed nothing, as it is handed no series it has no <c>Visit</c> for.</summary>
        /// <typeparam name="TSeries">The kind of series.</typeparam>
        /// <param name="series">The series.</param>
        /// <param name="renderer">Makes the series' renderer over the context of the pass.</param>
        internal void Draw<TSeries>(TSeries series, Func<SeriesRenderContext, SeriesRenderer<TSeries>> renderer) where TSeries : ISeries
        {
            if (visitor is ISeriesRendererHost host) host.Draw(series, renderer);
        }
    }
}
