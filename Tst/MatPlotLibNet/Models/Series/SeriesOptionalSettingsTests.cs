// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Every <see cref="ISeries"/> of the one roster (<see cref="DrawnSeriesFixtures"/>) is drawn through the full <see cref="Plt"/>
/// pipeline with each optional setting it has given a value that is not its default, so the renderer's <c>if (Color != null)</c>,
/// <c>if (EdgeColor != null)</c>, alpha, marker, visibility and z-order branches are taken, which the theories at rest
/// (<see cref="AllSeriesTests"/>) leave unhit. The assertion is "no exception and an SVG", not a pinned output.</summary>
public class SeriesOptionalSettingsTests
{
    /// <summary>An optional setting a series may have: its name, and the change that gives it a value that is not its default; the
    /// change answers <see langword="false"/> for a series that does not have the setting.</summary>
    private readonly record struct Setting(string Name, Func<ISeries, bool> Give);

    /// <summary>The settings, each given a value that is not its default: an explicit colour, half alpha, an explicit edge, a square
    /// marker, hidden, and a z-order of 5 (the draw order's sort).</summary>
    private static readonly Setting[] Settings =
    [
        new("Color = red", series => series is IHasColor c && Given(() => c.Color = Colors.Red)),
        new("Alpha = 0.5", series => series is IHasAlpha a && Given(() => a.Alpha = 0.5)),
        new("EdgeColor = black", series => series is IHasEdgeColor e && Given(() => e.EdgeColor = Colors.Black)),
        new("MarkerStyle = square", series => series is IHasMarkerStyle m && Given(() => m.MarkerStyle = MarkerStyle.Square)),
        new("Visible = false", series => Given(() => series.Visible = false)),
        new("ZOrder = 5", series => Given(() => series.ZOrder = 5)),
    ];

    private static bool Given(Action give)
    {
        give();
        return true;
    }

    /// <summary>One case per kind: each setting the kind has is given on a fresh instance, which is drawn; every setting under which it
    /// draws no SVG is named.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryOptionalSetting_GivenAValue_RendersWithoutError(string type)
    {
        var failed = Settings.Where(setting =>
        {
            var series = DrawnSeriesFixtures.Instance(type);
            if (!setting.Give(series)) return false;
            string svg = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).ToSvg();
            return !svg.StartsWith("<svg", StringComparison.Ordinal);
        }).Select(setting => setting.Name).ToList();

        Assert.True(failed.Count == 0, $"{type} drew no SVG with {string.Join(", ", failed)}.");
    }
}
