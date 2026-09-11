// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A model chooses the path this server writes to, and the library's writer truncates whatever it is
/// pointed at. So the path is resolved against one root the operator names, and anything that climbs out of it —
/// a traversal, an absolute path elsewhere, a drive letter — is refused rather than written. Refusing costs a
/// retry; a truncated file costs the file.</summary>
public class OutputPathResolverTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("mcp-path-tests");

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private OutputPathResolver Resolver => new(_root.FullName);

    [Fact]
    public void APlainName_LandsInTheOutputRoot() =>
        Assert.Equal(Path.Combine(_root.FullName, "chart.png"), Resolver.Resolve("chart.png"));

    [Fact]
    public void ASubfolder_IsCreatedUnderTheRoot()
    {
        string resolved = Resolver.Resolve("reports/q3/chart.png");

        Assert.StartsWith(_root.FullName, resolved, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.GetDirectoryName(resolved)));
    }

    [Fact]
    public void AnAbsolutePathInsideTheRoot_IsAccepted()
    {
        string inside = Path.Combine(_root.FullName, "inside.png");

        Assert.Equal(inside, Resolver.Resolve(inside));
    }

    [Theory]
    [InlineData("../escape.png")]
    [InlineData("reports/../../escape.png")]
    [InlineData("a/b/../../../escape.png")]
    public void APathThatClimbsOutOfTheRoot_IsRefused(string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Resolver.Resolve(path));

        Assert.Contains(_root.FullName, refusal.Message);
    }

    [Fact]
    public void AnAbsolutePathElsewhere_IsRefused()
    {
        string elsewhere = Path.Combine(Path.GetTempPath(), "not-under-the-root.png");

        var refusal = Assert.Throws<ToolRefusalException>(() => Resolver.Resolve(elsewhere));

        Assert.Contains(_root.FullName, refusal.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyPath_IsRefused(string path) =>
        Assert.Throws<ToolRefusalException>(() => Resolver.Resolve(path));

    [Fact]
    public void TheRootComesFromTheEnvironment_OrTheTempDirectory()
    {
        Assert.Equal(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),
            OutputPathResolver.FromEnvironment(null).Root.TrimEnd(Path.DirectorySeparatorChar));
        Assert.Equal(_root.FullName, OutputPathResolver.FromEnvironment(_root.FullName).Root);
    }
}
