// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>A figure that carries a script carries the script's code and nothing written for the person who maintains it: a
/// comment in the JavaScript is bytes in every interactive SVG, and it names members of the library that may be renamed
/// without the SVG ever being read. What a script's comments said stays in the C# comments above the script.</summary>
public partial class EmittedScriptsCarryNoCommentsTests
{
    /// <summary>A line comment (<c>//</c> at the start of a line or after a space, which a web address such as
    /// <c>http://</c> never has) or the start of a block comment.</summary>
    [GeneratedRegex(@"(^|[ \t])//|/\*", RegexOptions.Multiline)]
    private static partial Regex AComment();

    public static IEnumerable<object[]> Scripts() =>
    [
        ["3-D rotation", SvgTransform.GetScript()],
        ["custom tooltip", SvgTransform.CustomTooltipScript],
        ["highlight", SvgTransform.HighlightScript],
        ["zoom and pan", SvgTransform.GetZoomPanScript()],
        ["legend drag", SvgTransform.LegendDragScript],
        ["legend toggle", SvgTransform.LegendToggleScript],
        ["row selection", SvgTransform.RowSelectionScript],
        ["sankey hover", SvgTransform.SankeyHoverScript],
        ["selection", SvgTransform.SelectionScript],
        ["step through", StepThroughSvgTransform.GetScript()],
        ["treemap drilldown", SvgTransform.TreemapDrilldownScript],
        ["server interaction", SvgTransform.ServerInteractionScript(SvgTransform.ServerScriptBranches.None)],
        ["server interaction with every branch", SvgTransform.ServerInteractionScript(SvgTransform.ServerScriptBranches.All)],
    ];

    [Theory]
    [MemberData(nameof(Scripts))]
    public void AScriptTheWriterEmits_HoldsNoComment(string name, string script)
    {
        Assert.NotEmpty(script);
        var comments = AComment().Matches(script).Select(m => SourceFile.LineAt(script, m.Index).Trim()).ToList();

        Assert.True(comments.Count == 0, $"{name}: {comments.Count} comment(s), the first at: {comments.FirstOrDefault()}");
    }

    /// <summary>The whole document of an interactive 3-D figure, the script read out of it: it is there, it is the rotation script,
    /// and none of its lines is a comment.</summary>
    [Fact]
    public void TheSvgOfAnInteractive3DFigure_CarriesItsRotationScriptWithoutAComment()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax.Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build()
            .ToSvg();

        var scripts = svg.Scripts();

        Assert.Contains(scripts, s => s.Contains("mpl-3d-scene", StringComparison.Ordinal));
        Assert.All(scripts, s => Assert.Empty(AComment().Matches(s)));
    }
}
