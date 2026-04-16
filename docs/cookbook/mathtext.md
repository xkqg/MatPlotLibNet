# Math Text

MatPlotLibNet supports LaTeX-like inline math in any label, title, or annotation. Wrap expressions in `$...$`.

## Greek letters and super/subscript

```csharp
Plt.Create()
    .WithTitle(@"$\alpha$ decay and $\beta$ noise — $\omega = 0.4$ rad/ms")
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle(@"R$^{2}$ = 0.97")
        .SetXLabel(@"$\Delta t$ (ms)")
        .SetYLabel(@"$\sigma$ (normalised)")
        .Plot(t, decay, line => { line.Label = @"$\alpha$ decay"; })
        .WithLegend(LegendPosition.UpperRight))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle(@"Noise — $\mu \pm 2\sigma$")
        .SetXLabel(@"$\Delta t$ (ms)")
        .SetYLabel(@"Amplitude ($\times 10^{-3}$)")
        .Plot(t, noise, line => { line.Label = @"$\beta$ noise"; })
        .WithLegend(LegendPosition.UpperRight))
    .TightLayout()
    .Save("math_text.svg");
```

![Math text](../images/math_text.png)

## Fractions, square roots, accents, font variants (v1.3.0)

```csharp
Plt.Create()
    .WithTitle(@"MathText — $\frac{d}{dx}\sqrt{x^2+1}$ and $\hat{\alpha} \cdot \vec{F}$")
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle(@"$\mathbf{y} = \frac{\mathrm{sin}(x)}{e^{x/10}}$")
        .SetXLabel(@"$\Delta t$ (s)")
        .SetYLabel(@"$\hat{y}$ (normalised)")
        .Plot(x, y1, s => { s.Label = @"$\mathrm{sin}$"; })
        .Plot(x, y2, s => { s.Label = @"$\mathrm{cos}$"; })
        .WithLegend(LegendPosition.UpperRight))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle(@"$\sqrt{x^2 + y^2} \leq \mathbb{R}$")
        .SetXLabel(@"$x \in \mathbb{R}$")
        .SetYLabel(@"$\bar{y} \pm \sigma$")
        .Plot(x, y1, s => { s.Label = @"$\vec{v}$"; })
        .WithLegend(LegendPosition.UpperRight))
    .TightLayout()
    .Save("math_text_v130.svg");
```

![MathText v1.3.0](../images/math_text_v130.png)

## Supported features

| Feature | Syntax | Example |
|---|---|---|
| Greek letters | `$\alpha$`, `$\Omega$` | α, Ω |
| Superscript | `$x^{2}$` | x² |
| Subscript | `$x_{i}$` | xᵢ |
| Fractions | `$\frac{a}{b}$` | a/b stacked |
| Square root | `$\sqrt{x}$`, `$\sqrt[n]{x}$` | √x, ⁿ√x |
| Accents | `$\hat{x}$`, `$\bar{x}$`, `$\vec{F}$`, `$\tilde{x}$`, `$\dot{x}$` | x̂, x̄, F⃗ |
| Font variants | `$\mathrm{text}$`, `$\mathbf{F}$`, `$\mathit{x}$`, `$\mathcal{L}$`, `$\mathbb{R}$` | roman, **bold**, *italic*, script, blackboard |
| Text mode | `$\text{label}$` | roman text inside math |
| Spacing | `$a\,b$`, `$a\quad b$` | thin space, em space |
| Delimiters | `$\left(\frac{a}{b}\right)$` | scaled brackets |
| Math operators | `$\pm$`, `$\times$`, `$\leq$`, `$\infty$`, `$\rightarrow$` | ±, ×, ≤, ∞, → |
