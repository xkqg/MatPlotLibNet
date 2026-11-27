// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The trees of the math texts a figure's draws read, kept by the figure (<see cref="Models.Figure.MathTexts"/>) from one draw
/// to the next, so a label drawn again is not parsed again: the trees this draw has read, and the ones the draw before read, which a
/// read takes over. A text no draw reads any more (a title changed, a tick label gone) is let go of after one draw, so the store holds
/// the texts of two draws at most. The drawing sequence tells it to each surface with the looks (<see cref="IThemedRenderContext"/>),
/// and a label reads it when it is parsed (<see cref="TextLabel.Parsed"/>), the one place a text is parsed. Draws of one figure on
/// several threads share it under its lock.</summary>
internal sealed class KeptMathTexts
{
    private readonly object _gate = new();
    private Dictionary<string, LineBox> _read = new(StringComparer.Ordinal);
    private Dictionary<string, LineBox> _before = new(StringComparer.Ordinal);

    /// <summary>Starts a draw: the texts the draw that ended read are the ones this draw may take over; any other is let go of.</summary>
    internal void StartDraw()
    {
        lock (_gate)
        {
            (_before, _read) = (_read, _before);
            _read.Clear();
        }
    }

    /// <summary>The tree of <paramref name="text"/> when this draw or the draw before read it; one the draw before read is now this
    /// draw's.</summary>
    /// <param name="text">The text as written.</param>
    /// <param name="tree">Its tree.</param>
    /// <returns>Whether the store holds it.</returns>
    internal bool TryTake(string text, [NotNullWhen(true)] out LineBox? tree)
    {
        lock (_gate)
        {
            if (_read.TryGetValue(text, out tree)) return true;
            if (!_before.Remove(text, out tree)) return false;
            _read[text] = tree;
            return true;
        }
    }

    /// <summary>Keeps <paramref name="tree"/>, the parse of <paramref name="text"/>, as read by this draw. When a draw on another thread
    /// kept one first, that one stays and is the one returned, so every reader reads one tree.</summary>
    /// <param name="text">The text as written.</param>
    /// <param name="tree">Its tree.</param>
    /// <returns>The tree the store holds for the text.</returns>
    internal LineBox Keep(string text, LineBox tree)
    {
        lock (_gate)
        {
            return _read.TryAdd(text, tree) ? tree : _read[text];
        }
    }
}
