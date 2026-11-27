// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="Figure"/> export methods.</summary>
public class IntuitiveExportTests
{
    /// <summary>Verifies that SaveSvg creates a valid SVG file on disk.</summary>
    [Fact]
    public void SaveSvg_CreatesFile()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");
        try
        {
            figure.SaveSvg(path);
            Assert.True(File.Exists(path));
            Assert.StartsWith("<svg", File.ReadAllText(path).TrimStart());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
