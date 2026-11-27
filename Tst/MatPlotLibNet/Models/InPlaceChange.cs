// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Changes one value in place inside a collection, the way a caller who kept the array changes it: the first element
/// of an array or a list, the first cell of a grid, the first element of the first inner array of a jagged one; for a list
/// of library objects, a collection inside the first of them.</summary>
internal static class InPlaceChange
{
    /// <summary>Changes one value inside <paramref name="value"/>.</summary>
    /// <returns><see langword="true"/> when a value was changed; <see langword="false"/> when there was nothing it could change.</returns>
    public static bool Apply(object? value)
    {
        switch (value)
        {
            case null:
                return false;
            case Array { Length: 0 }:
                return false;
            case Array array when array.GetType().GetElementType()!.IsArray:
                return Apply(array.GetValue(0));
            case Array array:
                {
                    int[] at = new int[array.Rank];
                    var other = Different(array.GetValue(at));
                    if (other is null) return false;
                    array.SetValue(other, at);
                    return true;
                }
            case IList { Count: > 0, IsReadOnly: false } list when !list.IsFixedSize:
                {
                    var other = Different(list[0]);
                    if (other is not null)
                    {
                        list[0] = other;
                        return true;
                    }

                    return Inside(list[0]);
                }
            case IList { Count: > 0 } list:
                return Inside(list[0]);
            default:
                return Inside(value);
        }
    }

    /// <summary>Changes a collection inside a library object (a tree node's children, a row's cells).</summary>
    private static bool Inside(object? item)
    {
        if (item is null || item.GetType().Assembly != typeof(Figure).Assembly) return false;
        foreach (var field in DataSnapshot.Plan(item.GetType()))
            if (Apply(field.GetValue(item))) return true;
        return false;
    }

    private static object? Different(object? value) => value switch
    {
        double d => double.IsNaN(d) ? 0.0 : d + 1,
        float f => f + 1,
        int i => i + 1,
        long l => l + 1,
        bool b => !b,
        string s => s + "x",
        DateTime t => t.AddDays(1),
        Color c => c == Colors.Red ? Colors.Blue : Colors.Red,
        MatPlotLibNet.Interaction.DataPoint p => new MatPlotLibNet.Interaction.DataPoint(p.DataX + 1, p.DataY),
        Enum e => NextOf(e),
        _ => null,
    };

    private static object? NextOf(Enum value)
    {
        foreach (var candidate in Enum.GetValues(value.GetType()))
            if (!candidate.Equals(value)) return candidate;
        return null;
    }
}
