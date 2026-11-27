// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>Reconstructs an axes, or a secondary-axis series, from its DTO — every entry point past
/// <see cref="AxesDto.RestoreOnto"/> itself that needs the <see cref="Axes"/> it is restoring onto.</summary>
internal static class AxesSerializationExtensions
{
    extension(Axes target)
    {
        /// <summary>Makes the series <paramref name="dto"/> describes, of whatever registered kind, on this axes' own series
        /// list (<see cref="SeriesRegistry.Create"/> always lands its series there: every factory calls its own primary verb,
        /// e.g. <c>axes.Plot</c>). A kind this build does not know (old JSON naming a kind that is gone, or a hand-made or
        /// corrupt payload) is still dropped leniently, so the figure keeps loading, but the drop is announced on
        /// <see cref="ChartDiagnostics"/> instead of being silent. The one owner of that report: the primary series list and
        /// every secondary seat read through it.</summary>
        /// <param name="dto">The series to make; a missing discriminator is reported as <c>unknown</c>. A kind that is known but
        /// whose data its reader cannot make a series of is reported as unreadable, with the reason the reader gives.</param>
        /// <returns>The new series, or <see langword="null"/> for an unknown discriminator or unreadable data.</returns>
        internal ISeries? CreateFromDtoOrReport(SeriesDto dto)
        {
            var type = dto.Type ?? "unknown";
            var read = SeriesRegistry.Read(type, target, dto);
            if (read?.Series is { } created) return created;

            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ChartSerializer),
                read is { Why: var why }
                    ? $"The '{type}' series was skipped during deserialization: its data is unreadable: {why}."
                    : $"Unknown series discriminator '{type}' was skipped during deserialization.",
                null));
            return null;
        }

        /// <summary>Reconstructs ANY registered series kind from <paramref name="dto"/> and seats it where
        /// <paramref name="reseat"/> says — the row-99 fix (ROADMAP.md: "the right-hand and top axes carry lines
        /// and points; a bar or an area there is not kept"). The series is made as <see cref="CreateFromDtoOrReport"/>
        /// makes it, onto <c>target.Series</c>, and then the just-created one is moved, instead of rewriting every factory to
        /// take a seat parameter. An unknown kind is reported the same way, so a secondary-axis series no longer vanishes
        /// silently either.</summary>
        /// <param name="dto">The series to reconstruct.</param>
        /// <param name="reseat">Where the series belongs once built — <c>axes.AddXSecondarySeries</c>, or a Y axis
        /// slot's own <see cref="YAxisSlot.AddSeries{TSeries}"/>.</param>
        /// <returns>The reconstructed series, or <see langword="null"/> for an unknown discriminator.</returns>
        internal ISeries? CreateOnSecondarySeat(SeriesDto dto, Action<ISeries> reseat)
        {
            if (target.CreateFromDtoOrReport(dto) is not { } created)
                return null;

            target.TakeLastSeries();
            reseat(created);
            created.RestoreSharedFields(dto);
            return created;
        }

        /// <summary>Adds the series <paramref name="dto"/> describes to this axes' own series list, with the fields every
        /// series shares (<see cref="SeriesSharedFieldsExtensions"/>); a kind this build does not know is reported and
        /// skipped (<see cref="CreateFromDtoOrReport"/>).</summary>
        /// <param name="dto">The series to add.</param>
        internal void AddSeriesFromDto(SeriesDto dto)
        {
            if (target.CreateFromDtoOrReport(dto) is { } series)
                series.RestoreSharedFields(dto);
        }
    }
}
