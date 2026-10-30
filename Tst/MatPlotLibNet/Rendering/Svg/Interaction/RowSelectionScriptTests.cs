// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The charts of one group on a page are linked by the page, not by a server: a brush on one chart works out which
/// rows it covers (the marks that carry <c>data-row</c>) and says so to the window as <c>mpl:rows</c> with the group's name, and
/// every chart of that group lights those rows. A chart of another group, or of none, hears nothing. A chart that is mounted again
/// leaves no second listener behind, and Escape clears the selection.</summary>
public class RowSelectionScriptTests
{
    private static readonly double[] X = [1, 2, 3, 4];
    private static readonly double[] Y = [10, 20, 30, 40];
    private static readonly string[] Keys = ["a", "b", "c", "d"];

    private static Action<FigureBuilder> Chart(string? group, string[]? selected = null, bool brush = true) => b =>
    {
        b.WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys);
        if (group is not null) b.LinkSelection(group, selected);
        else if (brush) b.WithSelection();
    };

    /// <summary>The marks of the chart <paramref name="chart"/>, by key.</summary>
    private static string[] Selected(InteractionScriptHarness h, int chart, string state) =>
        [.. h.Document.QuerySelectorAllRaw($"#mpl-chart-{chart} [data-row]")
            .Where(mark => mark.getAttribute("aria-selected") == state)
            .Select(mark => mark.getAttribute("data-row")!)];

    private static (double X, double Y) CentreOf(InteractionScriptHarness h, int chart, string key)
    {
        var mark = h.Document.QuerySelectorAllRaw($"#mpl-chart-{chart} [data-row='{key}']").Single();
        return (double.Parse(mark.getAttribute("cx")!, System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(mark.getAttribute("cy")!, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A brush on chart <paramref name="chart"/> over the rectangle that covers the marks of the given keys and no other.</summary>
    private static void Brush(InteractionScriptHarness h, int chart, params string[] keys)
    {
        var centres = keys.Select(k => CentreOf(h, chart, k)).ToArray();
        var rect = new
        {
            x1 = centres.Min(c => c.X) - 3, y1 = centres.Min(c => c.Y) - 3,
            x2 = centres.Max(c => c.X) + 3, y2 = centres.Max(c => c.Y) + 3,
        };
        h.Document.querySelector($"#mpl-chart-{chart}")!.dispatchEvent(new DomEvent("mpl:selection") { detail = rect });
    }

    [Fact]
    public void ABrushOnOneChart_LightsTheSameRowsOnTheOthersOfItsGroup()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"), Chart("orders"));

        Brush(h, 0, "a", "b");

        Assert.Equal(["a", "b"], Selected(h, 0, "true"));    // the chart brushed
        Assert.Equal(["a", "b"], Selected(h, 1, "true"));    // the other chart of the group
        Assert.Equal(["c", "d"], Selected(h, 1, "false"));   // the rest are marked as not selected
    }

    [Fact]
    public void AChartOfAnotherGroup_OrOfNone_HearsNothing()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"), Chart("orders"), Chart("other"), Chart(null));

        Brush(h, 0, "b");

        Assert.Equal(["b"], Selected(h, 1, "true"));
        Assert.Empty(Selected(h, 2, "true"));
        Assert.Empty(Selected(h, 2, "false"));
        Assert.Empty(Selected(h, 3, "true"));   // the chart with no group: not linked, not touched
        Assert.Empty(Selected(h, 3, "false"));
    }

    [Fact]
    public void ABrushThatCoversNoMark_ClearsTheSelection()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"), Chart("orders"));
        Brush(h, 0, "a");

        h.Document.querySelector("#mpl-chart-0")!.dispatchEvent(new DomEvent("mpl:selection") { detail = new { x1 = 0.0, y1 = 0.0, x2 = 1.0, y2 = 1.0 } });

        Assert.Empty(Selected(h, 1, "true"));
        Assert.Empty(Selected(h, 1, "false"));   // nothing selected: no mark is dimmed or marked
    }

    [Fact]
    public void Escape_ClearsTheSelectionOnEveryChartOfTheGroup()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"), Chart("orders"));
        Brush(h, 0, "a", "c");

        h.Simulate("#mpl-chart-1", "keydown", e => e.key = "Escape");

        Assert.Empty(Selected(h, 0, "true"));
        Assert.Empty(Selected(h, 1, "true"));
    }

    [Fact]
    public void TheRowsAChartStartsWith_AreLitAtOnce_WithoutAnyBrush()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders", ["b", "d"]), Chart("orders"));

        Assert.Equal(["b", "d"], Selected(h, 0, "true"));
        Assert.Equal(["a", "c"], Selected(h, 0, "false"));
    }

    [Fact]
    public void ASelectedMark_IsNotDimmed_AndTheOthersAre()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"), Chart("orders"));

        Brush(h, 0, "b");

        var selected = h.Document.QuerySelectorAllRaw("#mpl-chart-1 [data-row='b']").Single();
        var other = h.Document.QuerySelectorAllRaw("#mpl-chart-1 [data-row='a']").Single();
        Assert.NotEqual("0.25", selected.style.opacity);
        Assert.Equal("0.25", other.style.opacity);
    }

    [Fact]
    public void AnEventOfAnotherGroup_OnTheWindow_ChangesNothing()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"));

        h.Window.dispatchEvent(new DomEvent("mpl:rows") { detail = new { group = "other", rows = new[] { "a" } } });

        Assert.Empty(Selected(h, 0, "true"));
    }

    [Fact]
    public void ARowTheChartDoesNotHave_IsIgnored_NotAnError()
    {
        using var h = InteractionScriptHarness.FromBuilders(Chart("orders"));

        h.Window.dispatchEvent(new DomEvent("mpl:rows") { detail = new { group = "orders", rows = new[] { "zzz", "a" } } });

        Assert.Equal(["a"], Selected(h, 0, "true"));
    }

    // ── remounting ──────────────────────────────────────────────────────────────

    private static Action<FigureBuilder> Served(string group) => b =>
    {
        b.WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys);
        b.LinkSelection(group);
        b.WithServerInteraction("c1", server => { });
    };

    [Fact]
    public void AChartMountedAgain_LeavesOneListenerOnTheWindow_NotTwo()
    {
        using var h = InteractionScriptHarness.FromBuilders(Served("orders"));
        Assert.Equal(1, h.Window.ListenerCount("mpl:rows"));

        h.Remount(Served("orders"));
        h.Window.dispatchEvent(new DomEvent("mpl:rows") { detail = new { group = "orders", rows = new[] { "b" } } });

        // The listener of the replaced chart goes the first time it hears the window, and the new one lights the new marks.
        Assert.Equal(1, h.Window.ListenerCount("mpl:rows"));
        Assert.Equal(["b"], Selected(h, 0, "true"));
    }

    [Fact]
    public void TheScript_IsInTheSvgOnlyForAChartWithAGroup()
    {
        string linked = Plt.Create().WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys).LinkSelection("orders").ToSvg();
        string plain = Plt.Create().WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys).WithSelection().ToSvg();

        Assert.Contains("mpl:rows", linked, StringComparison.Ordinal);
        Assert.DoesNotContain("mpl:rows", plain, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSvg_NamesItsGroup_AndItsStartingRows_OnTheRoot()
    {
        string svg = Plt.Create().WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys).LinkSelection("or\"ders", ["a", "b&c"]).ToSvg();

        var root = XDocument.Parse(svg).Root!;   // well-formed whatever the names hold
        Assert.Equal("or\"ders", root.Attribute("data-selection-group")!.Value);
        Assert.Equal("[\"a\",\"b\\u0026c\"]", root.Attribute("data-selected-rows")!.Value);
    }

    [Fact]
    public void AFigureWithNoGroup_WritesNeitherAttribute()
    {
        string svg = Plt.Create().WithSize(400, 300).Scatter(X, Y, s => s.RowKeys = Keys).WithSelection().ToSvg();

        Assert.DoesNotContain("data-selection-group", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-selected-rows", svg, StringComparison.Ordinal);
    }
}
