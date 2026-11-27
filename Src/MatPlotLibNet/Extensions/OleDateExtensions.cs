// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet;

/// <summary>The one rule for "this number can be written as a date": a value on a date axis is an OLE Automation date, and
/// <see cref="DateTime.FromOADate"/> makes a date only of a value strictly between -657435 and 2958466 (0001-01-01 to the end of
/// 9999-12-31); it throws on both edges, on everything beyond them and on NaN. A data table, a hover text and the date ticks ask
/// this rule and write any other value as the number, rather than failing a table or a whole draw over one value.</summary>
internal static class OleDateExtensions
{
    /// <summary>The first value <see cref="DateTime.FromOADate"/> makes a date of, to the millisecond.</summary>
    private const double FirstOleDate = -657434.99999999;

    /// <summary>The last value <see cref="DateTime.FromOADate"/> makes a date of, to the millisecond.</summary>
    private const double LastOleDate = 2958465.99999999;

    extension(double value)
    {
        /// <summary>Whether <see cref="DateTime.FromOADate"/> makes a date of the value.</summary>
        internal bool IsOleDate => value > -657435.0 && value < 2958466.0;

        /// <summary>The value moved into the dates <see cref="DateTime.FromOADate"/> makes, the way a date axis whose range runs
        /// past the calendar places its ticks only where there are dates.</summary>
        internal double ClampedToOleDates => Math.Clamp(value, FirstOleDate, LastOleDate);

        /// <summary>The value as a date in <paramref name="format"/>, or, when no date can be made of it, as the number.</summary>
        /// <param name="format">The .NET date and time format.</param>
        internal string ToOleDateText(string format) => value.IsOleDate
            ? DateTime.FromOADate(value).InvariantText(format)
            : value.InvariantText();
    }

    extension(DateTime when)
    {
        /// <summary>The date <paramref name="step"/> later, or <see langword="null"/> when that is past the last date a
        /// <see cref="DateTime"/> holds.</summary>
        /// <param name="step">How far to step.</param>
        internal DateTime? Plus(TimeSpan step) => DateTime.MaxValue - when < step ? null : when + step;

        /// <summary>The first of the next month, or <see langword="null"/> after the last month a <see cref="DateTime"/> holds.</summary>
        internal DateTime? NextMonth => when.Year == DateTime.MaxValue.Year && when.Month == 12 ? null : when.AddMonths(1);
    }
}
