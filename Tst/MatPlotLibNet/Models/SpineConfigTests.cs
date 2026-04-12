// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SpineConfig"/> and <see cref="SpinesConfig"/> default values.</summary>
public class SpineConfigTests
{
    /// <summary>
    /// Verifies that the default spine line width matches matplotlib's axes.linewidth (0.8 pt).
    /// </summary>
    [Fact]
    public void SpineConfig_DefaultLineWidth_MatchesMatplotlib()
    {
        var sc = new SpineConfig();
        Assert.Equal(0.8, sc.LineWidth);
    }

    /// <summary>Verifies that the default spine is visible.</summary>
    [Fact]
    public void SpineConfig_DefaultVisible_IsTrue()
    {
        var sc = new SpineConfig();
        Assert.True(sc.Visible);
    }

    /// <summary>Verifies that the default spine position is Edge.</summary>
    [Fact]
    public void SpineConfig_DefaultPosition_IsEdge()
    {
        var sc = new SpineConfig();
        Assert.Equal(SpinePosition.Edge, sc.Position);
    }

    /// <summary>Verifies that all four default spines share the same 0.8 pt line width.</summary>
    [Fact]
    public void SpinesConfig_AllDefaults_Have08LineWidth()
    {
        var spines = new SpinesConfig();
        Assert.Equal(0.8, spines.Top.LineWidth);
        Assert.Equal(0.8, spines.Bottom.LineWidth);
        Assert.Equal(0.8, spines.Left.LineWidth);
        Assert.Equal(0.8, spines.Right.LineWidth);
    }
}
