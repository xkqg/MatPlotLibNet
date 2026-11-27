// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What an axes draws past its spines, measured already: the layout hands these to a colour bar's room when it has measured
/// the bands it reads (<see cref="IAxesBands"/>).</summary>
/// <param name="Bands">The bands measured past the spines; both <see cref="Ends"/> and <see cref="Edges"/> give them.</param>
/// <param name="PastRight">How far past its right spine the axes draws; 0 when nothing is drawn there or it was not measured.</param>
internal readonly record struct MeasuredBands(EdgeBands Bands, double PastRight) : IAxesBands
{
    /// <inheritdoc />
    public EdgeBands Ends() => Bands;

    /// <inheritdoc />
    public EdgeBands Edges() => Bands;

    /// <inheritdoc />
    public double PastRightSpine() => PastRight;
}
