// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Per-axes stack of axis limit snapshots for back/forward navigation.
/// Push is called after zoom/pan/rectangle-zoom; Back/Forward restore previous views.</summary>
public sealed class ViewHistoryManager
{
    private const int MaxHistory = 50;

    private readonly List<DataRange> _history = [];
    private int _position = -1;

    /// <summary>Whether a previous view exists to go back to.</summary>
    public bool CanGoBack => _position > 0;

    /// <summary>Whether a forward view exists (after going back).</summary>
    public bool CanGoForward => _position < _history.Count - 1;

    /// <summary>Number of entries in the history.</summary>
    public int Count => _history.Count;

    /// <summary>Pushes a new axis limit snapshot. Clears any forward history beyond the current position.</summary>
    public void Push(double xMin, double xMax, double yMin, double yMax)
    {
        // Truncate forward history
        if (_position < _history.Count - 1)
            _history.RemoveRange(_position + 1, _history.Count - _position - 1);

        _history.Add(new DataRange(xMin, xMax, yMin, yMax));

        // Cap history size
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
        }

        _position = _history.Count - 1;
    }

    /// <summary>Returns the previous view, or <c>null</c> if at the beginning.</summary>
    public DataRange? Back()
    {
        if (!CanGoBack) return null;
        _position--;
        return _history[_position];
    }

    /// <summary>Returns the next view (after going back), or <c>null</c> if at the end.</summary>
    public DataRange? Forward()
    {
        if (!CanGoForward) return null;
        _position++;
        return _history[_position];
    }
}
