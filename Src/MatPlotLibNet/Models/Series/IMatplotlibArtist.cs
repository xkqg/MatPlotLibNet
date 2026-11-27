// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Something drawn that says what it is to matplotlib (<see cref="MatplotlibArtist"/>): a series, through its kind's
/// <see cref="ChartSeries.Artist"/>, and a patch of <c>AddPatch</c>, a <see cref="MatplotlibArtist.Patch"/> (the default of
/// <see cref="Styling.IEdgedPatch"/>). The one door a reader that
/// holds either asks it through (the edge of a patch-like shape, <see cref="Styling.IEdgedPatch"/>).</summary>
/// <remarks>Implemented explicitly once, by <see cref="ChartSeries"/>.</remarks>
internal interface IMatplotlibArtist
{
    /// <summary>What it is to matplotlib.</summary>
    MatplotlibArtist Artist { get; }
}
