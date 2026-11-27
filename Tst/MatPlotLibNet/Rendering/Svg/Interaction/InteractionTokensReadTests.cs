// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Every token of the <see cref="InteractionTheme"/> is written on the root when the figure's theme changes it, and read
/// back by the script that uses it, with the default theme's value when the root carries none. Before, only the highlight read its
/// token: the Sankey hover typed 0.08 and 0.25, the tooltips +12 and -4 (the server's +12 and +12), and nothing read the treemap's
/// transition, so five public settings changed the bytes of the root and nothing a reader saw.</summary>
public class InteractionTokensReadTests
{
    [Fact]
    public void EverySettingOfTheTheme_IsAToken()
    {
        var settings = typeof(InteractionTheme).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.Name != "EqualityContract").ToList();

        Assert.Equal(settings.Count, SvgTransform.InteractionToken.All.Length);
        foreach (var setting in settings)
        {
            var theme = InteractionTheme.Default with { };
            object current = setting.GetValue(InteractionTheme.Default)!;
            setting.SetValue(theme, current is int whole ? (object)(whole + 1) : (double)current + 0.25);
            var root = new StringBuilder();
            SvgTransform.InteractionToken.AppendDifferences(root, theme);

            Assert.True(root.Length > 0, $"{setting.Name} changed writes no token");
        }
    }

    private static string[] Scripts =>
    [
        SvgTransform.HighlightScript, SvgTransform.SankeyHoverScript, SvgTransform.CustomTooltipScript,
        SvgTransform.TreemapDrilldownScript, SvgTransform.ServerInteractionScript(SvgTransform.ServerScriptBranches.All),
    ];

    [Fact]
    public void EveryToken_IsReadByAScript_WithTheDefaultAsItsFallback()
    {
        string all = string.Concat(Scripts);

        Assert.All(SvgTransform.InteractionToken.All, token => Assert.Contains(token.Read, all, StringComparison.Ordinal));
    }

    private static readonly SankeyNode[] Nodes = [new("A"), new("B"), new("C"), new("D")];
    private static readonly SankeyLink[] Links = [new(0, 1, 5), new(2, 3, 5)];

    private static InteractionScriptHarness Sankey(InteractionTheme theme) => InteractionScriptHarness.FromBuilder(b => b
        .WithSize(600, 400).WithSankeyHover().WithInteractionTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Sankey(Nodes, Links).HideAllAxes()));

    [Theory]
    [InlineData(0.5, 0.6, "0.5", "0.6")]
    [InlineData(0.08, 0.25, "0.08", "0.25")]
    public void ASankeyHover_DimsWhatItCannotReach_ToTheThemesOpacities(double links, double nodes, string linkDim, string nodeDim)
    {
        using var h = Sankey(new InteractionTheme(SankeyDimLinkOpacity: links, SankeyDimNodeOpacity: nodes));

        h.Simulate("[data-sankey-node-id='0']", "mouseenter");

        Assert.Equal(linkDim, h.GetAttribute("[data-sankey-link-source='2']", "fill-opacity"));
        Assert.Equal(nodeDim, h.GetAttribute("[data-sankey-node-id='3']", "fill-opacity"));
    }

    private static InteractionScriptHarness Tooltips(InteractionTheme theme) => InteractionScriptHarness.FromBuilder(b => b
        .WithSize(600, 400).WithRichTooltips().WithInteractionTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])));

    [Theory]
    [InlineData(20.0, 6.0, "120px", "56px")]
    [InlineData(12.0, -4.0, "112px", "46px")]
    public void ATooltip_StandsAtTheThemesOffsetFromThePointer(double x, double y, string left, string top)
    {
        using var h = Tooltips(new InteractionTheme(TooltipOffsetX: x, TooltipOffsetY: y));
        var owner = h.Document.QuerySelectorAllRaw("g").First(g => g.Xml.Element("title") is not null);

        owner.Fire(new DomEvent("mouseover") { target = owner, clientX = 100, clientY = 50 });

        Assert.Equal(left, h.GetStyle("div[role='tooltip']", "left"));
        Assert.Equal(top, h.GetStyle("div[role='tooltip']", "top"));
    }

    /// <summary>On keyboard focus the tooltip is anchored on the top of the focused element, centred on it, and stands at the
    /// theme's offset from that point as from the pointer: the harness lays every box at (50, 30), 20 wide.</summary>
    [Fact]
    public void AFocusTooltip_StandsAtTheThemesOffsetFromTheTopOfTheElement()
    {
        using var h = Tooltips(InteractionTheme.Default);
        var owner = h.Document.QuerySelectorAllRaw("g").First(g => g.Xml.Element("title") is not null);

        owner.Fire(new DomEvent("focus") { target = owner });

        Assert.Equal("72px", h.GetStyle("div[role='tooltip']", "left"));
        Assert.Equal("26px", h.GetStyle("div[role='tooltip']", "top"));
    }

    [Fact]
    public void TheServersTooltip_StandsAtTheThemesOffsetFromThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableHover()).Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]));
        var hub = h.WireJsSignalRHub();
        h.Remount(b => b.WithSize(600, 500).WithServerInteraction("c", s => s.EnableHover()).Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]));

        h.Simulate("svg", "pointermove", e => { e.clientX = 100; e.clientY = 80; });
        hub.Deliver("ReceiveTooltipContent", "c", "tip");

        var tooltip = h.Document.QuerySelectorAllRaw("div[role=tooltip]").Single();
        Assert.Equal("112px", tooltip.style.left);
        Assert.Equal("76px", tooltip.style.top);
    }

    [Fact]
    public void AHighlight_DimsTheOtherSeries_ToAnOpacityOfNone_WhenTheThemeSaysSo()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b.WithHighlight().WithInteractionTheme(new InteractionTheme(HighlightOpacity: 0))
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "a").Plot([1.0, 2.0], [4.0, 3.0], s => s.Label = "b"));

        h.Simulate("[data-series-index='0']", "mouseenter");

        Assert.Equal("0", h.Document.querySelector("[data-series-index='1']")!.style.opacity);
    }

    private static TreeNode Tree => new()
    {
        Label = "All",
        Children = [new() { Label = "A", Value = 2, Children = [new() { Label = "a1", Value = 1 }, new() { Label = "a2", Value = 1 }] },
                    new() { Label = "B", Value = 1 }],
    };

    private static InteractionScriptHarness Treemap(InteractionTheme theme, bool reducedMotion = false) => InteractionScriptHarness.FromSvg(
        Plt.Create().WithSize(600, 500).WithTreemapDrilldown().WithInteractionTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(Tree).HideAllAxes()).Build().ToSvg(), reducedMotion);

    [Theory]
    [InlineData(350, false, "opacity 350ms, visibility 350ms")]
    [InlineData(600, false, "opacity 600ms, visibility 600ms")]
    [InlineData(600, true, "opacity 0ms, visibility 0ms")]
    public void ATreemapCollapse_FadesItsTilesOut_OverTheThemesTransition(int ms, bool reducedMotion, string transition)
    {
        using var h = Treemap(new InteractionTheme(TreemapTransitionMs: ms), reducedMotion);
        var svg = h.Chart();

        svg.Fire(new DomEvent("click") { target = h.Document.querySelector("rect[data-treemap-node='0.0']")! });

        var hidden = h.Document.querySelector("rect[data-treemap-node='0.0.0']")!;
        Assert.Equal("hidden", hidden.style.visibility);
        Assert.Equal("0", hidden.style.opacity);
        Assert.Equal(transition, hidden.style.transition);
    }
}
