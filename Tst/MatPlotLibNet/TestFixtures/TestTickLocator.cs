// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests;

/// <summary>A tick locator an application wrote, as a test hands one to an axis: it has no JSON form, answers what
/// <paramref name="locate"/> says for the range it is asked about and keeps every range it was asked about.</summary>
/// <param name="locate">The ticks of a range, from its low and its high end.</param>
internal sealed class TestTickLocator(Func<double, double, double[]> locate) : ITickLocator
{
    /// <summary>A locator that answers <paramref name="values"/> whatever the range.</summary>
    /// <param name="values">The ticks.</param>
    public TestTickLocator(params double[] values) : this((_, _) => values) { }

    /// <summary>Every range the locator was asked about, in order; written under its own lock.</summary>
    public List<Range1D> Asked { get; } = [];

    /// <summary>How many times the locator was asked.</summary>
    public int Calls => Asked.Count;

    public double[] Locate(double min, double max)
    {
        lock (Asked) Asked.Add(new Range1D(min, max));
        return locate(min, max);
    }
}
