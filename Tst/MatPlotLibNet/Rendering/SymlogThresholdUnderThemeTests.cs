// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The threshold a symmetric-log axis is linear within when its author gives none is the theme's: matplotlib's
/// <c>SymmetricalLogScale</c> takes <c>linthresh=2</c> (printed with 3.11.2: <c>set_yscale('symlog')</c> gives a transform of linthresh 2
/// under the default and the classic style), the library's own themes keep 1. A threshold its author gives wins under every theme and is
/// the one the JSON writes.</summary>
public class SymlogThresholdUnderThemeTests
{
    private static Figure Symlog(Action<AxesBuilder> scale, Theme theme) =>
        Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => scale(ax.Plot([0.0, 1, 2, 3], [-3200, -0.7, 0.9, 2900]))).Build();

    [Fact]
    public void TheMatplotlibThemes_NameTheThresholdOfMatplotlib_TheLibrarysNone()
    {
        Assert.Equal(2, Theme.MatplotlibV2.Looks.Range.SymLogLinThresh);
        Assert.Equal(2, Theme.MatplotlibClassic.Looks.Range.SymLogLinThresh);
        Assert.Null(Theme.Default.Looks.Range.SymLogLinThresh);
    }

    [Fact]
    public void AnAxisGivenNoThreshold_IsLinearWithinTheThemesThreshold()
    {
        var axis = new Axis { Scale = AxisScale.SymLog };

        Assert.Equal(2, axis.ScaleRuleUnder(Theme.MatplotlibV2).LinearThreshold);
        Assert.Equal(1, axis.ScaleRuleUnder(Theme.Default).LinearThreshold);
        Assert.Null(axis.GivenSymLogLinThresh);
    }

    [Fact]
    public void AThresholdItsAuthorGives_WinsUnderEveryTheme()
    {
        var axis = new Axis { Scale = AxisScale.SymLog, SymLogLinThresh = 1.0 };

        Assert.Equal(1, axis.ScaleRuleUnder(Theme.MatplotlibV2).LinearThreshold);
        Assert.Equal(1.0, axis.GivenSymLogLinThresh);
    }

    [Fact]
    public void TheVerbWithNoThreshold_GivesNone_AndLeavesTheLocatorToTheScale()
    {
        var figure = Symlog(ax => ax.WithSymlogYScale(), Theme.MatplotlibV2);
        var axis = figure.SubPlots[0].YAxis;

        Assert.Equal(AxisScale.SymLog, axis.Scale);
        Assert.Null(axis.GivenSymLogLinThresh);
        Assert.Null(axis.TickLocator);
        Assert.IsType<SymlogLocator>(axis.LocatedTickSource(isX: false, Theme.MatplotlibV2)!.Value.Locator);
    }

    [Fact]
    public void UnderAMatplotlibTheme_TheVerbWithNoThreshold_DrawsAsAThresholdOf2()
    {
        Assert.Equal(Symlog(ax => ax.WithSymlogYScale(2), Theme.MatplotlibV2).ToSvg(),
            Symlog(ax => ax.WithSymlogYScale(), Theme.MatplotlibV2).ToSvg());
        Assert.NotEqual(Symlog(ax => ax.WithSymlogYScale(1), Theme.MatplotlibV2).ToSvg(),
            Symlog(ax => ax.WithSymlogYScale(), Theme.MatplotlibV2).ToSvg());
        Assert.Equal(Symlog(ax => ax.WithSymlogYScale(1), Theme.Default).ToSvg(),
            Symlog(ax => ax.WithSymlogYScale(), Theme.Default).ToSvg());
    }

    [Fact]
    public void TheJson_WritesAGivenThresholdOnly_AndATheme_ItsOwn()
    {
        string given = Symlog(ax => ax.WithSymlogYScale(1), Theme.MatplotlibV2).ToJson();
        string none = Symlog(ax => ax.WithSymlogYScale(), Theme.MatplotlibV2).ToJson();

        Assert.Contains("symLogLinThresh", given, StringComparison.Ordinal);
        Assert.DoesNotContain("symLogLinThresh", none, StringComparison.Ordinal);
        Assert.Equal(2, Theme.MatplotlibV2.ThroughDto().Looks.Range.SymLogLinThresh);
        Assert.Null(ThemeDto.From(Theme.Default).SymlogThreshold);
    }
}
