// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A tile that LEADS somewhere says so — and it says so three ways at once, never by colour alone
/// (WCAG 1.4.1 / 1.4.11, the accessible-card pattern): the tile becomes a real SVG hyperlink (matplotlib's own
/// <c>Artist.set_url</c> idiom, rendered as <c>&lt;a href&gt;</c>), so it is focusable and Enter-activatable
/// with no script; the pointer cursor; and a chevron drawn in the tile that points right while the panel is
/// closed and down while it is open. A tile without a link keeps exactly the markup it always had.</summary>
public class StatTileLinkTests
{
    private static string Render(Action<StatTileSeries> configure) =>
        Plt.Create().WithSize(400, 200)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42, configure))
            .ToSvg();

    [Fact]
    public void ATileWithoutALink_CarriesNoAnchorAndNoChevron()
    {
        string svg = Render(t => t.Label = "Processes");

        Assert.DoesNotContain("<a ", svg);
        Assert.DoesNotContain("mpl-tile-chevron", svg);
        Assert.DoesNotContain("cursor:pointer", svg);
    }

    [Fact]
    public void ALinkedTile_IsAnAnchorWithAPointerAndALabel()
    {
        string svg = Render(t =>
        {
            t.Label = "Processes";
            t.Url = "/?panel=processes";
        });

        Assert.Contains("<a href=\"/?panel=processes\"", svg);
        Assert.Contains("aria-label=\"Processes", svg);
        Assert.Contains("cursor:pointer", svg);
        Assert.Contains("</a>", svg);
    }

    /// <summary>The disclosure pattern's one indispensable ARIA attribute (W3C APG, disclosure card): a decorative
    /// chevron does not announce open/closed to a screen reader; <c>aria-expanded</c> does.</summary>
    [Fact]
    public void ALinkedTile_AnnouncesItsOpenState()
    {
        string closed = Render(t => t.Url = "/?panel=processes");
        string open = Render(t => { t.Url = "/?panel=processes"; t.Expanded = true; });

        Assert.Contains("aria-expanded=\"false\"", closed);
        Assert.Contains("aria-expanded=\"true\"", open);
    }

    [Fact]
    public void TheUrl_IsEscapedForXml()
    {
        string svg = Render(t => t.Url = "/?panel=a&b=<c>");

        Assert.Contains("href=\"/?panel=a&amp;b=&lt;c&gt;\"", svg);
    }

    [Fact]
    public void TheChevron_PointsRightWhenClosedAndDownWhenOpen()
    {
        string closed = Render(t => { t.Label = "Processes"; t.Url = "/?panel=processes"; });
        string open = Render(t => { t.Label = "Processes"; t.Url = "/?panel=processes"; t.Expanded = true; });

        Assert.Contains("mpl-tile-chevron", closed);
        Assert.Contains("mpl-tile-chevron", open);
        Assert.NotEqual(Chevron(closed), Chevron(open));
    }

    [Fact]
    public void TheAnchorWrapsTheWholeTile_HeadlineLabelAndTrend()
    {
        string svg = Render(t =>
        {
            t.Label = "Processes";
            t.Url = "/?panel=processes";
            t.Trend = [1.0, 2.0, 3.0];
        });

        int open = svg.IndexOf("<a href", StringComparison.Ordinal);
        int close = svg.IndexOf("</a>", StringComparison.Ordinal);
        Assert.True(open >= 0 && close > open);
        string inside = svg[open..close];
        Assert.Contains(">42<", inside);
        Assert.Contains("Processes", inside);
        Assert.Contains("<polyline", inside);
    }

    private static string Chevron(string svg)
    {
        int at = svg.IndexOf("mpl-tile-chevron", StringComparison.Ordinal);
        int end = svg.IndexOf("/>", at, StringComparison.Ordinal);
        return svg[at..end];
    }
}
