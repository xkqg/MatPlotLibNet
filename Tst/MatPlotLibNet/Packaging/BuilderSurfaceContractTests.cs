// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The published surface of the two fluent builders is pinned, call by call: every method a caller writes on a builder
/// (<c>ax.SetXLabel("x")</c>), whether the class declares it or an extension block of the library adds it, keeps the name, the
/// parameters and the return type it shipped with, so every call a user wrote compiles unchanged. <c>AxesBuilder</c> declares the
/// builder itself (its constructor and adding a series made by hand), and each verb stands in the class of its concern
/// (<c>AxesBuilderXYVerbs</c> beside <c>AxesXYVerbs</c>, <c>AxesBuilderTickVerbs</c>, ...): a new verb is an extension block beside
/// the axes verb it forwards to, never one more member of the class. <c>FigureBuilder</c> declares the figure itself, and its verbs
/// stand in the class of their concern too (<c>FigureBuilderLayoutVerbs</c>, <c>FigureBuilderDecorationVerbs</c>, ...); its forwarders
/// to its single axes stand in <c>FigureBuilderDefaultAxesVerbs</c>, which takes no new one: a chart of one axes that needs a new verb
/// is built with <c>AddSubPlot</c>. The lists are held both ways: a call
/// that is not on the list and a listed call that is gone turn the test red. A call leaves the list only the day a release may break
/// it.</summary>
public class BuilderSurfaceContractTests
{
    /// <summary>The public members a type declares itself, one line each, in ordinal order: constructors, methods, properties and
    /// events, as reflection writes their signatures.</summary>
    private static IReadOnlyList<string> DeclaredOf(Type type) =>
    [
        .. type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(member => member is not MethodInfo { IsSpecialName: true })
            .Select(member => $"{member.MemberType} {member}")
            .Order(StringComparer.Ordinal),
    ];

    /// <summary>What a caller writes on a type, one line each, in ordinal order: the members it declares, its methods among them as
    /// <see cref="BuilderCallExtensions"/> finds them together with the extension members of the library on it, each written as the
    /// call reads, without the builder it is called on.</summary>
    private static IReadOnlyList<string> SurfaceOf(Type type) =>
    [
        .. DeclaredOf(type).Where(line => !line.StartsWith("Method ", StringComparison.Ordinal))
            .Concat(type.Calls().Select(call => call.Signature))
            .Order(StringComparer.Ordinal),
    ];

    private static string[] PinParts(Type type) => ["Tst", "MatPlotLibNet", "Packaging", "Pinned", type.Name + ".surface.txt"];

    private static string PinPath(Type type) => Path.Combine([RepositoryRoot.Folder, .. PinParts(type)]);

    private static IReadOnlyList<string> PinnedOf(Type type) =>
        File.Exists(PinPath(type)) ? [.. RepositoryTree.FileAt(PinParts(type)).Lines.Where(line => line.Length > 0)] : [];

    [Theory]
    [InlineData(typeof(AxesBuilder))]
    [InlineData(typeof(FigureBuilder))]
    public void ThePublishedSurfaceOfABuilder_IsThePinnedList(Type builder)
    {
        var surface = SurfaceOf(builder);
        var pinned = PinnedOf(builder);

        var added = surface.Except(pinned, StringComparer.Ordinal).ToList();
        var gone = pinned.Except(surface, StringComparer.Ordinal).ToList();

        Assert.True(added.Count == 0 && gone.Count == 0,
            $"The published calls of {builder.Name} are pinned in {PinPath(builder)}. A new verb is an extension block beside its axes verb, never a member of the class.\n"
            + $"Not on the list:\n{string.Join('\n', added)}\nOn the list and gone:\n{string.Join('\n', gone)}");
    }

    /// <summary>What each builder declares itself. The axes builder: its constructor, and adding a series made by hand, the one
    /// generic call, kept on the class so no extension overload is ever weighed against it. The figure builder: the figure itself,
    /// its canvas and document (size, resolution, theme, background, the texts that describe it), its subplots and its builds.</summary>
    public static TheoryData<Type, string[]> DeclaredItself { get; } = new()
    {
        { typeof(AxesBuilder), [
            "Constructor Void .ctor()",
            "Method MatPlotLibNet.AxesBuilder AddSeries[T](T, System.Action`1[T])"] },
        { typeof(FigureBuilder), [
            "Constructor Void .ctor()",
            "Method MatPlotLibNet.FigureBuilder AddSubPlot(Int32, Int32, Int32, System.Action`1[MatPlotLibNet.AxesBuilder], System.String)",
            "Method MatPlotLibNet.FigureBuilder AddSubPlot(MatPlotLibNet.Models.GridPosition, System.Action`1[MatPlotLibNet.AxesBuilder], System.String)",
            "Method MatPlotLibNet.FigureBuilder WithAltText(System.String)",
            "Method MatPlotLibNet.FigureBuilder WithAutoSize(MatPlotLibNet.Models.TreeNode)",
            "Method MatPlotLibNet.FigureBuilder WithBackground(MatPlotLibNet.Styling.Color)",
            "Method MatPlotLibNet.FigureBuilder WithDescription(System.String)",
            "Method MatPlotLibNet.FigureBuilder WithDpi(Double)",
            "Method MatPlotLibNet.FigureBuilder WithSize(Double, Double)",
            "Method MatPlotLibNet.FigureBuilder WithTheme(MatPlotLibNet.Styling.Theme)",
            "Method MatPlotLibNet.Models.Figure Build()",
            "Method MatPlotLibNet.Models.Streaming.StreamingFigure BuildStreaming(System.Nullable`1[System.TimeSpan])"] },
    };

    [Theory]
    [MemberData(nameof(DeclaredItself))]
    public void ABuilder_DeclaresTheBuilderItself_AndEveryVerbStandsInTheClassOfItsConcern(Type builder, string[] declared)
    {
        Assert.Equal(declared, DeclaredOf(builder));
    }

    [Fact]
    public void TheSurface_IsEveryPublicMemberTheTypeDeclares_AndNoAccessor()
    {
        var surface = SurfaceOf(typeof(Pinned));

        Assert.Equal(
        [
            "Constructor Void .ctor()",
            "Method Int32 Verb(Int32)",
            "Property Int32 Value",
        ], surface);
    }

    /// <summary>A type with one member of each kind the surface lists, and an accessor and a private member it leaves out.</summary>
    private sealed class Pinned
    {
        public int Value { get; set; }

        public int Verb(int value) => value + Hidden();

        private int Hidden() => Value;
    }
}
