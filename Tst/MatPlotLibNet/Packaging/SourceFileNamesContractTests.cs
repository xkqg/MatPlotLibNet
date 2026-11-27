// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A file of the library is named after a type it declares, so a reader who knows a type finds its file by the name, and a
/// file that holds several types is found by one of them (<see cref="SourceFileNames"/> reads the declarations).</summary>
public class SourceFileNamesContractTests
{
    private static IReadOnlyList<string> Read(string path, string text) => SourceFileNames.Unnamed([new SourceFile(path, text)]);

    [Fact]
    public void EveryFileOfSrc_IsNamedAfterATypeItDeclares()
    {
        var findings = SourceFileNames.Unnamed(RepositoryTree.SourceFiles());

        Assert.True(findings.Count == 0, $"{findings.Count} files are named after no type they declare:\n" + string.Join('\n', findings));
    }

    /// <summary>Each row is one way a file stood under a name that is no type of it: a name no type of the file has (a file of peers named
    /// for none of them), a type that is nested in another and so no type of the file's own, and a file that declares no type and holds
    /// more than the attributes of its assembly.</summary>
    [Theory]
    [InlineData("Src/Lib/Peers.cs", "namespace Lib;\npublic sealed class First { }\npublic sealed class Second { }")]
    [InlineData("Src/Lib/Outer.cs", "namespace Lib { public sealed class Holder { internal sealed class Outer { } } }")]
    [InlineData("Src/Lib/Usings.cs", "global using System;")]
    [InlineData("Src/Lib/Mixed.cs", "[assembly: System.CLSCompliant(true)]\nnamespace Lib;")]
    public void AFileNamedAfterNoTypeItDeclares_IsFound(string path, string text) => Assert.Single(Read(path, text));

    /// <summary>What the rule allows, one row for each way the reader finds a name: a delegate (every other kind of type is read by the
    /// same node), a generic type by its name without the arity, a generated half of a type named by the type (the name is read up to its
    /// first dot) in a block namespace, a type in no namespace, one of several types of the file, and the attributes of an assembly in a
    /// file with no type.</summary>
    [Theory]
    [InlineData("Src/Lib/Handler.cs", "namespace Lib;\npublic delegate void Handler();")]
    [InlineData("Src/Lib/Box.cs", "namespace Lib;\ninternal sealed class Box<T> { }")]
    [InlineData("Src/Lib/Table.g.cs", "namespace Lib { internal static partial class Table { } }")]
    [InlineData("Src/Lib/Main.cs", "namespace Lib;\npublic sealed class Other { }\npublic sealed class Main { }")]
    [InlineData("Src/Lib/Bare.cs", "public sealed class Bare { }")]
    [InlineData("Src/Lib/Properties/AssemblyAttributes.cs", "using System.Runtime.CompilerServices;\n[assembly: InternalsVisibleTo(\"Lib.Tests\")]")]
    public void AFileNamedAfterATypeItDeclares_IsNotFound(string path, string text) => Assert.Empty(Read(path, text));
}
