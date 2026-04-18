// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Playground;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase O — every public enum's (name → ordinal) mapping is pinned here.
/// Any new PR that REORDERS, RENUMBERS, REMOVES, or RENAMES an existing enum
/// member breaks this test. Binary-compatibility guarantee: callers compiled
/// against v1.7.2 that embed these ordinals into their IL keep the same
/// semantic in every later v1.7.x release.
///
/// <para><b>How to evolve an enum SAFELY:</b></para>
/// <list type="number">
///   <item><description>Pick the next unused ordinal for that enum (one greater than the current max).</description></item>
///   <item><description>Add the new member with an explicit <c>= N</c> clause at the END of the enum declaration.</description></item>
///   <item><description>Add the <c>(name, N)</c> entry to the corresponding dictionary below.</description></item>
///   <item><description>Never renumber, remove, or reorder existing entries — treat them as immutable.</description></item>
/// </list>
///
/// <para>To deprecate a value: apply <c>[Obsolete]</c> to the member but keep
/// the ordinal entry in this dictionary. Only a major version bump may delete.</para>
/// </summary>
public static class EnumOrdinalSnapshot
{
    /// <summary>Pinned enum-member ordinals. Alphabetical for easy scanning.
    /// Ordinals ARE part of the public contract — append only.</summary>
    public static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, int>> Pinned =
        new Dictionary<Type, IReadOnlyDictionary<string, int>>
        {
            [typeof(AnimationPlaybackState)] = new Dictionary<string, int> {
                ["Stopped"] = 0, ["Playing"] = 1, ["Paused"] = 2,
            },
            [typeof(ArrowStyle)] = new Dictionary<string, int> {
                ["None"] = 0, ["Simple"] = 1, ["FancyArrow"] = 2, ["Wedge"] = 3,
                ["CurveA"] = 4, ["CurveB"] = 5, ["CurveAB"] = 6,
                ["BracketA"] = 7, ["BracketB"] = 8, ["BracketAB"] = 9,
            },
            [typeof(AxisScale)] = new Dictionary<string, int> {
                ["Linear"] = 0, ["Log"] = 1, ["SymLog"] = 2, ["Logit"] = 3, ["Date"] = 4,
            },
            [typeof(BarAlignment)] = new Dictionary<string, int> {
                ["Center"] = 0, ["Edge"] = 1,
            },
            [typeof(BarMode)] = new Dictionary<string, int> {
                ["Grouped"] = 0, ["Stacked"] = 1,
            },
            [typeof(BarOrientation)] = new Dictionary<string, int> {
                ["Vertical"] = 0, ["Horizontal"] = 1,
            },
            [typeof(BlendMode)] = new Dictionary<string, int> {
                ["Normal"] = 0, ["Multiply"] = 1, ["Screen"] = 2, ["Overlay"] = 3,
            },
            [typeof(BoxStyle)] = new Dictionary<string, int> {
                ["None"] = 0, ["Square"] = 1, ["Round"] = 2,
                ["RoundTooth"] = 3, ["Sawtooth"] = 4,
            },
            [typeof(BreakStyle)] = new Dictionary<string, int> {
                ["Zigzag"] = 0, ["Straight"] = 1, ["None"] = 2,
            },
            [typeof(ColorBarExtend)] = new Dictionary<string, int> {
                ["Neither"] = 0, ["Min"] = 1, ["Max"] = 2, ["Both"] = 3,
            },
            [typeof(ColorBarOrientation)] = new Dictionary<string, int> {
                ["Vertical"] = 0, ["Horizontal"] = 1,
            },
            [typeof(ConnectionStyle)] = new Dictionary<string, int> {
                ["Straight"] = 0, ["Arc3"] = 1, ["Angle"] = 2, ["Angle3"] = 3,
            },
            [typeof(CoordinateSystem)] = new Dictionary<string, int> {
                ["Cartesian"] = 0, ["Polar"] = 1, ["ThreeD"] = 2,
            },
            [typeof(DateInterval)] = new Dictionary<string, int> {
                ["Years"] = 0, ["Months"] = 1, ["Weeks"] = 2, ["Days"] = 3,
                ["Hours"] = 4, ["Minutes"] = 5, ["Seconds"] = 6,
            },
            [typeof(DisplayMode)] = new Dictionary<string, int> {
                ["Inline"] = 0, ["Expandable"] = 1, ["Popup"] = 2,
            },
            [typeof(DrawStyle)] = new Dictionary<string, int> {
                ["Default"] = 0, ["StepsPre"] = 1, ["StepsMid"] = 2, ["StepsPost"] = 3,
            },
            [typeof(FontSlant)] = new Dictionary<string, int> {
                ["Normal"] = 0, ["Italic"] = 1, ["Oblique"] = 2,
            },
            [typeof(FontVariant)] = new Dictionary<string, int> {
                ["Default"] = 0, ["Roman"] = 1, ["Bold"] = 2,
                ["Italic"] = 3, ["Calligraphic"] = 4, ["BlackboardBold"] = 5,
            },
            [typeof(FontWeight)] = new Dictionary<string, int> {
                ["Light"] = 0, ["Normal"] = 1, ["Bold"] = 2,
            },
            [typeof(GridAxis)] = new Dictionary<string, int> {
                ["X"] = 0, ["Y"] = 1, ["Both"] = 2,
            },
            [typeof(GridWhich)] = new Dictionary<string, int> {
                ["Major"] = 0, ["Minor"] = 1, ["Both"] = 2,
            },
            [typeof(HatchPattern)] = new Dictionary<string, int> {
                ["None"] = 0, ["ForwardDiagonal"] = 1, ["BackDiagonal"] = 2,
                ["Horizontal"] = 3, ["Vertical"] = 4, ["Cross"] = 5,
                ["DiagonalCross"] = 6, ["Dots"] = 7, ["Stars"] = 8,
            },
            [typeof(HistType)] = new Dictionary<string, int> {
                ["Bar"] = 0, ["Step"] = 1, ["StepFilled"] = 2,
            },
            [typeof(MatPlotLibNet.Rendering.Layout.LabelPriority)] = new Dictionary<string, int> {
                ["Low"] = 0, ["Normal"] = 1, ["High"] = 2,
            },
            [typeof(LegendPosition)] = new Dictionary<string, int> {
                ["Best"] = 0, ["UpperRight"] = 1, ["UpperLeft"] = 2,
                ["LowerRight"] = 3, ["LowerLeft"] = 4, ["Right"] = 5,
                ["CenterLeft"] = 6, ["CenterRight"] = 7, ["LowerCenter"] = 8,
                ["UpperCenter"] = 9, ["Center"] = 10, ["OutsideRight"] = 11,
                ["OutsideLeft"] = 12, ["OutsideTop"] = 13, ["OutsideBottom"] = 14,
            },
            [typeof(LineStyle)] = new Dictionary<string, int> {
                ["Solid"] = 0, ["Dashed"] = 1, ["Dotted"] = 2,
                ["DashDot"] = 3, ["None"] = 4,
            },
            [typeof(MarkerStyle)] = new Dictionary<string, int> {
                ["None"] = 0, ["Circle"] = 1, ["Square"] = 2, ["Triangle"] = 3,
                ["Diamond"] = 4, ["Cross"] = 5, ["Plus"] = 6, ["Star"] = 7,
                ["Pentagon"] = 8, ["Hexagon"] = 9, ["TriangleDown"] = 10,
                ["TriangleLeft"] = 11, ["TriangleRight"] = 12,
            },
            [typeof(ModifierKeys)] = new Dictionary<string, int> {
                // [Flags] enum — values are powers of two, not sequential. The
                // contract: None=0, Shift=1, Ctrl=2, Alt=4 must never change.
                ["None"] = 0, ["Shift"] = 1, ["Ctrl"] = 2, ["Alt"] = 4,
            },
            [typeof(Orientation)] = new Dictionary<string, int> {
                ["Horizontal"] = 0, ["Vertical"] = 1,
            },
            [typeof(PlaygroundExample)] = new Dictionary<string, int> {
                ["LineChart"] = 0, ["BarChart"] = 1, ["ScatterPlot"] = 2,
                ["MultiSeries"] = 3, ["Heatmap"] = 4, ["PieChart"] = 5,
                ["Histogram"] = 6, ["ContourPlot"] = 7, ["Surface3D"] = 8,
                ["RadarChart"] = 9, ["ViolinPlot"] = 10, ["Candlestick"] = 11,
                ["Treemap"] = 12, ["SankeyFlow"] = 13, ["PolarLine"] = 14,
                ["MultiSubplot"] = 15,
            },
            [typeof(PointerButton)] = new Dictionary<string, int> {
                ["None"] = 0, ["Left"] = 1, ["Middle"] = 2, ["Right"] = 3,
            },
            [typeof(PriceSource)] = new Dictionary<string, int> {
                ["Close"] = 0, ["Open"] = 1, ["High"] = 2, ["Low"] = 3,
                ["HL2"] = 4, ["HLC3"] = 5, ["OHLC4"] = 6,
            },
            [typeof(SankeyLinkColorMode)] = new Dictionary<string, int> {
                ["Source"] = 0, ["Target"] = 1, ["Gradient"] = 2,
            },
            [typeof(SankeyNodeAlignment)] = new Dictionary<string, int> {
                ["Justify"] = 0, ["Left"] = 1, ["Right"] = 2, ["Center"] = 3,
            },
            [typeof(SankeyOrientation)] = new Dictionary<string, int> {
                ["Horizontal"] = 0, ["Vertical"] = 1,
            },
            [typeof(SignalDirection)] = new Dictionary<string, int> {
                ["Buy"] = 0, ["Sell"] = 1,
            },
            [typeof(SpinePosition)] = new Dictionary<string, int> {
                ["Edge"] = 0, ["Data"] = 1, ["Axes"] = 2,
            },
            [typeof(StackedBaseline)] = new Dictionary<string, int> {
                ["Zero"] = 0, ["Symmetric"] = 1,
                ["Wiggle"] = 2, ["WeightedWiggle"] = 3,
            },
            [typeof(StepPosition)] = new Dictionary<string, int> {
                ["Pre"] = 0, ["Mid"] = 1, ["Post"] = 2,
            },
            [typeof(TextAlignment)] = new Dictionary<string, int> {
                ["Left"] = 0, ["Center"] = 1, ["Right"] = 2,
            },
            [typeof(TextSpanKind)] = new Dictionary<string, int> {
                ["Normal"] = 0, ["Superscript"] = 1, ["Subscript"] = 2,
                ["FractionNumerator"] = 3, ["FractionDenominator"] = 4,
                ["Radical"] = 5, ["Accent"] = 6, ["LargeOperator"] = 7,
                ["OperatorSubscript"] = 8, ["OperatorSuperscript"] = 9,
                ["MatrixStart"] = 10, ["MatrixCell"] = 11,
                ["MatrixCellSeparator"] = 12, ["MatrixRowSeparator"] = 13,
                ["MatrixEnd"] = 14,
            },
            [typeof(TickDirection)] = new Dictionary<string, int> {
                ["In"] = 0, ["Out"] = 1, ["InOut"] = 2,
            },
            [typeof(TitleLocation)] = new Dictionary<string, int> {
                ["Left"] = 0, ["Center"] = 1, ["Right"] = 2,
            },
            [typeof(InteractionToolbar.ToolMode)] = new Dictionary<string, int> {
                ["Pan"] = 0, ["Zoom"] = 1, ["Rotate3D"] = 2,
                ["DataCursor"] = 3, ["SpanSelect"] = 4,
            },
            [typeof(ViolinSide)] = new Dictionary<string, int> {
                ["Both"] = 0, ["Low"] = 1, ["High"] = 2,
            },
        };
}

/// <summary>Phase O — enforces the pinned snapshot in <see cref="EnumOrdinalSnapshot.Pinned"/>.
/// If this test turns red, someone reordered / renumbered / removed / renamed an
/// enum member. Read the message, and either revert or extend the snapshot with
/// an intentional append.</summary>
public class EnumOrdinalContractTests
{
    [Theory]
    [MemberData(nameof(PinnedEnumTypes))]
    public void EnumOrdinals_MatchPinnedSnapshot(Type enumType)
    {
        var expected = EnumOrdinalSnapshot.Pinned[enumType];
        var actual = Enum.GetValues(enumType).Cast<Enum>()
            .ToDictionary(e => e.ToString(), e => Convert.ToInt32(e));

        // (1) Every pinned entry must exist at the pinned ordinal.
        foreach (var (name, ord) in expected)
        {
            Assert.True(actual.TryGetValue(name, out int have),
                $"{enumType.Name}.{name} (pinned ordinal {ord}) was REMOVED or RENAMED. " +
                $"If intentional, this is a breaking change — bump the major version. " +
                $"Otherwise, restore the member.");
            Assert.True(have == ord,
                $"{enumType.Name}.{name} expected pinned ordinal {ord} but got {have}. " +
                $"Inserting or renumbering existing members silently breaks binary " +
                $"compat for consumers compiled against earlier versions.");
        }

        // (2) Every live member must have a pinned entry.
        foreach (var name in actual.Keys)
        {
            Assert.True(expected.ContainsKey(name),
                $"{enumType.Name}.{name} exists in source but is NOT pinned. " +
                $"Add it to EnumOrdinalSnapshot.Pinned with a unique ordinal > all " +
                $"existing (append-only rule).");
        }
    }

    /// <summary>Discovery: every public enum in the MatPlotLibNet assembly must be
    /// pinned in <see cref="EnumOrdinalSnapshot.Pinned"/>. Adding a new public enum
    /// without registering it fails this test.</summary>
    [Fact]
    public void EveryPublicEnum_InPublicAssemblies_IsPinned()
    {
        var assemblies = new[]
        {
            typeof(MatPlotLibNet.Plt).Assembly,
            typeof(MatPlotLibNet.Playground.PlaygroundExample).Assembly,
        };

        var livePublicEnums = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsEnum && (t.IsPublic || (t.IsNestedPublic && (t.DeclaringType?.IsPublic ?? false))))
            .ToList();

        var pinnedTypes = EnumOrdinalSnapshot.Pinned.Keys.ToHashSet();

        var missing = livePublicEnums.Where(t => !pinnedTypes.Contains(t)).ToList();

        Assert.True(missing.Count == 0,
            $"Public enum(s) exist without a pin in EnumOrdinalSnapshot.Pinned: " +
            string.Join(", ", missing.Select(t => t.FullName)));
    }

    public static IEnumerable<object[]> PinnedEnumTypes() =>
        EnumOrdinalSnapshot.Pinned.Keys.Select(t => new object[] { t });
}
