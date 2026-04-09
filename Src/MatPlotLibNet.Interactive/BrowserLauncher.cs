// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace MatPlotLibNet.Interactive;

/// <summary>Default <see cref="IBrowserLauncher"/> that opens URLs via the OS shell.</summary>
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
