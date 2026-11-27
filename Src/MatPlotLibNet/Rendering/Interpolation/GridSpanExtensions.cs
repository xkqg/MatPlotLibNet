// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>A grid of values as the row-by-row span it is laid out as in memory, for an engine's one core over a value or a colour.</summary>
internal static class GridSpanExtensions
{
    extension<T>(T[,] grid) where T : struct
    {
        /// <summary>The grid's cells row by row, without a copy.</summary>
        internal Span<T> AsRows() =>
            MemoryMarshal.CreateSpan(
                ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(grid)),
                grid.Length);
    }
}
