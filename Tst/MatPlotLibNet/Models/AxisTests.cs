// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Axis"/> behavior.</summary>
public class AxisTests
{
    // --- TickConfig defaults (v1.1.2 matplotlib-fidelity fixes) ---

    // ── Axis.LabelRotation shorthand ─────────────────────────────────────────

    [Fact]
    public void LabelRotation_Set_UpdatesMajorTicks()
    {
        var axis = new Axis();
        axis.LabelRotation = -45.0;
        Assert.Equal(-45.0, axis.MajorTicks.LabelRotation);
    }

    [Fact]
    public void LabelRotation_Get_ReadsMajorTicks()
    {
        var axis = new Axis();
        axis.MajorTicks = axis.MajorTicks with { LabelRotation = 30.0 };
        Assert.Equal(30.0, axis.LabelRotation);
    }

    [Fact]
    public void LabelRotation_SetDoesNotAffectMinorTicks()
    {
        var axis = new Axis();
        double before = axis.MinorTicks.LabelRotation;
        axis.LabelRotation = -45.0;
        Assert.Equal(before, axis.MinorTicks.LabelRotation);
    }
}
