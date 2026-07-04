// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Uno.Tests;

/// <summary>B3 (council main@a5fe781) — <see cref="MatPlotLibNet.Uno.MplStreamingChartElement"/>
/// subscribed to <c>StreamingFigure.RenderRequested</c> on property change but never detached when
/// the element itself was unloaded (only on a subsequent StreamingFigure property swap). That
/// leaked the element via the figure's event (figure outlives element) and kept invalidation
/// callbacks firing into a dead element.
/// <para>
/// Unlike <c>MplChartElementTests</c>'s adjacent adapter-type tests, this bug's fix lives entirely
/// inside a class derived from Uno's <c>SKCanvasElement</c> (<c>Uno.WinUI.Graphics2DSK</c>), which
/// — like <c>MplChartElement</c> — has no pre-compiled DLL for the <c>net10.0-windows*</c> test
/// TFM: attempting to add <c>MplStreamingChartElement.cs</c> to this project (verified empirically
/// before writing this test) fails with CS0012 (<c>Uno.UI.FrameworkElement</c> not referenced),
/// even after adding the <c>Uno.WinUI.Graphics2DSK</c> package — the type requires the full Uno
/// multi-target build pipeline (Uno.Sdk head projects), not available to a plain test csproj.
/// So the element cannot be instantiated here; this is a source-level regression guard instead,
/// mirroring the established <c>PlaygroundNewTabTests</c> "read the file, assert the pattern"
/// idiom used elsewhere in this repo for the same class of untestable-by-instantiation code.
/// </para></summary>
public class MplStreamingChartElementSourceTests
{
    private static string SourcePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md"))
               && !File.Exists(Path.Combine(dir.FullName, "MatPlotLibNet.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        var path = Path.Combine(dir!.FullName,
            "Src", "MatPlotLibNet.Uno", "MplStreamingChartElement.cs");
        Assert.True(File.Exists(path), $"MplStreamingChartElement.cs not found at expected path: {path}");
        return path;
    }

    [Fact]
    public void Uno_OnUnload_UnsubscribesRenderRequested()
    {
        // Regression guard: if the Unloaded wiring or the Unsubscribe() call within it is ever
        // reverted, this test fails the build the moment that happens.
        string source = File.ReadAllText(SourcePath());

        Assert.Contains("Unloaded += OnUnloaded", source);
        Assert.Contains("private void OnUnloaded", source);

        // The unload handler must route through the same Unsubscribe() used by the
        // StreamingFigure property-changed path (mirrors the Avalonia exemplar's
        // Subscribe/Unsubscribe pair, reused from both OnPropertyChanged and
        // OnDetachedFromVisualTree).
        int unloadedIdx = source.IndexOf("private void OnUnloaded", StringComparison.Ordinal);
        Assert.True(unloadedIdx >= 0);
        string afterUnloaded = source[unloadedIdx..];
        Assert.Contains("Unsubscribe()", afterUnloaded);

        // The Unsubscribe() method itself must detach from RenderRequested and clear the field
        // so the figure no longer holds a live reference to this element.
        int unsubscribeIdx = source.IndexOf("private void Unsubscribe()", StringComparison.Ordinal);
        Assert.True(unsubscribeIdx >= 0, "Unsubscribe() method not found");
        string unsubscribeBody = source[unsubscribeIdx..];
        Assert.Contains("RenderRequested -= OnRenderRequested", unsubscribeBody);
        Assert.Contains("_subscribedFigure = null", unsubscribeBody);
    }

    [Fact]
    public void StreamingFigureChange_StillUnsubscribesOldFigure()
    {
        // Figure-swap case (pre-existing, must survive the refactor): OnStreamingFigureChanged
        // must still detach the OLD figure before attaching the new one.
        string source = File.ReadAllText(SourcePath());
        int changedIdx = source.IndexOf("OnStreamingFigureChanged(StreamingFigure", StringComparison.Ordinal);
        Assert.True(changedIdx >= 0, "OnStreamingFigureChanged method not found");
        string changedBody = source[changedIdx..];
        Assert.Contains("Unsubscribe()", changedBody);
        Assert.Contains("Subscribe(newValue)", changedBody);
    }
}
