---
title: "LaTeX-style math in C# chart labels"
description: "Write LaTeX-style math in C# chart titles, labels and annotations: fractions, superscripts, Greek letters and symbols."
---

# Math Text

> **Install:** `dotnet add package MatPlotLibNet`

MatPlotLibNet supports LaTeX-like inline math in any label, title, or annotation. Wrap expressions in `$...$`. It reads the
grammar of matplotlib's mathtext: every one of its 634 symbol names, its 32 function names, its 23 accents and its 12 spaces, and it
spaces math as matplotlib does: a space typed inside `$...$` is no space, a binary operator, a relation and an arrow stand between
two small spaces, punctuation has one after it, and a function name has a thin space after it.

Math is laid out as matplotlib lays it out (TeX's rules, on the sizes of the theme's math fonts): a superscript is raised and a
subscript lowered by the font's own measures and set one size smaller, a fraction is stacked over a rule on the math axis, a square
root draws its sign and a bar over its body, a sum takes its limits above and below, a delimiter after `\left` grows to what it
encloses, and anything can be nested in anything. The same layout is measured for the figure's layout and drawn on every surface
(SVG, PNG, PDF, the native controls), each piece of it as a text of its own and each rule as a filled rectangle.

## The fonts math is drawn in

The text around the math keeps the label's font. The math itself is set in the fonts the theme names, as matplotlib's
`mathtext.fontset` names them:

| Theme | Math font set | What it looks like |
|---|---|---|
| `Theme.MatplotlibV2` (the default) | matplotlib's `dejavusans` | letters in DejaVu Sans Oblique, digits and signs in DejaVu Sans, large operators in DejaVu Sans Display |
| `Theme.MatplotlibClassic` | matplotlib's `cm` | Computer Modern: letters in math italic (`cmmi10`), digits in roman (`cmr10`), signs and operators where TeX puts them |
| the library's own themes | the label's font | the label's font, upright |

The fonts ship inside `MatPlotLibNet.Skia` (the Computer Modern faces are the BaKoMa fonts matplotlib ships and the STIX faces are the
STIX fonts it ships, each under its own licence, named in the package's `THIRD-PARTY-NOTICES.md`), so a PNG, a PDF and an SVG drawn with the Skia package loaded show
the same glyphs on every machine; the SVG writes each piece of a label as its glyph outline. Without the Skia package an SVG
writes each piece as a text element in the label's font, where the layout puts it, for the viewer to draw. A character the set does not hold
is set where matplotlib takes it from, in its STIX fonts: `\neq` and `\partial` under `Theme.MatplotlibClassic`, and `\mathbb` and
`\mathcal` under either matplotlib theme (`\mathbb{R}` is STIXGeneral's double-struck R, `\mathcal{A}` a STIX script capital).

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

## Fractions, square roots, accents, font variants

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

![Fractions, square roots, accents and font variants](../images/math_text_v130.png)

## Operator limits — integrals, sums, products

A script on a sum, a product, `\lim`, `\max`, `\min`, `\sup`, `\liminf` or `\limsup` is read as its limit; an integral's scripts,
like those of `\log` or `\sin`, are scripts beside it, as matplotlib reads them:

```csharp
Plt.Create()
    .WithTitle(@"$\sum_{i=0}^{n} \frac{1}{i!} = e$ — Taylor expansion")
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel(@"$\int_a^b f(x)\,dx$")
        .SetYLabel(@"$\prod_{k=1}^{N} (1 + r_k)$")
        .Plot(x, y, s => s.Label = @"$e^{-\lambda t}$")
        .WithLegend())
    .Save("operator_limits.svg");
```

### Supported large operators

| LaTeX | Renders | Example |
|---|---|---|
| `\int_a^b` | ∫ with its scripts beside it | `$\int_0^{\infty} e^{-x}\,dx$` |
| `\iint` | ∬ | `$\iint_D f\,dA$` |
| `\iiint` | ∭ | `$\iiint_V f\,dV$` |
| `\oint` | ∮ | `$\oint_C \vec{F} \cdot d\vec{r}$` |
| `\sum_{i=0}^{n}` | Σ with limits (also `\coprod`, `\bigcup`, `\bigcap` and the other big operators) | `$\sum_{i=1}^{n} x_i$` |
| `\prod_{k=1}^{N}` | Π with limits | `$\prod_{k=1}^{N} a_k$` |
| `\lim_{x \to 0}` | lim with subscript below | `$\lim_{n \to \infty} a_n$` |
| `\max`, `\min` | max/min with subscript | `$\max_{x \in S} f(x)$` |

## Matrices

```csharp
Plt.Create()
    .WithTitle(@"$\begin{pmatrix} a & b \\ c & d \end{pmatrix} \cdot \vec{x} = \vec{b}$")
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel(@"$\begin{bmatrix} 1 & 0 \\ 0 & 1 \end{bmatrix}$")
        .Plot(x, y))
    .Save("matrix_labels.svg");
```

### Supported matrix environments

| Environment | Delimiters | Example |
|---|---|---|
| `matrix` | None | `$\begin{matrix} a & b \\ c & d \end{matrix}$` |
| `pmatrix` | Parentheses ( ) | `$\begin{pmatrix} 1 & 2 \\ 3 & 4 \end{pmatrix}$` |
| `bmatrix` | Brackets [ ] | `$\begin{bmatrix} x \\ y \\ z \end{bmatrix}$` |
| `vmatrix` | Vertical bars | | | `$\begin{vmatrix} a & b \\ c & d \end{vmatrix}$` |

Separate cells with `&` and rows with `\\`.

## Supported features

| Feature | Syntax | Example |
|---|---|---|
| Greek letters | `$\alpha$`, `$\Omega$` | α, Ω |
| Superscript | `$x^{2}$`, `$f'(x)$` | x², raised by the font's measure, one size smaller; an apostrophe a prime |
| Subscript | `$x_{i}$` | xᵢ, lowered; both scripts kept apart |
| Fractions | `$\frac{a}{b}$`, `$\dfrac{a}{b}$`, `$\binom{n}{k}$`, `$\genfrac{(}{)}{0}{1}{a}{b}$` | stacked over a rule on the math axis (`\dfrac` at the text's size, `\binom` without a rule, in parentheses) |
| Square root | `$\sqrt{x}$`, `$\sqrt[n]{x}$` | the sign grown to the body, a bar over it, the index raised over the sign |
| Rules and annotations | `$\overline{x}$`, `$\underline{x}$`, `$\overset{!}{=}$`, `$\underset{n}{\max}$`, `$\substack{a \\ b}$` | a rule over or under, a small annotation over or under, rows stacked |
| Spacing boxes | `$\phantom{x}$`, `$\llap{x}$`, `$\rlap{x}$`, `$\hspace{2}$` | room without ink, ink without room, a space of so many ems |
| Accents | `$\hat{x}$`, `$\bar{x}$`, `$\vec{F}$`, `$\tilde{x}$`, `$\dot{x}$`, `$\acute{e}$`, `$\mathring{a}$`, `$\widehat{x}$` | x̂, x̄, F⃗, é, å |
| Font variants | `$\mathrm{text}$`, `$\mathbf{F}$`, `$\mathit{x}$`, `$\mathcal{L}$`, `$\mathbb{R}$` | upright, **bold** and *italic* under every theme; `\mathcal` in Computer Modern's calligraphic capitals under the classic theme and in STIX's script letters under the default one; `\mathbb` in STIX's double-struck letters under both matplotlib themes, in the label's font under the library's own |
| Text mode | `$\text{a label}$` | roman text inside math, its spaces kept |
| Spacing | `$a\,b$`, `$a\;b$`, `$a\enspace b$`, `$a\quad b$`, `$a~b$` | a sixth of an em, a quarter, half an em, an em, a third; a space typed in math is none |
| Function names | `$\sin x$`, `$\exp(x)$`, `$\det A$`, `$\arctan$`, `$\operatorname{sgn} x$` | upright, a thin space after the name |
| Groups | `${}^{14}C$`, `$\{x\}$` | a brace groups and is not drawn; `\{` and `\}` are braces |
| Delimiters | `$\left(\frac{a}{b}\right)$`, `$\left\langle x \middle| y \right\rangle$` | the bracket, ⟨ \| ⟩, grown to what they enclose; `\left.` draws none |
| Symbols | `$\pm$`, `$\times$`, `$\leq$`, `$\infty$`, `$\rightarrow$`, `$\Rightarrow$`, `$\circ$` | ±, ×, ≤, ∞, →, ⇒, ∘: every name of matplotlib's mathtext |
| Large operators | `$\sum_{i}^{n}$`, `$\int_a^b$`, `$\prod$` | Σ, ∫, Π with limits |
| Matrices | `$\begin{pmatrix}...\end{pmatrix}$` | cells in rows and columns at the text's size, centred on the math axis, the delimiters grown to the grid |

## Using math in annotations

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .Annotate(@"$\frac{\partial f}{\partial x} = 0$ (critical point)", 5, 3.2, ann =>
        {
            ann.ArrowTargetX = 5;
            ann.ArrowTargetY = y[5];
            ann.ArrowStyle = ArrowStyle.FancyArrow;
        }))
    .Save("math_annotation.svg");
```
