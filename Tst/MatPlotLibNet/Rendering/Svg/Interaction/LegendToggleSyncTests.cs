// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>A series can have several legend entries (the named layers of a stacked area), all carrying its index. The toggle
/// script hides or shows the series once, and every entry of it shows the same state: clicking one dims them all.</summary>
public class LegendToggleSyncTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    private static InteractionScriptHarness NamedStack() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithLegendToggle()
            .StackPlot(X, [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0], [3.0, 4.0, 5.0]], s => s.Labels = ["a", "b", "c"])
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "line"));

    private static string? Style(DomElement element, string property)
    {
        foreach (var pair in (element.getAttribute("style") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split(':', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals(property, StringComparison.OrdinalIgnoreCase)) return kv[1].Trim();
        }

        return null;
    }

    private static DomElement[] Entries(InteractionScriptHarness h, int index) =>
        h.Document.QuerySelectorAllRaw($"[data-legend-index='{index}']");

    private static void Click(DomElement element) => element.Fire(new DomEvent("click") { target = element });

    [Fact]
    public void TheStack_HasOneEntryForEachNamedLayer_AllCarryingItsIndex()
    {
        using var h = NamedStack();

        Assert.Equal(["a", "b", "c"], Entries(h, 0).Select(e => e.getAttribute("aria-label")));
        Assert.Single(Entries(h, 1));
    }

    [Fact]
    public void ClickingOneLayersEntry_HidesTheStackOnce_AndDimsEveryEntryOfIt()
    {
        using var h = NamedStack();
        var parts = Entries(h, 0);

        Click(parts[1]);

        Assert.Equal("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.All(parts, part =>
        {
            Assert.Equal("0.3", Style(part, "opacity"));
            Assert.Equal("true", part.getAttribute("aria-pressed"));
        });
    }

    [Fact]
    public void TheEntryOfAnotherSeries_IsLeftAlone()
    {
        using var h = NamedStack();

        Click(Entries(h, 0)[0]);

        var line = Assert.Single(Entries(h, 1));
        Assert.NotEqual("0.3", Style(line, "opacity"));
        Assert.NotEqual("none", h.GetStyle("[data-series-index='1']", "display"));
    }

    [Fact]
    public void ClickingAgain_ShowsTheStack_AndRestoresEveryEntryOfIt()
    {
        using var h = NamedStack();
        var parts = Entries(h, 0);

        Click(parts[2]);
        Click(parts[0]);

        Assert.NotEqual("none", h.GetStyle("[data-series-index='0']", "display"));
        Assert.All(parts, part =>
        {
            Assert.Equal("1", Style(part, "opacity"));
            Assert.Equal("false", part.getAttribute("aria-pressed"));
        });
    }
}
