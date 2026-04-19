// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>Phase Y.7 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="InteractiveExtensions.Browser"/> setter's null-arg guard
/// (line 17, was 50%B). Show/ShowAsync depend on
/// `BrowserLauncher` actually opening a browser, which is exempt
/// (`MatPlotLibNet.Interactive.BrowserLauncher` thresholds.json entry — pre-X
/// since the launcher does Process.Start with UseShellExecute and can't be
/// unit-tested without mocking System.Diagnostics.Process).</summary>
public class InteractiveExtensionsBranchTests
{
    /// <summary>Browser setter line 17 — `value ?? throw` true arm: passing null
    /// must throw ArgumentNullException.</summary>
    [Fact]
    public void Browser_Setter_Null_ThrowsArgumentNullException()
    {
        var original = InteractiveExtensions.Browser;
        try
        {
            Assert.Throws<ArgumentNullException>(() => InteractiveExtensions.Browser = null!);
        }
        finally
        {
            InteractiveExtensions.Browser = original;
        }
    }

    /// <summary>Browser setter line 17 — false arm: passing a non-null instance
    /// stores it (already covered by existing tests; forward-regression guard).</summary>
    [Fact]
    public void Browser_Setter_NonNull_StoresInstance()
    {
        var original = InteractiveExtensions.Browser;
        try
        {
            var custom = new BrowserLauncher();
            InteractiveExtensions.Browser = custom;
            Assert.Same(custom, InteractiveExtensions.Browser);
        }
        finally
        {
            InteractiveExtensions.Browser = original;
        }
    }
}
