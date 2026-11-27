// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The browser turns a 3-D scene and puts its marks back to front again for the new camera (<c>resortDepth</c> in the rotation
/// script). Every series of an interactive scene is drawn inside its group (and a series with an opacity inside an opacity group within
/// it), so the marks to sort are the children of those groups, not only the children of the data tier: marks left in the server's
/// order would paint a far face over a near one once the camera has turned.</summary>
public class ThreeDGroupResortTests
{
    private static double[,] Hill(int n, out double[] axis)
    {
        axis = Enumerable.Range(0, n).Select(i => -2.0 + 4.0 * i / (n - 1)).ToArray();
        var z = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                z[i, j] = Math.Exp(-(axis[i] * axis[i] + axis[j] * axis[j]) / 2);
        return z;
    }

    /// <summary>The marks of the series' group, in the order the document holds them, by their vertex data.</summary>
    private static List<string> MarksInTheSeriesGroup(InteractionScriptHarness h)
    {
        var scene = h.Document.QuerySelectorAllRaw(".mpl-3d-scene").Single();
        var group = scene.Xml.Each("g").Single(e => e.Attribute("data-series-index") is not null);
        return [.. group.Descendants().Where(e => e.Attribute("data-v3d") is not null).Select(e => e.Attribute("data-v3d")!.Value)];
    }

    [Fact]
    public void TheMarksInsideASeriesGroup_AreSortedAgain_WhenTheSceneTurns()
    {
        var z = Hill(6, out var axis);
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax.Surface(axis, axis, z, s => s.Label = "hill")));
        var before = MarksInTheSeriesGroup(h);

        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 300; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup", e => { e.clientX = 300; e.clientY = 0; });
        var after = MarksInTheSeriesGroup(h);

        Assert.Equal(before.Order(), after.Order());
        Assert.NotEqual(before, after);
    }
}
