// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>A water as a laboratory delivers it: the concentration of each major ion in milligrams per litre. An ion that is
/// not there is zero. Carbonate and bicarbonate are both given because a laboratory reports both; the diagram adds them.</summary>
public readonly record struct WaterSample
{
    /// <summary>The name of the sample, as the legend and the table show it; none when it has no name.</summary>
    public string? Label { get; init; }

    /// <summary>Calcium (Ca²⁺), in mg/L.</summary>
    public double Ca { get; init; }

    /// <summary>Magnesium (Mg²⁺), in mg/L.</summary>
    public double Mg { get; init; }

    /// <summary>Sodium (Na⁺), in mg/L.</summary>
    public double Na { get; init; }

    /// <summary>Potassium (K⁺), in mg/L; the diagram adds it to sodium.</summary>
    public double K { get; init; }

    /// <summary>Bicarbonate (HCO₃⁻), in mg/L.</summary>
    public double HCO3 { get; init; }

    /// <summary>Carbonate (CO₃²⁻), in mg/L; the diagram adds it to bicarbonate.</summary>
    public double CO3 { get; init; }

    /// <summary>Chloride (Cl⁻), in mg/L.</summary>
    public double Cl { get; init; }

    /// <summary>Sulphate (SO₄²⁻), in mg/L.</summary>
    public double SO4 { get; init; }
}

/// <summary>A water as a Piper diagram reads it: each triangle closed on its own sum, in percent of milliequivalents per litre.
/// The cations are calcium, magnesium and sodium with potassium; the anions are bicarbonate with carbonate, sulphate and chloride.</summary>
/// <param name="Ca">Calcium, in percent of the cations.</param>
/// <param name="Mg">Magnesium, in percent of the cations.</param>
/// <param name="NaK">Sodium and potassium, in percent of the cations.</param>
/// <param name="HCO3CO3">Bicarbonate and carbonate, in percent of the anions.</param>
/// <param name="SO4">Sulphate, in percent of the anions.</param>
/// <param name="Cl">Chloride, in percent of the anions.</param>
/// <param name="ChargeBalance">The charge balance error, <c>(Σcations − Σanions) / (Σcations + Σanions) · 100</c>: how far the
/// analysis is from the electroneutrality that every real water has.</param>
public readonly record struct PiperShares(double Ca, double Mg, double NaK, double HCO3CO3, double SO4, double Cl, double ChargeBalance);

/// <summary>The frame a Piper diagram is drawn in, in units of the side of one triangle: the cation triangle from 0 to 1, the
/// anion triangle from <c>1 + Gap</c> to <c>2 + Gap</c>, and the diamond between and above them.</summary>
public static class PiperFrame
{
    /// <summary>The clear space between the two triangles, and between the triangles and the diamond: 0.2 of a side.</summary>
    public const double Gap = 0.2;

    /// <summary>How wide the frame is: the two triangles and the gap between them.</summary>
    public static readonly double Width = 2 + Gap;

    /// <summary>How high the frame is: the top of the diamond.</summary>
    public static readonly double Height = TernaryFrame.Height * (2 + Gap);

    /// <summary>How far the charge balance error of a water may be, in percent either way, before the analysis is flagged.</summary>
    public const double ChargeBalanceTolerance = 5;

    /// <summary>Where a water stands on the diamond, from the share of its cations that are calcium and magnesium and the share of
    /// its anions that are sulphate and chloride: <c>x = 1 + Gap/2 + (B − A)/2</c>, <c>y = (√3/2)·(Gap + A + B)</c>. The one rule
    /// the markers and the grid of the diamond both stand on.</summary>
    /// <param name="calciumAndMagnesium">A, as a fraction of the cations.</param>
    /// <param name="sulphateAndChloride">B, as a fraction of the anions.</param>
    /// <returns>The place in the frame of the diagram.</returns>
    public static Point DiamondAt(double calciumAndMagnesium, double sulphateAndChloride) =>
        new(1 + Gap / 2 + (sulphateAndChloride - calciumAndMagnesium) / 2, TernaryFrame.Height * (Gap + calciumAndMagnesium + sulphateAndChloride));
}

/// <summary>Turns a water into its place on a Piper diagram: milligrams per litre to milliequivalents (each ion by the mass of one
/// milliequivalent of it), the shares of each triangle, the charge balance, and the place of the water on the cation triangle,
/// the anion triangle and the diamond.</summary>
public static class WaterSampleExtensions
{
    // Milligrams of each ion in one milliequivalent (its atomic or formula mass over its charge).
    private const double MgPerMeqCa = 20.039;
    private const double MgPerMeqMg = 12.1525;
    private const double MgPerMeqNa = 22.98977;
    private const double MgPerMeqK = 39.0983;
    private const double MgPerMeqHCO3 = 61.0168;
    private const double MgPerMeqCO3 = 30.00445;
    private const double MgPerMeqCl = 35.453;
    private const double MgPerMeqSO4 = 48.0313;

    extension(WaterSample sample)
    {
        /// <summary>The ion amounts of this sample, each with its name, for a check that names the ion it refuses.</summary>
        /// <returns>The eight ions in the order of the record.</returns>
        internal IEnumerable<IonAmount> Ions() =>
        [
            new("Ca", sample.Ca), new("Mg", sample.Mg), new("Na", sample.Na), new("K", sample.K),
            new("HCO3", sample.HCO3), new("CO3", sample.CO3), new("Cl", sample.Cl), new("SO4", sample.SO4),
        ];

        /// <summary>The shares of each triangle and the charge balance of this water. A water with no cations or no anions has no
        /// shares (they come out as not a number); the verbs refuse such a water.</summary>
        /// <returns>The percentages of the cations and of the anions, and the charge balance error in percent.</returns>
        public PiperShares ToShares()
        {
            double ca = sample.Ca / MgPerMeqCa;
            double mg = sample.Mg / MgPerMeqMg;
            double naK = sample.Na / MgPerMeqNa + sample.K / MgPerMeqK;
            double alkalinity = sample.HCO3 / MgPerMeqHCO3 + sample.CO3 / MgPerMeqCO3;
            double sulphate = sample.SO4 / MgPerMeqSO4;
            double chloride = sample.Cl / MgPerMeqCl;

            double cations = ca + mg + naK;
            double anions = alkalinity + sulphate + chloride;
            return new PiperShares(
                100 * ca / cations, 100 * mg / cations, 100 * naK / cations,
                100 * alkalinity / anions, 100 * sulphate / anions, 100 * chloride / anions,
                100 * (cations - anions) / (cations + anions));
        }
    }

    extension(PiperShares shares)
    {
        /// <summary>Where this water stands on the cation triangle: calcium at the bottom left, magnesium at the apex, sodium and
        /// potassium at the bottom right (<see cref="TernaryPointExtensions.InFrame"/>).</summary>
        /// <returns>The place in the frame of the diagram.</returns>
        public Point CationPlace() => new TernaryPoint(shares.Mg / 100, shares.Ca / 100, shares.NaK / 100).InFrame();

        /// <summary>Where this water stands on the anion triangle, which is shifted right by <c>1 + Gap</c> sides: bicarbonate and
        /// carbonate at the bottom left, sulphate at the apex, chloride at the bottom right.</summary>
        /// <returns>The place in the frame of the diagram.</returns>
        public Point AnionPlace()
        {
            var at = new TernaryPoint(shares.SO4 / 100, shares.HCO3CO3 / 100, shares.Cl / 100).InFrame();
            return new Point(at.X + 1 + PiperFrame.Gap, at.Y);
        }

        /// <summary>Where this water stands on the diamond, from <c>A = (Ca + Mg) / cations</c> and
        /// <c>B = (SO₄ + Cl) / anions</c>, both as fractions: <c>x = 1 + Gap/2 + (B − A)/2</c>, <c>y = (√3/2)·(Gap + A + B)</c>.</summary>
        /// <returns>The place in the frame of the diagram.</returns>
        public Point DiamondPlace() => PiperFrame.DiamondAt((shares.Ca + shares.Mg) / 100, (shares.SO4 + shares.Cl) / 100);

        /// <summary>Whether the charge balance error is beyond <see cref="PiperFrame.ChargeBalanceTolerance"/> percent either way:
        /// an analysis that does not balance is flagged in the table, not left out of the diagram.</summary>
        /// <returns><see langword="true"/> when the analysis is out of balance.</returns>
        public bool IsUnbalanced() => Math.Abs(shares.ChargeBalance) > PiperFrame.ChargeBalanceTolerance;
    }

}

/// <summary>One ion of a water with its name, as a check on the amounts names what it refuses.</summary>
/// <param name="Ion">The ion's symbol as the record names it.</param>
/// <param name="MgPerLitre">Its concentration, in mg/L.</param>
internal readonly record struct IonAmount(string Ion, double MgPerLitre);
