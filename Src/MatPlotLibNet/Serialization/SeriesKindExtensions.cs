// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>How the writer of a series asks for the name of its kind: the one place the name of a kind leaves the class that holds it.</summary>
internal static class SeriesKindExtensions
{
    extension<TSeries>(TSeries series) where TSeries : ISeriesKind<TSeries>
    {
        /// <summary>The name this series is written under: the <c>type</c> of its record (<see cref="ISeriesKind{TSelf}.Kind"/>).</summary>
        internal string WireKind() => TSeries.Kind;
    }
}
