// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    /// <summary>The WHOLE CARD is the target, not just the ink on it. An anchor around drawn text and a
    /// polyline is only hittable where something is actually painted, so a reader had to land the pointer on a
    /// glyph — reported from an ops wall (2026-09-02: "moeilijk de mouse te plaatsen om te kunnen klikken").
    /// A transparent hit rect over the tile's own bounds makes every pixel of the card clickable, and paints
    /// nothing: fill-opacity 0 still takes pointer events, where fill="none" would not.</summary>
    [Fact]
    public void ALinkedTile_MakesItsWholeCardClickable_NotOnlyItsInk()
    {
        string svg = Render(t =>
        {
            t.Label = "Processes";
            t.Url = "/?panel=processes";
        });

        int open = svg.IndexOf("<a href", StringComparison.Ordinal);
        int close = svg.IndexOf("</a>", StringComparison.Ordinal);
        string inside = svg[open..close];
        Assert.Contains("mpl-tile-hit", inside);
        Assert.Contains("fill-opacity=\"0\"", inside);
        Assert.DoesNotContain("mpl-tile-hit", svg[..open]);
    }

    /// <summary>An unlinked tile has nothing to hit — the invisible rect exists only to carry the link.</summary>
    [Fact]
    public void AnUnlinkedTile_CarriesNoHitRect()
    {
        Assert.DoesNotContain("mpl-tile-hit", Render(t => t.Label = "Processes"));
    }

    /// <summary>The chevron is a POINTING DEVICE TARGET, and 8 px is under every guideline for one (WCAG 2.5.8
    /// asks 24x24 CSS px). It keeps its shape and grows to a size a mouse can actually land on.</summary>
    [Fact]
    public void TheChevron_IsLargeEnoughToAimAt()
    {
        string svg = Render(t =>
        {
            t.Label = "Processes";
            t.Url = "/?panel=processes";
        });

        var xs = new List<double>();
        var ys = new List<double>();
        string chevron = Chevron(svg);
        int pts = chevron.IndexOf("points=\"", StringComparison.Ordinal) + 8;
        foreach (var pair in chevron[pts..chevron.IndexOf('"', pts)].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var xy = pair.Split(',');
            xs.Add(double.Parse(xy[0], CultureInfo.InvariantCulture));
            ys.Add(double.Parse(xy[1], CultureInfo.InvariantCulture));
        }

        Assert.True(xs.Max() - xs.Min() >= 12, $"chevron width {xs.Max() - xs.Min()}");
        Assert.True(ys.Max() - ys.Min() >= 12, $"chevron height {ys.Max() - ys.Min()}");
    }

    private static string Chevron(string svg)
    {
        int at = svg.IndexOf("mpl-tile-chevron", StringComparison.Ordinal);
        int end = svg.IndexOf("/>", at, StringComparison.Ordinal);
        return svg[at..end];
    }
}
