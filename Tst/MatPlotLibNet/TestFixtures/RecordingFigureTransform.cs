// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests;

/// <summary>The transform a test registers or saves through: it writes <paramref name="writes"/>, counts its calls and, given
/// <paramref name="fails"/>, throws it after writing, as an export that fails half way does. It implements only the two-argument member,
/// so the interface's own member with options runs through it; a test that reads the options derives a leaf that lists the interface
/// again.</summary>
/// <param name="writes">The text the transform writes, as UTF-8.</param>
/// <param name="fails">The failure the transform throws after writing; <see langword="null"/> for none.</param>
internal class RecordingFigureTransform(string writes = "", Exception? fails = null) : IFigureTransform
{
    /// <summary>How many times the transform ran.</summary>
    public int Calls { get; private set; }

    public void Transform(Figure figure, Stream output)
    {
        Calls++;
        output.Write(Encoding.UTF8.GetBytes(writes));
        if (fails is not null) throw fails;
    }
}
