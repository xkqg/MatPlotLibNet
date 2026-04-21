// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase L.5 (v1.7.2, 2026-04-21) — single Theory replacing the four
/// identical <c>{Series}_RendersWithoutError</c> [Fact] methods that each only
/// asserted <c>Assert.Contains("&lt;svg")</c>. Each case now asserts two things:
/// SVG is non-empty AND contains the series-type-specific SVG element, giving a
/// meaningful failure signal rather than a vacuous "rendered some XML".</summary>
public class SimpleSeriesRenderTheoryTests
{
    public static IEnumerable<object[]> Cases() =>
    [
        [(Func<string>)(() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pointplot([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]]))
            .ToSvg()), "<circle"],
        [(Func<string>)(() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot([[1.0, 2.0, 3.0]]))
            .ToSvg()), "<line"],
        [(Func<string>)(() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Barbs([0.0, 1.0, 2.0], [0.0, 0.0, 0.0], [10.0, 20.0, 30.0], [0.0, 45.0, 90.0]))
            .ToSvg()), "<line"],
        [(Func<string>)(() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Countplot(["x", "y", "x", "z"]))
            .ToSvg()), "<rect"],
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Renders_ContainsExpectedElement(Func<string> renderFn, string expectedElement)
    {
        string svg = renderFn();
        Assert.NotEmpty(svg);
        Assert.Contains(expectedElement, svg);
    }
}
