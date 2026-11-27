// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace MatPlotLibNet.Rendering;

/// <summary>The order the steps of one pass are drawn in (<see cref="LayerStep"/>), by z-order, a tie in the order they were listed
/// (<see cref="Rendering.LayerOrder.SortByZ"/>): the one orderer. A series pass lists its grid, its patches and its series here
/// (<see cref="SeriesPass.Order"/>), and the Cartesian renderer its whole data layer (the grid, the spans, each patch, the reference
/// lines, the drawing tools, each series and the tick marks). A list of one step takes no array; a longer one a rented array, handed back
/// by <see cref="Return"/>.</summary>
internal struct LayerOrder
{
    /// <summary>The one step of a list of one, for each layer, shared and never written: a list of one is listed in place, its index 0
    /// (the only series, the only patch, the layer itself).</summary>
    private static readonly LayerStep[][] OneStep =
        [.. Enum.GetValues<DataLayer>().Select(layer => new[] { new LayerStep(0, 0, layer, 0) })];

    private LayerStep[] _steps;
    private int _count;
    private readonly bool _rented;

    /// <summary>An empty list with room for <paramref name="capacity"/> steps.</summary>
    /// <param name="capacity">How many steps will be listed: exactly, so a list of one takes no array.</param>
    internal LayerOrder(int capacity)
    {
        _rented = capacity > 1;
        _steps = _rented ? ArrayPool<LayerStep>.Shared.Rent(capacity) : [];
        _count = 0;
    }

    /// <summary>Lists one step, after the steps listed before it.</summary>
    /// <param name="z">Where it stands in the z-order.</param>
    /// <param name="layer">What it draws.</param>
    /// <param name="index">Which patch or series it draws; 0 for a layer that draws all of its kind.</param>
    internal void Add(double z, DataLayer layer, int index)
    {
        if (!_rented)
        {
            _steps = OneStep[(int)layer];
            _count = 1;
            return;
        }
        _steps[_count] = new LayerStep(z, _count, layer, index);
        _count++;
    }

    /// <summary>Puts the listed steps in the order they are drawn in.</summary>
    internal readonly void Sort() => SortByZ(Steps);

    /// <summary>The steps in the order they were listed, or drawn in once <see cref="Sort"/> was called.</summary>
    internal readonly Span<LayerStep> Steps => _steps.AsSpan(0, _count);

    /// <summary>Hands a rented array back; the steps are not read after it.</summary>
    internal readonly void Return()
    {
        if (_rented) ArrayPool<LayerStep>.Shared.Return(_steps);
    }

    /// <summary>Puts the steps in the order they are drawn in: by <see cref="LayerStep.Z"/>, a tie by
    /// <see cref="LayerStep.Place"/>. Steps listed in that order already, as every step at rest is under the library's own
    /// themes, are not sorted.</summary>
    /// <param name="steps">The value it works on.</param>
    internal static void SortByZ(Span<LayerStep> steps)
    {
        for (int i = 1; i < steps.Length; i++)
        {
            if (steps[i].Z < steps[i - 1].Z)
            {
                steps.Sort(static (a, b) => a.Z != b.Z ? a.Z.CompareTo(b.Z) : a.Place.CompareTo(b.Place));
                return;
            }
        }
    }
}
