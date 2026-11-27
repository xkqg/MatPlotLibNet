// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The one <typeparamref name="T"/> a thread keeps between two uses of it: the memory a parse writes on before it knows how
/// long a row or a run of text is (<see cref="Rendering.MathText.MathTextCursor.MathParseScratch"/>), and the writer of a label's flat view (<see cref="MathSpanWriter"/>).
/// A use takes it (<see cref="Take"/>), so a second use on the same thread before it is handed back gets one of its own, and hands it
/// back emptied (<see cref="Return"/>); a use that fails hands nothing back, and the next makes a new one. Nothing of a label outlives
/// its use here: what is kept is empty.</summary>
/// <typeparam name="T">What is kept.</typeparam>
internal static class KeptByThread<T>
    where T : class, new()
{
    [ThreadStatic]
    private static T? _kept;

    /// <summary>The one this thread keeps, which it keeps no longer; a new one when it keeps none.</summary>
    internal static T Take()
    {
        var kept = _kept ?? new T();
        _kept = null;
        return kept;
    }

    /// <summary>Hands <paramref name="emptied"/> back for the thread's next use.</summary>
    /// <param name="emptied">The one taken, holding nothing of the use that ends.</param>
    internal static void Return(T emptied) => _kept = emptied;
}
