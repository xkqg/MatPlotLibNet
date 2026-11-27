// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The silent surface every test double of <see cref="IRenderContext"/> derives from declares exactly the members of the interface
/// that have no default body, so the interface's own defaults still run through it.</summary>
public sealed class SilentRenderContextTests
{
    private static string Signature(MethodInfo method) =>
        $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

    [Fact]
    public void TheBase_DeclaresTheMembersNoDefaultBodyCovers_AndNoMemberTheInterfaceGivesABody()
    {
        var withoutBody = typeof(IRenderContext).GetMethods().Where(m => m.IsAbstract).Select(Signature).Order().ToList();
        var declared = typeof(SilentRenderContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(Signature).Order().ToList();

        Assert.Equal(12, withoutBody.Count);
        Assert.Equal(withoutBody, declared);
    }

    private sealed class TextNoter : SilentRenderContext
    {
        public List<string> Texts { get; } = [];

        public override void DrawText(string text, Point position, Font font, TextAlignment alignment) => Texts.Add(text);
    }

    [Fact]
    public void ATextDrawnTurned_ReachesThePlainDrawTextOfALeaf_ThroughTheInterfaceDefault()
    {
        var leaf = new TextNoter();
        IRenderContext surface = leaf;

        surface.DrawText("turned", new Point(1, 2), new Font(), TextAlignment.Left, 90);

        Assert.Equal(["turned"], leaf.Texts);
    }

}
