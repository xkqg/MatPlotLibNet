---
title: "Power spectrum, coherence and correlation in C# and .NET"
description: "Draw the power spectral density, cross spectrum, coherence, magnitude, phase and angle spectrum, autocorrelation and cross-correlation of a sampled signal in C#, with matplotlib's psd, csd, cohere, acorr and xcorr defaults."
---

# Signal analysis

> **Install:** `dotnet add package MatPlotLibNet`

A signal is samples taken one after the other at a steady rate, handed over as a `double[]` in the order they were
taken. The verbs on this page draw what the signal is made of rather than the samples themselves: how its power is
spread over the frequencies, how much two signals share and where they move together, and how much a signal resembles
itself or another signal moved along by some number of samples. Each draws one line from series the library already
has, sets no axis limit, and goes through JSON as that line.

Each verb takes one options value, and its `default` is matplotlib's defaults, so a call with the options left out draws
what matplotlib draws with the keywords left out:

| Options | Used by | Holds |
|---|---|---|
| `SpectralOptions` | `Psd`, `Csd`, `Cohere` | `SampleRate` (2), `Window` (Hann), `Detrend` (none), `SegmentLength` (256), `Overlap` (0), `PadTo` (0, none), `CenterFrequency` (0), `Scaling` (`Density`) |
| `SpectrumOptions` | `MagnitudeSpectrum`, `PhaseSpectrum`, `AngleSpectrum` | `SampleRate` (2), `Window` (Hann), `PadTo` (0, none), `CenterFrequency` (0) |
| `CorrelationOptions` | `Acorr`, `Xcorr` | `MaxLags` (10), `Unnormalised` (false), `Detrend` (none), `Markers` (false) |

A window is a `SpectralWindow` (`Hann`, `Hamming`, `Blackman`, `Rectangular`, or `SpectralWindow.Of` your own weights) and
a detrend a `SpectralDetrend` (`None`, `Mean`, `Linear`, or `SpectralDetrend.Of` your own); both live in
`MatPlotLibNet.Numerics`. A setting that cannot be used (a sample rate that is not above zero, a negative overlap) is
refused by its own name when it is set; one that does not fit the others or the signal (an overlap as long as the segment,
a segment longer than the signal) is refused by the verb, before it draws anything.

The examples below use two signals: the vibration of a machine, with tones at 50 and 170 Hz, and a sensor on its casing,
which picks up the 50 Hz three samples later and a tone of its own at 310 Hz, each with some noise:

```csharp
using MatPlotLibNet;

const int rate = 1000;
ulong state = 7;
double Noise()
{
    state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
    return (state >> 11) / (double)(1UL << 53) - 0.5;
}

double[] vibration = [.. Enumerable.Range(0, 4000).Select(i =>
    Math.Sin(2 * Math.PI * 50 * i / rate) + 0.5 * Math.Sin(2 * Math.PI * 170 * i / rate) + Noise())];
double[] casing = [.. Enumerable.Range(0, 4000).Select(i =>
    0.7 * Math.Sin(2 * Math.PI * 50 * (i - 3) / rate) + 0.4 * Math.Sin(2 * Math.PI * 310 * i / rate) + Noise())];
var options = new SpectralOptions { SampleRate = rate, SegmentLength = 256, Overlap = 128 };
```

## Power spectral density (psd)

`Psd` estimates how the power of a signal is spread over the frequencies by Welch's method: the signal is cut into
segments of `SegmentLength` samples that start `SegmentLength - Overlap` apart, each segment is detrended, weighted by
the window and transformed, and the powers are averaged. The line is ten times the logarithm to base ten of the density,
so a tone stands out as a peak however quiet it is.

```csharp
Plt.Create()
    .WithTitle("Where the vibration holds its power")
    .AddSubPlot(1, 1, 1, ax => ax
        .Psd(vibration, options, line => line.Label = "vibration")
        .SetXLabel("Frequency (Hz)")
        .WithLegend())
    .Save("psd_vibration.svg");
```

![The power spectral density of the vibration, with peaks at 50 and 170 Hz](../images/psd_vibration.png)

The Y axis is titled "Power Spectral Density (dB/Hz)", or "(dB)" with `Scaling = SpectralScaling.Spectrum` (matplotlib's
`scale_by_freq=False`). A title of your own set after the call replaces it. What is left at the end of the signal, less
than a segment, is not used, and a signal shorter than one segment is refused: give a shorter `SegmentLength`.

## Cross spectral density (csd)

`Csd` draws how much power two signals share at each frequency: the product of the transforms of their segments, averaged
the same way, drawn as the magnitude in decibels. Only the 50 Hz tone is in both signals here, so it is the one peak that
stands clear of the noise.

```csharp
Plt.Create()
    .WithTitle("What the casing shares with the vibration")
    .AddSubPlot(1, 1, 1, ax => ax
        .Csd(vibration, casing, options, line => line.Label = "vibration with casing")
        .SetXLabel("Frequency (Hz)")
        .WithLegend())
    .Save("csd_casing.svg");
```

![The cross spectral density of the two signals, with one clear peak at 50 Hz](../images/csd_casing.png)

## Coherence (cohere)

`Cohere` draws, at each frequency, how much of one signal is explained by the other as a linear relation, from 0 (none) to
1 (all of it): |Pxy|² / (Pxx · Pyy), on the linear scale. The coherence of a single segment is 1 everywhere, whatever the
signals are, so the signals must hold at least two segments, as matplotlib requires.

```csharp
Plt.Create()
    .WithTitle("Where the casing follows the vibration")
    .AddSubPlot(1, 1, 1, ax => ax
        .Cohere(vibration, casing, options, line => line.Label = "coherence")
        .SetXLabel("Frequency (Hz)")
        .WithLegend())
    .Save("cohere_casing.svg");
```

![The coherence of the two signals, close to 1 at 50 Hz and low elsewhere](../images/cohere_casing.png)

## Magnitude, phase and angle spectrum

`MagnitudeSpectrum`, `PhaseSpectrum` and `AngleSpectrum` take one transform of the whole signal instead of averaging
segments, so the line shows every detail and every bit of noise. The magnitude is |X| divided by the sum of the window's
weights, so a constant signal has magnitude 1 at frequency 0; `MagnitudeScale.Decibel` draws it as 20 · log10. The phase
is unwrapped along the frequencies, so a delay is a straight line whose slope is the delay; the angle stays wrapped between
−π and π. An impulse and its echo, under a rectangular window:

```csharp
using MatPlotLibNet.Numerics;

double[] echo = [.. Enumerable.Range(0, 256).Select(i => i == 10 ? 1.0 : i == 40 ? 0.5 : 0.0)];
var spectrum = new SpectrumOptions { SampleRate = 1000, Window = SpectralWindow.Rectangular };

Plt.Create()
    .WithSize(1100, 380)
    .TightLayout()
    .AddSubPlot(1, 3, 1, ax => ax
        .WithTitle("Magnitude")
        .MagnitudeSpectrum(echo, spectrum, MagnitudeScale.Decibel)
        .SetXLabel("Frequency (Hz)"))
    .AddSubPlot(1, 3, 2, ax => ax
        .WithTitle("Phase")
        .PhaseSpectrum(echo, spectrum)
        .SetXLabel("Frequency (Hz)"))
    .AddSubPlot(1, 3, 3, ax => ax
        .WithTitle("Angle")
        .AngleSpectrum(echo, spectrum)
        .SetXLabel("Frequency (Hz)"))
    .Save("echo_spectra.svg");
```

![The magnitude, phase and angle spectrum of an impulse and its echo](../images/echo_spectra.png)

## Autocorrelation and cross-correlation (acorr, xcorr)

`Acorr` draws how much a signal resembles itself moved along by each number of samples, for the lags −`MaxLags` to
`MaxLags`; `Xcorr` does the same for two signals. The value at lag k is the sum of x[n + k] · y[n], so a positive lag is
x delayed against y, as in matplotlib: when y is x some samples later, the chart peaks at the negative lag of that many
samples. Normalised (the default), lag 0 of an autocorrelation is exactly 1. The chart is drawn as stems with a line at 0,
or with `Markers` as circles alone, never both; the callback receives a `CorrelationParts` with the `Line` and the `Zero`
line (null for markers).

```csharp
double[] signal = [.. Enumerable.Range(0, 200).Select(i => Math.Sin(2 * Math.PI * i / 25.0) + 0.3 * Math.Sin(2 * Math.PI * i / 7.0))];
double[] sent = [.. Enumerable.Range(0, 120).Select(i => i % 20 < 3 ? 1.0 : 0.0)];
double[] received = [.. Enumerable.Range(0, 120).Select(i => i >= 4 && (i - 4) % 20 < 3 ? 1.0 : 0.0)];

Plt.Create()
    .WithSize(960, 400)
    .TightLayout()
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("A signal that repeats every 25 samples")
        .Acorr(signal, new CorrelationOptions { MaxLags = 60 })
        .SetXLabel("Lag (samples)"))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("The echo runs 4 samples behind")
        .Xcorr(sent, received, new CorrelationOptions { MaxLags = 12, Markers = true })
        .SetXLabel("Lag (samples)"))
    .Save("correlation_lags.svg");
```

![The autocorrelation of a periodic signal as stems, and the cross-correlation of a pulse train and its echo as markers peaking at lag -4](../images/correlation_lags.png)

The signal needs more samples than `MaxLags`, or the call is refused.

## A missing sample

NaN is the one missing value, and a value worked out from a missing sample is missing too. A Welch segment that holds a
missing or infinite sample is left out of the average (for `Csd` and `Cohere`, a segment missing in either signal), and
when no segment is left nothing is drawn. A spectrum of the whole signal and a correlation are worked out from every
sample, so one missing sample leaves them empty; a correlation keeps its line at 0. A frequency with no power at all is a
gap in decibels, never minus infinity, and a normalised correlation of a signal with no energy is a gap, never zero.

## Spectrogram

`Spectrogram` draws the short-time transform of a signal: the magnitude of each frequency in each window, as colour. Each
frame is weighted once by the Hann window and transformed at exactly `WindowSize` samples, so its `WindowSize / 2 + 1`
rows stand at the frequencies they are, for a window of any length. A 125 Hz tone through a window of 100 samples lights
the row at 125 Hz:

```csharp
using MatPlotLibNet.Styling;

double[] tone = [.. Enumerable.Range(0, 2000).Select(i => Math.Sin(2 * Math.PI * 125 * i / 1000.0))];

Plt.Create()
    .WithTitle("A 125 Hz tone")
    .AddSubPlot(1, 1, 1, ax => ax
        .Spectrogram(tone, 1000, s => { s.WindowSize = 100; s.Overlap = 50; })
        .AxHLine(125, line => { line.Color = Colors.Red; line.LineStyle = LineStyle.Dashed; })
        .SetXLabel("Time (s)")
        .SetYLabel("Frequency (Hz)"))
    .Save("spectrogram_tone.svg");
```

![A spectrogram of a 125 Hz tone with the bright row on the dashed line at 125 Hz](../images/spectrogram_tone.png)

The arrays behind the picture are `Fft.Stft(signal, windowSize, overlap, sampleRate)` in `MatPlotLibNet.Numerics`: the
magnitudes, the frequency of each row and the time of each column.
