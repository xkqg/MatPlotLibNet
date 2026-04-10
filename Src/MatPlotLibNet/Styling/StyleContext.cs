// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Scoped override of <see cref="RcParams.Current"/>.
/// Uses <see langword="using"/> to restore the previous scope on disposal.
/// Because <see cref="RcParams.ScopeValue"/> uses <c>AsyncLocal&lt;T&gt;</c>, the override
/// flows correctly across <c>await</c> boundaries within the same logical call chain.
/// </summary>
public sealed class StyleContext : IDisposable
{
    private readonly RcParams _previous;

    /// <summary>
    /// Pushes a new scope that applies the parameters from <paramref name="sheet"/> on top of the current scope.
    /// </summary>
    public StyleContext(StyleSheet sheet)
    {
        _previous = RcParams.Current;

        // Build a clone of the current scope, then apply the sheet's overrides
        var scoped = _previous.Clone();
        foreach (var kv in sheet.Parameters)
            scoped.Set(kv.Key, kv.Value);

        RcParams.ScopeValue = scoped;
    }

    /// <summary>Restores the previous <see cref="RcParams.Current"/>.</summary>
    public void Dispose() => RcParams.ScopeValue = _previous == RcParams.Default ? null : _previous;
}
