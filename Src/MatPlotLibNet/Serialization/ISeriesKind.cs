// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>A kind of series that is written to a figure's JSON under a name of its own, the <c>type</c> of its record. The name is a
/// member of the class that is the kind, so its writer (<see cref="SeriesKindExtensions.WireKind"/>) and the registry that reads it back
/// (<see cref="SeriesRegistry"/>) take it from one place and cannot drift apart. Internal: what the library adds to a series stays off
/// the public interfaces; a kind of your own is registered under a name with <see cref="SeriesRegistry.Register"/>.</summary>
/// <typeparam name="TSelf">The series class that is the kind.</typeparam>
internal interface ISeriesKind<TSelf> where TSelf : ISeriesKind<TSelf>
{
    /// <summary>The name the kind is written under: the <c>type</c> of its record, in lower case.</summary>
    static abstract string Kind { get; }
}
