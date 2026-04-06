// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Verifies <see cref="MplChart"/> behavior.</summary>
public class MplChartTests : BunitContext
{
    /// <summary>Verifies that the component renders SVG content inside a container div when a figure is provided.</summary>
    [Fact]
    public void Renders_SvgContent_WhenFigureProvided()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        var cut = Render<MplChart>(parameters =>
            parameters.Add(p => p.Figure, figure));

        var div = cut.Find(".mpl-chart");
        Assert.NotNull(div);
        var svgElement = cut.Find("svg");
        Assert.NotNull(svgElement);
    }

    /// <summary>Verifies that the component renders an empty container div when no figure is provided.</summary>
    [Fact]
    public void Renders_EmptyDiv_WhenFigureIsNull()
    {
        var cut = Render<MplChart>();

        var div = cut.Find(".mpl-chart");
        Assert.NotNull(div);
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("svg"));
    }

    /// <summary>Verifies that the component re-renders with updated content when the figure changes.</summary>
    [Fact]
    public void ReRenders_WhenFigureChanges()
    {
        var fig1 = Plt.Create().WithTitle("First").Plot([1.0], [1.0]).Build();
        var fig2 = Plt.Create().WithTitle("Second").Plot([2.0], [2.0]).Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, fig1));
        Assert.Contains("First", cut.Markup);

        // Re-render with new figure
        var cut2 = Render<MplChart>(p => p.Add(x => x.Figure, fig2));
        Assert.Contains("Second", cut2.Markup);
        Assert.DoesNotContain("First", cut2.Markup);
    }

    /// <summary>Verifies that the rendered SVG has a viewBox matching the configured figure dimensions.</summary>
    [Fact]
    public void SvgHasCorrectViewBox()
    {
        var figure = Plt.Create()
            .WithSize(1024, 768)
            .Plot([1.0], [2.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        var svg = cut.Find("svg");
        var viewBox = svg.GetAttribute("viewBox");
        Assert.NotNull(viewBox);
        Assert.Contains("1024", viewBox);
        Assert.Contains("768", viewBox);
    }

    /// <summary>Verifies that the container div has the mpl-chart CSS class.</summary>
    [Fact]
    public void ContainerDiv_HasMplChartClass()
    {
        var figure = Plt.Create().Build();
        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        var div = cut.Find("div.mpl-chart");
        Assert.NotNull(div);
    }

    /// <summary>Verifies that a custom CSS class can be added to the chart container.</summary>
    [Fact]
    public void CssClass_CanBeAdded()
    {
        var figure = Plt.Create().Build();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.CssClass, "my-custom-chart");
        });

        var div = cut.Find("div.mpl-chart");
        Assert.Contains("my-custom-chart", div.GetAttribute("class"));
    }

    /// <summary>Verifies that a line chart renders a polyline element in the SVG output.</summary>
    [Fact]
    public void LineChart_ContainsPolylineInSvg()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("<polyline", cut.Markup);
    }

    /// <summary>Verifies that a bar chart renders rect elements in the SVG output.</summary>
    [Fact]
    public void BarChart_ContainsRectInSvg()
    {
        var figure = Plt.Create()
            .Bar(["A", "B"], [10.0, 20.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("<rect", cut.Markup);
    }

    /// <summary>Verifies that a scatter chart renders circle elements in the SVG output.</summary>
    [Fact]
    public void ScatterChart_ContainsCircleInSvg()
    {
        var figure = Plt.Create()
            .Scatter([1.0, 2.0], [3.0, 4.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("<circle", cut.Markup);
    }

    /// <summary>Verifies that the figure title appears in the rendered SVG markup.</summary>
    [Fact]
    public void Title_AppearsInRenderedSvg()
    {
        var figure = Plt.Create()
            .WithTitle("Dashboard")
            .Plot([1.0], [2.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("Dashboard", cut.Markup);
    }

    /// <summary>Verifies that the dark theme renders a dark background color in the SVG.</summary>
    [Fact]
    public void DarkTheme_RendersDarkBackground()
    {
        var figure = Plt.Create()
            .WithTheme(Theme.Dark)
            .Plot([1.0], [2.0])
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("#1C1C1C", cut.Markup);
    }

    /// <summary>Verifies that an empty figure still renders a valid SVG element.</summary>
    [Fact]
    public void EmptyFigure_StillRendersValidSvg()
    {
        var figure = Plt.Create().Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        var svg = cut.Find("svg");
        Assert.NotNull(svg);
        Assert.Contains("</svg>", cut.Markup);
    }

    /// <summary>Verifies that multiple subplots all render their titles in the SVG output.</summary>
    [Fact]
    public void MultipleSubPlots_AllRenderInSvg()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Left").Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Right").Plot([3.0], [4.0]))
            .Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        Assert.Contains("Left", cut.Markup);
        Assert.Contains("Right", cut.Markup);
    }

    /// <summary>Verifies that the default display mode is inline with no expandable or popup classes.</summary>
    [Fact]
    public void DisplayMode_DefaultIsInline()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));

        var div = cut.Find("div.mpl-chart");
        Assert.NotNull(div);
        Assert.DoesNotContain("mpl-expandable", div.GetAttribute("class"));
        Assert.DoesNotContain("mpl-popup", div.GetAttribute("class"));
    }

    /// <summary>Verifies that the expandable display mode renders an expand button.</summary>
    [Fact]
    public void DisplayMode_Expandable_RendersExpandButton()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.DisplayMode, DisplayMode.Expandable);
        });

        var btn = cut.Find("button.mpl-expand-btn");
        Assert.NotNull(btn);
        Assert.Contains("Expand", btn.TextContent);
    }

    /// <summary>Verifies that the expandable display mode applies the mpl-expandable CSS class.</summary>
    [Fact]
    public void DisplayMode_Expandable_HasExpandableClass()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.DisplayMode, DisplayMode.Expandable);
        });

        var div = cut.Find("div.mpl-expandable");
        Assert.NotNull(div);
    }

    /// <summary>Verifies that the popup display mode renders a link with the correct href and target.</summary>
    [Fact]
    public void DisplayMode_Popup_RendersPopupLink()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.DisplayMode, DisplayMode.Popup);
            p.Add(x => x.PopupUrl, "http://localhost:5000/chart/abc");
        });

        var link = cut.Find("a.mpl-popup-link");
        Assert.NotNull(link);
        Assert.Equal("http://localhost:5000/chart/abc", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
    }

    /// <summary>Verifies that the popup display mode applies the mpl-popup CSS class.</summary>
    [Fact]
    public void DisplayMode_Popup_HasPopupClass()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.DisplayMode, DisplayMode.Popup);
        });

        var div = cut.Find("div.mpl-popup");
        Assert.NotNull(div);
    }

    /// <summary>Verifies that the popup display mode does not render a link when PopupUrl is empty.</summary>
    [Fact]
    public void DisplayMode_Popup_NoLinkWhenPopupUrlEmpty()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.DisplayMode, DisplayMode.Popup);
        });

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("a.mpl-popup-link"));
    }
}
