// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The server is started by a host, never by code that could call <c>SkiaFonts.Register</c>, so its font
/// door is an environment variable: files and directories, separated the way PATH is. These facts cover what the
/// variable expands to, and that a wrong entry is reported instead of stopping the server.</summary>
public sealed class FontDirectoryTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("mpl-fonts-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string File(string name, string? subdirectory = null)
    {
        string dir = subdirectory is null ? _root : Path.Combine(_root, subdirectory);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        System.IO.File.WriteAllBytes(path, [0]);
        return path;
    }

    [Fact]
    public void Expand_Nothing_IsEmpty()
    {
        var expanded = FontDirectory.Expand(null);

        Assert.Empty(expanded.Files);
        Assert.Empty(expanded.Problems);
    }

    [Fact]
    public void Expand_Blank_IsEmpty()
    {
        var expanded = FontDirectory.Expand("  ;  ");

        Assert.Empty(expanded.Files);
        Assert.Empty(expanded.Problems);
    }

    [Fact]
    public void Expand_AFile_IsThatFile()
    {
        string font = File("Noto.ttf");

        var expanded = FontDirectory.Expand(font);

        Assert.Equal([font], expanded.Files);
        Assert.Empty(expanded.Problems);
    }

    [Fact]
    public void Expand_ADirectory_IsEveryFontFileInIt_InNameOrder()
    {
        string b = File("b.otf", "fonts");
        string a = File("a.ttf", "fonts");
        File("readme.txt", "fonts");
        File("nested.ttf", Path.Combine("fonts", "deeper"));

        var expanded = FontDirectory.Expand(Path.Combine(_root, "fonts"));

        Assert.Equal([a, b], expanded.Files);
        Assert.Empty(expanded.Problems);
    }

    [Fact]
    public void Expand_SeveralEntries_AreSeparatedLikeAPath()
    {
        string font = File("Noto.ttf");
        string other = File("Other.ttf", "more");

        var expanded = FontDirectory.Expand($" {font} ; {Path.Combine(_root, "more")} ");

        Assert.Equal([font, other], expanded.Files);
    }

    [Fact]
    public void Expand_AMissingEntry_IsAProblem_NotAStop()
    {
        string font = File("Noto.ttf");
        string missing = Path.Combine(_root, "no-such-font.ttf");

        var expanded = FontDirectory.Expand($"{missing};{font}");

        Assert.Equal([font], expanded.Files);
        var problem = Assert.Single(expanded.Problems);
        Assert.Contains(missing, problem);
    }

    [Fact]
    public void Expand_AnEmptyDirectory_IsAProblem()
    {
        string empty = Path.Combine(_root, "empty");
        Directory.CreateDirectory(empty);

        var expanded = FontDirectory.Expand(empty);

        Assert.Empty(expanded.Files);
        Assert.Contains(expanded.Problems, p => p.Contains(empty, StringComparison.Ordinal));
    }

    [Fact]
    public void TheVariable_IsNamedAfterTheLibrary() =>
        Assert.Equal("MATPLOTLIBNET_FONTS", FontDirectory.Variable);
}
