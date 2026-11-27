// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A type exists when it carries a concept more than one place uses. An internal type that exactly one other type uses is a
/// part of that type: it is declared inside it (a nested type) or becomes its members, so the namespace shows only the concepts the
/// library shares. The users are read by the compiler, not by name: a type that names the type or reaches one of its members uses it,
/// and a type that derives from a base of the library (or implements one of its interfaces) is used by every type that uses that base,
/// since a call of the base lands in it. A type that stays with one user names its reason, by its name in <see cref="Kept"/> or by its
/// kind in <see cref="KeptKinds"/>, and both lists are pinned both ways. A type a caller calls through one member (a static class of one
/// method, overloads of one name counted once, its constants not; a value that holds one value or none) is that member of the type of the
/// library that owns its subject: its one user, the class or struct it extends or the value it builds, or the base its users share. It
/// stays a type only when it extends what the library cannot give a member (a type of the framework, an enum, an interface), or when it
/// names its reason in <see cref="KeptOfOneMember"/>. A type nested in its one user is no concept of its own either: a static one is
/// members of the user or of the type that owns its subject, and one that holds a value stays a type only when two or more members of
/// the user hand that value to each other; a value one member makes and reads is that member's locals, however that member holds it (in
/// a list, an array, a query, a fold or an object).</summary>
public class SingleUserTypeContractTests
{
    /// <summary>An internal type that one other type uses, kept as a type of its own, and why.</summary>
    private readonly record struct KeptType(string Type, string Reason);

    /// <summary>A kind of internal type kept as a type of its own whatever uses it (a pattern over the full name), and why.</summary>
    private readonly record struct KeptKind(string Pattern, string Reason);

    private const string BesideAPublicInterface =
        "The one internal extension block on a public interface that reaches the internal interface beside it: what the library adds to "
        + "a public interface lives on an internal interface beside it, reached through one block on the public one, so an application can "
        + "still implement, proxy or mock the public interface (PublicInterfaceContractTests).";

    private const string Generated =
        "Its table is generated from the Unicode Character Database into a partial class of this name (tools/unicode/generate_bidi_tables.py "
        + "writes the .g.cs beside it), so the class is named by the generator.";

    private const string CompiledByTheUnoSuite =
        "The Uno suite compiles this file by itself (a Compile item of its project), since an Uno element cannot be built in a unit "
        + "test: what the element does with its input is held through this file alone.";

    /// <summary>The kinds of internal type kept whatever uses them, each with its reason.</summary>
    private static readonly KeptKind[] KeptKinds =
    [
        new(@"^MatPlotLibNet\.Numerics\.",
            "A calculation is declared in the Numerics namespace beside the other calculations (NumericsPlacementContractTests), not inside "
            + "the one type that calls it: the math of the library lives in one place."),
        new(@"^MatPlotLibNet\.Serialization\.\w+Dto$",
            "A record of the JSON a figure is written as: the serializer reads and writes it by its properties and the MCP server describes "
            + "its shape to a model, so it stands beside the other records of the document."),
        new(@"^MatPlotLibNet\.Serialization\.\w+Converter$",
            "A converter the serializer's options hold: the serializer calls it for every value of its type, not the type that registers it."),
    ];

    /// <summary>The internal types kept with one user, by full name, each with its reason.</summary>
    private static readonly KeptType[] Kept =
    [
        new("MatPlotLibNet.Mcp.ChartTools", "The tools of the MCP server: the host finds them by their attribute (WithTools) and calls them by name."),
        new("MatPlotLibNet.Blazor.SignalRHubConnector",
            "SignalR's connection, the half of the IHubConnector seam a client is made with; a test hands the client another connector through the same seam."),
        new("MatPlotLibNet.Uno.ClickCounter", CompiledByTheUnoSuite),
        new("MatPlotLibNet.Uno.UnoInputAdapter", CompiledByTheUnoSuite),
        new("MatPlotLibNet.Rendering.Text.BidiBrackets", Generated),
        new("MatPlotLibNet.Rendering.Text.BidiClassTable", Generated),
        new("MatPlotLibNet.Indicators.Streaming.StreamingPaneExtensions", BesideAPublicInterface),
        new("MatPlotLibNet.Rendering.SeriesRendererHostExtensions", BesideAPublicInterface),
        new("MatPlotLibNet.Rendering.Svg.SvgRendererDocumentExtensions", BesideAPublicInterface),
        new("MatPlotLibNet.Rendering.TextMeasurement.GlyphPathProviderExtensions", BesideAPublicInterface),
        new("MatPlotLibNet.Serialization.NormalizableExtensions", BesideAPublicInterface),
    ];

    /// <summary>The internal types of one member kept as a type of their own, by full name, each with its reason.</summary>
    private static readonly KeptType[] KeptOfOneMember =
    [
        new("MatPlotLibNet.Geo.GeoPackage", "The module initializer of the Geo package: the runtime calls it when the assembly loads, no type of the library does."),
        new("MatPlotLibNet.Interaction.AxisRangeEventExtensions",
            "How a range event sets the secondary slots of an axes, shared by the zoom and the pan events, whose common base is the base of every "
            + "interaction event; the axes, the public type the fluent builder is built on, takes no member of an event."),
        new("MatPlotLibNet.Interaction.ZoomNotch", "How far one notch of a wheel or a pinch zooms, one rule the touch, the wheel and the keyboard paths read."),
        new("MatPlotLibNet.Rendering.Text.BidiClassTable", Generated),
        new("MatPlotLibNet.Serialization.EnumWireNames<T>",
            "The names and values of one enum, read once per enum: the static fields of a generic class are one set per type argument."),
    ];

    /// <summary>The parts of one user that one member of it reads, kept as a type of their own by name, each with the rule its
    /// dissolving would break: no rule of this guard keeps them.</summary>
    private static readonly KeptType[] KeptParts =
    [
        new("MatPlotLibNet.Geo.GeoAxesExtensions.FlowCartogram",
            "The flow-based cartogram of Gastner, Seguy and More: the lattice, the density and the vertices its steps share. Its user is a "
            + "static class, which holds no fields, so its state as locals makes every step one method, and the tests of the steps (the "
            + "flow, the lattice's cosine series) would test nothing a caller can reach."),
        new("MatPlotLibNet.Rendering.ChartRenderer.ZoomIndicatorPath",
            "The zoom indicator's geometry and its drawing: its members in ChartRenderer would take it past the method budget "
            + "(ResponsibilityBudgetContractTests: 20 methods, which ChartRenderer declares)."),
        new("MatPlotLibNet.Rendering.DataTransform.RoomCut",
            "The cut of a path at the rooms of an axis drawn in parts: its members in DataTransform would add to a type over the method "
            + "budget, whose reason in ResponsibilityBudgetContractTests names this cut as RoomCut's."),
        new("MatPlotLibNet.Rendering.Text.BidiAlgorithm.BidiExplicitLevels",
            "Rules X1 to X8 of UAX #9, a part BidiAlgorithm was split into by clause "
            + "(ResponsibilityBudgetContractTests.EachTypeOfASplitClass_StandsInAFileOfItsOwn); its members in BidiAlgorithm would take it "
            + "past the method budget."),
        new("MatPlotLibNet.Rendering.Text.BidiAlgorithm.BidiNeutralTypes",
            "Rules N0 to N2 of UAX #9, a part BidiAlgorithm was split into by clause "
            + "(ResponsibilityBudgetContractTests.EachTypeOfASplitClass_StandsInAFileOfItsOwn); its members in BidiAlgorithm would take it "
            + "past the method budget."),
        new("MatPlotLibNet.Rendering.ThreeDAxesRenderer.CubeFrame",
            "The frame of one 3-D draw, a value on the stack its nine drawing steps read: as fields of the renderer it is carried by "
            + "every ThreeDAxesRenderer a draw makes (three, the layout's among them), measured 0.37 KB more a draw of a surface "
            + "(LiveWall Surface 2,302.26 to 2,302.63 KB), and as locals it is three more parameters on each of its nine steps."),
        new("MatPlotLibNet.Rendering.Text.BidiAlgorithm.BidiWeakTypes",
            "Rules W1 to W7 of UAX #9, one method a rule: its eight members in BidiAlgorithm would take it past the method budget "
            + "(ResponsibilityBudgetContractTests), as they would the run sequence's."),
    ];

    private static readonly Lazy<IReadOnlyList<TypeUse>> All = new(() => TypeUsers.Of(SourceCompilation.Packages));

    private static readonly Lazy<List<TypeUse>> Library = new(() => SingleUser(All.Value));

    [Fact]
    public void AnInternalTypeOneOtherTypeUses_IsPartOfThatType_OrNamesItsReason()
    {
        var unexplained = Library.Value.Where(use => !IsKept(use.Type)).Select(Describe).ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} internal types are used by one other type only; fold each into its user, or name it in Kept with the reason it stays:\n"
            + string.Join('\n', unexplained));
    }

    [Fact]
    public void EveryKeptTypeAndKind_StillStandsForATypeOfOneUser()
    {
        var single = Library.Value.Select(use => use.Type).ToList();

        var stale = Kept.Where(kept => !single.Contains(kept.Type, StringComparer.Ordinal)).Select(kept => kept.Type)
            .Concat(KeptKinds.Where(kind => !single.Any(type => Regex.IsMatch(type, kind.Pattern))).Select(kind => kind.Pattern))
            .ToList();

        Assert.True(stale.Count == 0, "Kept names a type or a kind that is gone or has more users now:\n" + string.Join('\n', stale));
    }

    [Fact]
    public void ATypeOfOneMember_IsAMemberOfTheTypeThatOwnsIt_OrNamesItsReason()
    {
        var unexplained = OfOneMember(All.Value)
            .Where(use => !KeptOfOneMember.Any(kept => kept.Type == use.Type) && !KeptKinds.Any(kind => Regex.IsMatch(use.Type, kind.Pattern)))
            .Select(use => $"{use.Type} ({use.File}): {use.Members[0]}, used by {use.Users.Count}")
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} internal types are called through one member that a type of the library could own; make it a member of its "
            + "one user, of the class or struct it extends or the value it builds, or of the base its users share, or name it in "
            + "KeptOfOneMember with the reason it stays:\n" + string.Join('\n', unexplained));
    }

    [Fact]
    public void EveryTypeKeptWithOneMember_StillHasOneMember() =>
        AssertEachStillStands(KeptOfOneMember, OfOneMember(All.Value), "KeptOfOneMember names a type that is gone or has more members now");

    /// <summary>Holds that every type <paramref name="kept"/> names is still among <paramref name="found"/>.</summary>
    private static void AssertEachStillStands(KeptType[] kept, List<TypeUse> found, string message)
    {
        var types = found.Select(use => use.Type).ToHashSet(StringComparer.Ordinal);

        var stale = kept.Where(entry => !types.Contains(entry.Type)).Select(entry => entry.Type).ToList();

        Assert.True(stale.Count == 0, message + ":\n" + string.Join('\n', stale));
    }

    [Theory]
    [InlineData("internal static class Lone { internal static int Of(int x) => x; } public class A { int M() => Lone.Of(1); } public class B { int M() => Lone.Of(2); }")]
    [InlineData("internal static class Lone { internal static int Of(int x) { return x; } internal static int Of(double x) { return 1; } } public class A { int M() => Lone.Of(1); }")]
    [InlineData("internal static class Lone { internal const int N = 2; private static readonly int[] Cache = [1]; internal static int Of(int x) => x * N + Cache[0]; }")]
    [InlineData("internal static class Lone { internal static int Size => 1; }")]
    [InlineData("public sealed class Box { } internal static class Lone { extension(Box box) { internal int Size => 1; } }")]
    [InlineData("public readonly struct Box { } internal static class Lone { extension(Box box) { internal int Size() => 1; } }")]
    [InlineData("public sealed class Box { } internal static class Lone { internal static int Size(this Box box) => 1; }")]
    [InlineData("public class Box { } internal static class Lone { extension<T>(T box) where T : Box { internal int Size => 1; } }")]
    [InlineData("public class User { private static class Lone { internal static int Of(int x) => x; } int M() => Lone.Of(1); }")]
    [InlineData("internal readonly record struct Lone(int Value);")]
    [InlineData("internal sealed class Lone(int value) { internal int Twice() => value * 2; }")]
    [InlineData("internal struct Lone { private int _value; internal int Next(int x) => _value += x; }")]
    public void ATypeOfOneMemberThatALibraryTypeCouldOwn_IsFound(string source) =>
        Assert.Contains(OfOneMember(TypeUsers.Of([Sample(source)])), use => use.Type.EndsWith("Lone", StringComparison.Ordinal));

    [Theory]
    [InlineData("internal static class Lone { extension(int value) { internal int Twice => value * 2; } }")]
    [InlineData("internal static class Lone { extension(string text) { internal int Twice() => text.Length * 2; } }")]
    [InlineData("public interface IBox { } internal static class Lone { extension(IBox box) { internal int Size => 1; } }")]
    [InlineData("public enum Side { Left, Right } internal static class Lone { extension(Side side) { internal int Sign => side == Side.Left ? -1 : 1; } }")]
    [InlineData("public interface IBox { } internal static class Lone { extension<T>(T box) where T : struct, IBox { internal int Size => 1; } }")]
    [InlineData("internal static class Lone { internal static int A() => 1; internal static int B() => 2; }")]
    [InlineData("public static class Lone { public static int Of(int x) => x; }")]
    [InlineData("internal abstract class Shape { internal abstract int Sides { get; } } internal sealed class Lone : Shape { internal override int Sides => 4; }")]
    [InlineData("internal class Lone { internal virtual int Sides => 3; } internal sealed class Square : Lone { internal override int Sides => 4; }")]
    [InlineData("internal sealed class Lone(int x) : IComparer<int> { public int Compare(int a, int b) => a - b + x; }")]
    [InlineData("internal struct Lone { private int _a; private int _b; internal int Next(int x) { _a += x; _b = _a; return _b; } }")]
    [InlineData("internal interface Lone { int Sides { get; } }")]
    [InlineData("internal sealed class Lone { private readonly int _start = 2; internal int Sides { get; } = 4; internal Lone(int sides) => Sides = sides + _start; }")]
    public void ATypeOfOneMemberNoLibraryTypeCouldOwn_AndATypeOfMore_AreNotFound(string source) =>
        Assert.DoesNotContain(OfOneMember(TypeUsers.Of([Sample(source)])), use => use.Type.EndsWith("Lone", StringComparison.Ordinal));

    /// <summary>The types of <paramref name="uses"/> called through one member that a type of the library could own.</summary>
    private static List<TypeUse> OfOneMember(IReadOnlyList<TypeUse> uses) =>
        [.. uses.Where(use => use.Members.Count == 1 && use.Shape is TypeShape.Owned or TypeShape.Wrapper)];

    [Fact]
    public void APartOfOneUser_IsAValueItsMembersHandEachOther()
    {
        var unexplained = PartsAUserCouldHold(All.Value).Where(use => !IsKept(use.Type) && !KeptParts.Any(kept => kept.Type == use.Type))
            .Select(use => $"{use.Type} ({use.File}): {use.Shape}, read by {string.Join(", ", use.Readers)}")
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} types nested in their one user carry no concept of their own: make the members of a static one the members "
            + "of the user or of the type that owns their subject, and the value one member makes and reads that member's locals, or name "
            + "it in KeptParts with the rule its dissolving would break:\n" + string.Join('\n', unexplained));
    }

    [Fact]
    public void EveryKeptPart_StillIsAPartItsUserCouldHold() =>
        AssertEachStillStands(KeptParts, PartsAUserCouldHold(All.Value), "KeptParts names a type that is gone or no part its user could hold now");

    [Theory]
    [InlineData("public class User { private static class Lone { internal static int Of(int x) => x; } int M() => Lone.Of(1); int N() => Lone.Of(2); }")]
    [InlineData("public static class User { internal static class Lone { internal const int N = 2; internal static int Of(int x) => x * N; internal static int Twice(int x) => 2 * x; } public static int M(int x) => Lone.Of(x) + Lone.Twice(x); }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); int M() { var lone = new Lone(1, 2); return lone.A + lone.B; } }")]
    [InlineData("public class User { internal sealed class Lone(int value) { internal int Twice() => value * 2; internal int Thrice() => value * 3; } int M() => new Lone(1).Twice() + new Lone(2).Thrice(); }")]
    [InlineData("public class User { private struct Lone { internal int A; internal int B; } int M() { Lone lone; lone.A = 1; lone.B = 2; return lone.A + lone.B; } }")]
    [InlineData("public class User { private enum Lone { A, B } int M(int x) => x == 0 ? (int)Lone.A : (int)Lone.B; }")]
    [InlineData("public class User { private sealed class Outer { private readonly record struct Lone(int A, int B); internal int M() => new Lone(1, 2).A; internal int N() => 1; } int P() => new Outer().M() + new Outer().N(); }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); int M() { var rows = new List<Lone> { new(1, 2) }; return rows[0].A; } }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); int M() { var rows = new Dictionary<int, Lone>(); rows[1] = new(1, 2); return rows[1].B; } }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); int M() { var rows = new Lone[1]; rows[0] = new(1, 2); return rows[0].A; } }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private static int Twice<T>(T state, System.Func<T, int> map) => map(state) * 2; int M() => Twice(new Lone(1, 2), static l => l.A + l.B); }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); int M(int[] v) => v.Select(x => new Lone(x, x)).ToList().Count; }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); internal object M() => new Lone(1, 2); }")]
    [InlineData("public class User { private static class Lone { internal static int Of(int x) => x; } int M0() => Lone.Of(0); " + TwentyMethods + "}")]
    [InlineData("public class User { private sealed class Lone(int value) { private int _seen; internal static Lone Of(int x) => new(x); internal int Twice() => _seen = value * 2; } int M0() => Lone.Of(0).Twice(); " + TwentyMethods + "}")]
    [InlineData("public class User { private static class Lone { internal const int N = 2; } int M0() => Lone.N; " + TwentyMethods + "}")]
    public void APartItsUserCouldHold_IsFound(string source) =>
        Assert.Contains(PartsAUserCouldHold(TypeUsers.Of([Sample(source)])), use => use.Type.EndsWith("Lone", StringComparison.Ordinal));

    /// <summary>The rest of a user at the method budget beside the one method that reads its part: a part is found whatever the size of
    /// its user.</summary>
    private const string TwentyMethods =
        "int M1() => 1; int M2() => 2; int M3() => 3; int M4() => 4; int M5() => 5; int M6() => 6; "
        + "int M7() => 7; int M8() => 8; int M9() => 9; int M10() => 10; int M11() => 11; int M12() => 12; int M13() => 13; int M14() => 14; "
        + "int M15() => 15; int M16() => 16; int M17() => 17; int M18() => 18; int M19() => 19; ";

    [Theory]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private Lone Make() => new(1, 2); int M() => Make().A; }")]
    [InlineData("public class User { private enum Lone { A, B } private Lone Pick(int x) => x == 0 ? Lone.A : Lone.B; int M(int x) => Pick(x) == Lone.A ? 1 : 2; }")]
    [InlineData("public class User { private sealed class Lone(int value) : System.IDisposable { public void Dispose() { } internal int V => value; } System.IDisposable M() => new Lone(1); }")]
    [InlineData("public class User { private interface Lone { int Sides { get; } } int M(object o) => ((Lone)o).Sides; }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private Lone[] _rows = [new(1, 2)]; int M() => _rows[0].A; }")]
    [InlineData("internal readonly record struct Lone(int A, int B); public class User { int M() => new Lone(1, 2).A; }")]
    [InlineData("public class User { internal readonly record struct Lone(int A, int B); } public class Other { int M() => new User.Lone(1, 2).A; }")]
    [InlineData("internal class User { private int[] _items = [1]; public Lone GetEnumerator() => new(_items); public struct Lone(int[] items) { private int _at = -1; public int Current => items[_at]; public bool MoveNext() => ++_at < items.Length; } } public class Other { int M(User u) { int s = 0; foreach (int i in u) s += i; return s; } }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private Lone? Make() => new Lone(1, 2); int M() => Make() is var (a, b) ? a + b : 0; }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private Lone Make() => new Lone(1, 2); int M() { var (a, b) = Make(); return a + b; } }")]
    [InlineData("public class User { private readonly record struct Lone(int A, int B); private Lone Make() => new Lone(1, 2); int M() => Make() is (1, var b) ? b : 0; }")]
    [InlineData("public static class User { [System.Runtime.CompilerServices.InterpolatedStringHandler] internal ref struct Lone { private System.Text.StringBuilder _text; "
        + "public Lone(int literalLength, int formattedCount) => _text = new(literalLength); public void AppendLiteral(string literal) => _text.Append(literal); "
        + "public void AppendFormatted<T>(T value) => _text.Append(value); internal string Text() => _text.ToString(); } "
        + "internal static string Of(ref Lone text) => text.Text(); } public class Other { string M(int x) => User.Of($\"a{x}\"); }")]
    public void AValueItsUserHandsOn_ADispatchedPart_ATypeOfANamespace_AndAHandlerTheCompilerBuildsForOthers_AreNotFound(string source) =>
        Assert.DoesNotContain(PartsAUserCouldHold(TypeUsers.Of([Sample(source)])), use => use.Type.EndsWith("Lone", StringComparison.Ordinal));

    /// <summary>The types of <paramref name="uses"/> nested in their one user that the user could hold itself: a static one, and one
    /// that holds a value fewer than two members of the user read.</summary>
    private static List<TypeUse> PartsAUserCouldHold(IReadOnlyList<TypeUse> uses) =>
    [
        .. uses.Where(use => use.Users.Count == 1 && use.Type.StartsWith(use.Users[0] + ".", StringComparison.Ordinal) && use.Shape switch
        {
            TypeShape.Owned or TypeShape.Extension => true,
            TypeShape.Wrapper or TypeShape.Value or TypeShape.Enum => use.Readers.Count < 2,
            _ => false,
        }),
    ];

    private static bool IsKept(string type) =>
        Kept.Any(kept => kept.Type == type) || KeptKinds.Any(kind => Regex.IsMatch(type, kind.Pattern));

    [Theory]
    [InlineData("internal sealed class Lone { } public class User { private Lone _lone = new(); }")]
    [InlineData("internal sealed class Lone { public int N => 1; } public class User { internal int Read(object o) => ((Lone)o).N; }")]
    [InlineData("internal static class Lone { extension(int value) { public int Twice => value * 2; } } public class User { public int M(int x) => x.Twice; }")]
    [InlineData("internal static class Lone { public static int Of(int x) => x; } public static class User { public static int M() => Lone.Of(2); }")]
    [InlineData("internal readonly record struct Lone(int A, int B); public class User { internal Lone Make() => new(1, 2); }")]
    [InlineData("public class Outer { internal sealed class Lone { } } public class User { private Outer.Lone? _lone; }")]
    [InlineData("internal static class Lone { extension(int value) { public int Twice => value * 2; } } public class User { public int M(Unbound u) => u.Twice; }")]
    [InlineData("internal static class Lone { extension(int value) { public int Twice() => value * 2; } } public class User { public int M(Unbound u) => u.Twice(); }")]
    [InlineData("internal static class Lone { extension(int value) { public int Twice() => value * 2; } } public class User : Unbound { public int M() => Queue.Twice(); }")]
    public void AnInternalTypeOneOtherTypeUses_IsFound(string source) =>
        Assert.Contains("Any.Lone", SingleUser(TypeUsers.Of([Sample(source)])).Select(use => use.Type).Select(type => type.Replace("Outer.", "", StringComparison.Ordinal)));

    [Theory]
    [InlineData("internal sealed class Lone { } public class A { private Lone? _a; } public class B { private Lone? _b; }")]
    [InlineData("public class User { private sealed class Lone { } private Lone _lone = new(); }")]
    [InlineData("public class User { internal sealed class Lone { } private Lone _lone = new(); }")]
    [InlineData("public sealed class Lone { } public class User { private Lone _lone = new(); }")]
    [InlineData("internal abstract class Shape { public abstract int Sides { get; } public static Shape Square => new Lone(); } "
        + "internal sealed class Lone : Shape { public override int Sides => 4; } "
        + "public class A { internal int M() => Shape.Square.Sides; } public class B { internal int M() => Shape.Square.Sides; }")]
    [InlineData("internal interface IShape { int Sides { get; } } internal sealed class Lone : IShape { public int Sides => 4; } "
        + "public class A { internal int M(IShape s) => s.Sides; } public class B { internal IShape Make() => new Lone(); }")]
    [InlineData("internal sealed class Lone { }")]
    public void ATypeOfTwoUsers_ANestedPart_APublicType_AndALeafOfASharedBase_AreNotFound(string source) =>
        Assert.DoesNotContain(SingleUser(TypeUsers.Of([Sample(source)])), use => use.Type.EndsWith("Lone", StringComparison.Ordinal));

    /// <summary>The internal types of <paramref name="uses"/> that exactly one type uses, which is not the type they are declared in.</summary>
    private static List<TypeUse> SingleUser(IReadOnlyList<TypeUse> uses) =>
        [.. uses.Where(use => use.Users.Count == 1 && !use.Type.StartsWith(use.Users[0] + ".", StringComparison.Ordinal))];

    private static string Describe(TypeUse use) => $"{use.Type} ({use.File}) is used by {use.Users[0]} only";

    private static Compilation Sample(string source) =>
        SourceCompilation.Create("Any", [new("Src/Any/Any.cs", "namespace Any; " + source)], SourceCompilation.Framework);
}
