// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Samples.ControlRoom.Services;

/// <summary>One stateful alarm in the simulated federation: a key, a human title, and where it stands in the
/// lifecycle an operator drives.</summary>
/// <param name="Key">The alarm's identity — one alarm per key, however often the condition re-fires.</param>
/// <param name="Title">What an operator reads in the list.</param>
/// <param name="State">Where it stands: firing, or acknowledged.</param>
public readonly record struct SimAlarm(string Key, string Title, AlarmState State);

/// <summary>The two states an OPEN alarm can hold. Resolved is deliberately not here: an alarm whose condition
/// cleared leaves the book — resolved is history, not load.</summary>
public enum AlarmState
{
    /// <summary>Raised and unseen.</summary>
    Firing = 0,

    /// <summary>An operator saw it. Seen is not gone: it stays counted until the condition clears.</summary>
    Acked = 1,
}

/// <summary>
/// The alarm lifecycle, sample-sized — the same Firing → Acked → gone-when-resolved shape the real wall's
/// registry drives, so the descent's ack gesture can be demonstrated where the library's reference
/// implementation lives.
///
/// <para><b>Everything in this file is the OBSERVABILITY layer, not the charting library</b> — the same rule
/// the rest of <c>Services/</c> states: what counts as an alarm is a judgement about a domain, and a chart
/// library that made it would be holding an opinion it has no business holding.</para>
///
/// <para><b>Why Acked stays counted.</b> Acking is the only verb an operator has here, and if it removed the
/// alarm from the wall, the operator's own clicks would make the screen look better while the services are
/// still down — a sedative, not a wall. Acked alarms stay visible beside the firing count until their
/// condition actually clears.</para>
/// </summary>
public sealed class AlarmBook
{
    // Concurrent BY NEED, not by habit: Observe ticks on the simulator's background service while Ack arrives
    // on the Blazor circuit thread — two writers, no lock (a demo people copy must not carry a torn map).
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SimAlarm> _open = new();

    /// <summary>Alarms firing right now — the number on the tile, and exactly what the ack panel lists.</summary>
    public int Firing => _open.Values.Count(a => a.State == AlarmState.Firing);

    /// <summary>Alarms an operator acknowledged whose condition has not cleared — seen, not gone.</summary>
    public int Acked => _open.Values.Count(a => a.State == AlarmState.Acked);

    /// <summary>The open alarms, firing first, for the panel the Alarms tile opens onto.</summary>
    public IReadOnlyList<SimAlarm> Snapshot() =>
        [.. _open.Values.OrderBy(a => a.State).ThenBy(a => a.Key, StringComparer.Ordinal)];

    /// <summary>Feed one condition's current truth. Active raises (or keeps) the alarm; inactive resolves it —
    /// the condition owns resolution, the operator only owns acknowledgement.</summary>
    public void Observe(string key, string title, bool active)
    {
        if (!active)
        {
            _open.TryRemove(key, out _);
            return;
        }
        _open.TryAdd(key, new SimAlarm(key, title, AlarmState.Firing));
    }

    /// <summary>The operator saw it: Firing → Acked. It stays on the wall until the condition clears.</summary>
    public void Ack(string key)
    {
        if (_open.TryGetValue(key, out var alarm) && alarm.State == AlarmState.Firing)
        {
            // TryUpdate, not the indexer: an ack racing the condition's clear must LOSE — a plain write here
            // would resurrect an alarm the simulator just resolved.
            _open.TryUpdate(key, alarm with { State = AlarmState.Acked }, alarm);
        }
    }
}
