// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>
/// A dotnet tool ships its whole dependency closure, so what the project references decides what a user downloads.
/// SkiaSharp's native assets carry a debug symbol file per architecture, and those symbols measured 247 MB of a
/// 301 MB payload — an 86 MB download to draw a line chart. The project drops them on both the build and the
/// publish side; this pins that, because the failure mode is silent: the package simply gets fat again.
/// </summary>
public class ToolPackagingTests
{
    private static DirectoryInfo ServerOutput()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var bin = new DirectoryInfo(Path.Combine(dir!.FullName, "Src", "MatPlotLibNet.Mcp", "bin"));
        Assert.True(bin.Exists, $"The server project has not been built: {bin.FullName}");
        return bin;
    }

    private static FileInfo[] NativeFiles(string extension) =>
        [.. ServerOutput().EnumerateFiles($"*{extension}", SearchOption.AllDirectories)
            .Where(f => f.FullName.Replace('\\', '/').Contains("/runtimes/", StringComparison.Ordinal))];

    [Theory]
    [InlineData(".dll", "win")]
    [InlineData(".so", "linux")]
    [InlineData(".dylib", "osx")]
    public void TheServerCarriesItsNativeRenderer_ForEveryPlatformItMayLandOn(string extension, string platform)
    {
        // The managed SkiaSharp package carries no binary: each platform's comes from its own RID package. A
        // library may leave that to whoever hosts it; a TOOL is hosted by nobody — it is resolved and started on
        // a machine nobody asked about. Missing the Linux one is how the first CI run of this package failed,
        // with "Unable to load shared library 'libSkiaSharp'", on a suite that was green on Windows.
        var native = NativeFiles(extension)
            .Where(f => f.Name.Contains("SkiaSharp", StringComparison.OrdinalIgnoreCase)
                        && f.FullName.Replace(Path.DirectorySeparatorChar, '/').Contains($"/runtimes/{platform}", StringComparison.Ordinal))
            .ToArray();

        Assert.True(native.Length > 0, $"No libSkiaSharp for {platform}: every render on that platform would die.");
    }

    [Fact]
    public void TheServerCarriesNoNativeDebugSymbols()
    {
        var symbols = NativeFiles(".pdb");

        Assert.True(symbols.Length == 0,
            "Native symbols are back in the tool payload: "
            + string.Join(", ", symbols.Select(f => $"{f.Name} ({f.Length / (1024 * 1024)} MB)")));
    }
}
