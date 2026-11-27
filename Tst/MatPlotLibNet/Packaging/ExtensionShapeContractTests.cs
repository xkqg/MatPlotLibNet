// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>Shared behaviour on a value is written as a C# 14 <c>extension(T value) { }</c> block, in every package. The old
/// <c>static T M(this Type self, …)</c> shape does the same call but names the receiver once per method instead of once per
/// block, and the block is where the documentation says what the receiver is. The source scan holds that no package puts the
/// old shape back. A public static that never took <c>this</c> is not in the scan and cannot move: package validation reads
/// its replacement as a removal (CP0002), so it stays a plain static.</summary>
public class ExtensionShapeContractTests
{
    /// <summary>A static method whose parameter list opens with <c>this</c>, over several lines when the signature does.</summary>
    private static readonly Regex OldShape = new(@"static[^;{}=]*?\(\s*this\s", RegexOptions.CultureInvariant);

    [Fact]
    public void NoPackage_WritesAnExtensionAsAStaticMethodThatTakesThis()
    {
        var oldShape = new List<string>();

        foreach (var file in RepositoryTree.SourceFiles())
        {
            // The code of the file leaves its comment lines empty, so a summary that names the old shape is not code.
            foreach (Match match in OldShape.Matches(file.Code))
            {
                int line = SourceFile.LineIndexAt(file.Code, match.Index);
                oldShape.Add($"{file.Path}:{line + 1}: {file.Lines[line].Trim()}");
            }
        }

        Assert.True(oldShape.Count == 0,
            $"{oldShape.Count} extension member(s) are written as 'static … (this T …)' instead of an extension block:\n" + string.Join("\n", oldShape));
    }

    [Fact]
    public void TheScan_FindsTheOldShapeWhereItIsWritten()
    {
        const string written = "public static class Foo\n{\n    public static int Bar(\n        this string s,\n        int n) => n;\n}";

        Assert.Single(OldShape.Matches(written));
        Assert.Empty(OldShape.Matches("public static class Foo\n{\n    extension(string s)\n    {\n        public int Bar(int n) => n;\n    }\n}"));
    }
}
