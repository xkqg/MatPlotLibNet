// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MatPlotLibNet.Interactive;

/// <summary>Default <see cref="IBrowserLauncher"/> that opens URLs via the OS shell.</summary>
/// <remarks>Excluded from coverage: calls <see cref="Process.Start(ProcessStartInfo)"/> with
/// <c>UseShellExecute=true</c> — the launch contract can only be tested end-to-end with a
/// real desktop environment. CI has the <c>ShowAsync_OpensBrowser</c> integration test SKIPped
/// for this reason. Mocking <c>Process.Start</c> here would test the mock, not the launcher.</remarks>
[ExcludeFromCodeCoverage]
public sealed class BrowserLauncher : IBrowserLauncher
{
    /// <inheritdoc/>
    public Task OpenAsync(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }
}
