// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Written by tools/mpl_reference/generate_mathtext.py from matplotlib 3.11.2: each character's name parsed with
// matplotlib's own MathTextParser under mathtext.fontset 'cm' (its classic style), and the font and code of the glyph it
// set read: change the table by running it again.

using System.Collections.Frozen;

namespace MatPlotLibNet.Rendering.MathText;

internal sealed partial class ComputerModernMathFontSet
{
    /// <summary>Every character but an ASCII letter or digit that the parser can put in a math span and Computer Modern holds: its
    /// face and its code there. A character matplotlib takes from STIX is a comment: StixFallback sets it. A method, so the
    /// table is made after the faces it reads (the initialisers of two parts of a class run in no set order).</summary>
    private static FrozenDictionary<char, MathGlyph> TableOfFaces() => new Dictionary<char, MathGlyph>
    {
        ['\u0021'] = new(Roman, '\u0021'), // ASCII, Roman 0x21
        // ASCII (U+0022): not in Computer Modern, no face holds it, the label's face
        ['\u0023'] = new(Roman, '\u0023'), // ASCII, Roman 0x23
        ['\u0024'] = new(Roman, '\u0024'), // ASCII, Roman 0x24
        ['\u0025'] = new(Roman, '\u0025'), // ASCII, Roman 0x25
        ['\u0026'] = new(Roman, '\u0026'), // ASCII, Roman 0x26
        ['\u0027'] = new(Symbols, '\u0030'), // ASCII, Symbols 0x30
        ['\u0028'] = new(Roman, '\u0028'), // ASCII, Roman 0x28
        ['\u0029'] = new(Roman, '\u0029'), // ASCII, Roman 0x29
        ['\u002A'] = new(Symbols, '\u00A4'), // ASCII, Symbols 0xa4
        ['\u002B'] = new(Roman, '\u002B'), // ASCII, Roman 0x2b
        ['\u002C'] = new(MathItalic, '\u003B'), // ASCII, MathItalic 0x3b
        ['\u002D'] = new(Symbols, '\u00A1'), // ASCII, Symbols 0xa1
        ['\u002E'] = new(MathItalic, '\u003A'), // ASCII, MathItalic 0x3a
        ['\u002F'] = new(MathItalic, '\u003D'), // ASCII, MathItalic 0x3d
        ['\u003A'] = new(Roman, '\u003A'), // ASCII, Roman 0x3a
        ['\u003B'] = new(Roman, '\u003B'), // ASCII, Roman 0x3b
        ['\u003C'] = new(MathItalic, '\u003C'), // ASCII, MathItalic 0x3c
        ['\u003D'] = new(Roman, '\u003D'), // ASCII, Roman 0x3d
        ['\u003E'] = new(MathItalic, '\u003E'), // ASCII, MathItalic 0x3e
        ['\u003F'] = new(Roman, '\u003F'), // ASCII, Roman 0x3f
        ['\u0040'] = new(Roman, '\u0040'), // ASCII, Roman 0x40
        ['\u005B'] = new(Roman, '\u005B'), // ASCII, Roman 0x5b
        ['\u005C'] = new(Symbols, '\u006E'), // \backslash, Symbols 0x6e
        ['\u005D'] = new(Roman, '\u005D'), // ASCII, Roman 0x5d
        // ASCII (U+005E): not in Computer Modern, StixFallback sets it
        ['\u005F'] = new(Typewriter, '\u005F'), // ASCII, Typewriter 0x5f
        // ASCII (U+0060): not in Computer Modern, StixFallback sets it
        ['\u007B'] = new(Extension, '\u00A9'), // ASCII, Extension 0xa9
        ['\u007C'] = new(Symbols, '\u006A'), // ASCII, Symbols 0x6a
        ['\u007D'] = new(Extension, '\u00AA'), // ASCII, Extension 0xaa
        // ASCII (U+007E): not in Computer Modern, StixFallback sets it
        // \textexclamdown (U+00A1): not in Computer Modern, StixFallback sets it
        // \cent (U+00A2): not in Computer Modern, StixFallback sets it
        // \sterling (U+00A3): not in Computer Modern, StixFallback sets it
        // \yen (U+00A5): not in Computer Modern, StixFallback sets it
        ['\u00A7'] = new(Symbols, '\u0078'), // \S, Symbols 0x78
        // \copyright (U+00A9): not in Computer Modern, StixFallback sets it
        // \guillemotleft (U+00AB): not in Computer Modern, StixFallback sets it
        ['\u00AC'] = new(Symbols, '\u003A'), // \neg, Symbols 0x3a
        // \circledR (U+00AE): not in Computer Modern, StixFallback sets it
        // \macron (U+00AF): not in Computer Modern, StixFallback sets it
        // \degree (U+00B0): not in Computer Modern, StixFallback sets it
        ['\u00B1'] = new(Symbols, '\u00A7'), // \pm, Symbols 0xa7
        // \textasciiacute (U+00B4): not in Computer Modern, StixFallback sets it
        ['\u00B6'] = new(Symbols, '\u007B'), // \P, Symbols 0x7b
        // \cdotp (U+00B7): not in Computer Modern, StixFallback sets it
        // \guillemotright (U+00BB): not in Computer Modern, StixFallback sets it
        // \textquestiondown (U+00BF): not in Computer Modern, StixFallback sets it
        // \AA (U+00C5): not in Computer Modern, StixFallback sets it
        // \AE (U+00C6): not in Computer Modern, StixFallback sets it
        // \DH (U+00D0): not in Computer Modern, StixFallback sets it
        ['\u00D7'] = new(Symbols, '\u00A3'), // \times, Symbols 0xa3
        // \O (U+00D8): not in Computer Modern, StixFallback sets it
        // \Thorn (U+00DE): not in Computer Modern, StixFallback sets it
        // \ss (U+00DF): not in Computer Modern, StixFallback sets it
        // \aa (U+00E5): not in Computer Modern, StixFallback sets it
        // \ae (U+00E6): not in Computer Modern, StixFallback sets it
        // \dh (U+00F0): not in Computer Modern, StixFallback sets it
        ['\u00F7'] = new(Symbols, '\u00A5'), // \div, Symbols 0xa5
        // \o (U+00F8): not in Computer Modern, StixFallback sets it
        // \thorn (U+00FE): not in Computer Modern, StixFallback sets it
        // \hbar (U+0127): not in Computer Modern, StixFallback sets it
        ['\u0131'] = new(MathItalic, '\u007B'), // \imath, MathItalic 0x7b
        // \L (U+0141): not in Computer Modern, StixFallback sets it
        // \l (U+0142): not in Computer Modern, StixFallback sets it
        // \OE (U+0152): not in Computer Modern, StixFallback sets it
        // \oe (U+0153): not in Computer Modern, StixFallback sets it
        // \lambdabar (U+019B): not in Computer Modern, StixFallback sets it
        ['\u0237'] = new(MathItalic, '\u007C'), // \jmath, MathItalic 0x7c
        // \rasp (U+02BC): not in Computer Modern, StixFallback sets it
        // \lasp (U+02BD): not in Computer Modern, StixFallback sets it
        // \ring (U+02DA): not in Computer Modern, StixFallback sets it
        ['\u0300'] = new(Roman, '\u00B5'), // \combininggraveaccent, Roman 0xb5
        ['\u0301'] = new(Roman, '\u00B6'), // \combiningacuteaccent, Roman 0xb6
        ['\u0302'] = new(Roman, '\u005E'), // \circumflexaccent, Roman 0x5e
        ['\u0303'] = new(Roman, '\u007E'), // \combiningtilde, Roman 0x7e
        ['\u0304'] = new(Roman, '\u00B9'), // \combiningoverline, Roman 0xb9
        ['\u0306'] = new(Roman, '\u00B8'), // \combiningbreve, Roman 0xb8
        ['\u0307'] = new(Roman, '\u005F'), // \combiningdotabove, Roman 0x5f
        ['\u0308'] = new(Roman, '\u00C4'), // \combiningdiaeresis, Roman 0xc4
        // \ocirc (U+030A): not in Computer Modern, StixFallback sets it
        // \H (U+030B): not in Computer Modern, StixFallback sets it
        // \check (U+030C): not in Computer Modern, StixFallback sets it
        // \candra (U+0310): not in Computer Modern, StixFallback sets it
        // \overarc (U+0311): not in Computer Modern, StixFallback sets it
        // \d (U+0323): not in Computer Modern, StixFallback sets it
        // \c (U+0327): not in Computer Modern, StixFallback sets it
        // \k (U+0328): not in Computer Modern, StixFallback sets it
        // \underbar (U+0331): not in Computer Modern, StixFallback sets it
        // \not (U+0338): not in Computer Modern, StixFallback sets it
        // \t (U+0361): not in Computer Modern, StixFallback sets it
        // \Alpha (U+0391): not in Computer Modern, StixFallback sets it
        // \Beta (U+0392): not in Computer Modern, StixFallback sets it
        ['\u0393'] = new(Roman, '\u00A1'), // \Gamma, Roman 0xa1
        ['\u0394'] = new(Roman, '\u00A2'), // \Delta, Roman 0xa2
        // \Epsilon (U+0395): not in Computer Modern, StixFallback sets it
        // \Zeta (U+0396): not in Computer Modern, StixFallback sets it
        // \Eta (U+0397): not in Computer Modern, StixFallback sets it
        ['\u0398'] = new(Roman, '\u00A3'), // \Theta, Roman 0xa3
        // \Iota (U+0399): not in Computer Modern, StixFallback sets it
        // \Kappa (U+039A): not in Computer Modern, StixFallback sets it
        ['\u039B'] = new(Roman, '\u00A4'), // \Lambda, Roman 0xa4
        // \Mu (U+039C): not in Computer Modern, StixFallback sets it
        // \Nu (U+039D): not in Computer Modern, StixFallback sets it
        ['\u039E'] = new(Roman, '\u00A5'), // \Xi, Roman 0xa5
        // \Omicron (U+039F): not in Computer Modern, StixFallback sets it
        ['\u03A0'] = new(Roman, '\u00A6'), // \Pi, Roman 0xa6
        // \Rho (U+03A1): not in Computer Modern, StixFallback sets it
        ['\u03A3'] = new(Roman, '\u00A7'), // \Sigma, Roman 0xa7
        // \Tau (U+03A4): not in Computer Modern, StixFallback sets it
        ['\u03A5'] = new(Roman, '\u00A8'), // \Upsilon, Roman 0xa8
        ['\u03A6'] = new(Roman, '\u00A9'), // \Phi, Roman 0xa9
        // \Chi (U+03A7): not in Computer Modern, StixFallback sets it
        ['\u03A8'] = new(Roman, '\u00AA'), // \Psi, Roman 0xaa
        ['\u03A9'] = new(Roman, '\u00AD'), // \Omega, Roman 0xad
        ['\u03B1'] = new(MathItalic, '\u00AE'), // \alpha, MathItalic 0xae
        ['\u03B2'] = new(MathItalic, '\u00AF'), // \beta, MathItalic 0xaf
        ['\u03B3'] = new(MathItalic, '\u00B0'), // \gamma, MathItalic 0xb0
        ['\u03B4'] = new(MathItalic, '\u00B1'), // \delta, MathItalic 0xb1
        ['\u03B5'] = new(MathItalic, '\u00B2'), // \epsilon, MathItalic 0xb2
        ['\u03B6'] = new(MathItalic, '\u00B3'), // \zeta, MathItalic 0xb3
        ['\u03B7'] = new(MathItalic, '\u00B4'), // \eta, MathItalic 0xb4
        ['\u03B8'] = new(MathItalic, '\u00B5'), // \theta, MathItalic 0xb5
        ['\u03B9'] = new(MathItalic, '\u00B6'), // \iota, MathItalic 0xb6
        ['\u03BA'] = new(MathItalic, '\u2219'), // \kappa, MathItalic 0x2219
        ['\u03BB'] = new(MathItalic, '\u00B8'), // \lambda, MathItalic 0xb8
        ['\u03BC'] = new(MathItalic, '\u00B9'), // \mu, MathItalic 0xb9
        ['\u03BD'] = new(MathItalic, '\u00BA'), // \nu, MathItalic 0xba
        ['\u03BE'] = new(MathItalic, '\u00BB'), // \xi, MathItalic 0xbb
        // \omicron (U+03BF): not in Computer Modern, StixFallback sets it
        ['\u03C0'] = new(MathItalic, '\u00BC'), // \pi, MathItalic 0xbc
        ['\u03C1'] = new(MathItalic, '\u00BD'), // \rho, MathItalic 0xbd
        ['\u03C2'] = new(MathItalic, '\u0026'), // \varsigma, MathItalic 0x26
        ['\u03C3'] = new(MathItalic, '\u00BE'), // \sigma, MathItalic 0xbe
        ['\u03C4'] = new(MathItalic, '\u00BF'), // \tau, MathItalic 0xbf
        ['\u03C5'] = new(MathItalic, '\u00C0'), // \upsilon, MathItalic 0xc0
        ['\u03C6'] = new(MathItalic, '\u0027'), // \varphi, MathItalic 0x27
        ['\u03C7'] = new(MathItalic, '\u00C2'), // \chi, MathItalic 0xc2
        ['\u03C8'] = new(MathItalic, '\u00C3'), // \psi, MathItalic 0xc3
        ['\u03C9'] = new(MathItalic, '\u0021'), // \omega, MathItalic 0x21
        ['\u03D1'] = new(MathItalic, '\u0023'), // \vartheta, MathItalic 0x23
        ['\u03D5'] = new(MathItalic, '\u00C1'), // \phi, MathItalic 0xc1
        // \varpi (U+03D6): not in Computer Modern, StixFallback sets it
        // \digamma (U+03DD): not in Computer Modern, StixFallback sets it
        // \varkappa (U+03F0): not in Computer Modern, StixFallback sets it
        ['\u03F1'] = new(MathItalic, '\u0025'), // \varrho, MathItalic 0x25
        // \backepsilon (U+03F6): not in Computer Modern, StixFallback sets it
        // \quad (U+2003): not in Computer Modern, no face holds it, the label's face
        // \thickspace (U+2005): not in Computer Modern, StixFallback sets it
        // \endash (U+2013): not in Computer Modern, StixFallback sets it
        // \emdash (U+2014): not in Computer Modern, StixFallback sets it
        ['\u2016'] = new(Symbols, '\u006B'), // \Vert, Symbols 0x6b
        // \lq (U+2018): not in Computer Modern, StixFallback sets it
        // \rq (U+2019): not in Computer Modern, StixFallback sets it
        // \textquotedblleft (U+201C): not in Computer Modern, StixFallback sets it
        // \textquotedblright (U+201D): not in Computer Modern, StixFallback sets it
        ['\u2020'] = new(Symbols, '\u0079'), // \dag, Symbols 0x79
        ['\u2021'] = new(Symbols, '\u007A'), // \ddag, Symbols 0x7a
        // \dots (U+2026): not in Computer Modern, StixFallback sets it
        // \perthousand (U+2030): not in Computer Modern, StixFallback sets it
        ['\u2032'] = new(Symbols, '\u0030'), // \prime, Symbols 0x30
        // \dprime (U+2033): not in Computer Modern, StixFallback sets it
        // \backprime (U+2035): not in Computer Modern, StixFallback sets it
        // \guilsinglleft (U+2039): not in Computer Modern, StixFallback sets it
        // \guilsinglright (U+203A): not in Computer Modern, StixFallback sets it
        // \leftharpoonaccent (U+20D0): not in Computer Modern, StixFallback sets it
        // \rightharpoonaccent (U+20D1): not in Computer Modern, StixFallback sets it
        ['\u20D7'] = new(MathItalic, '\u007E'), // \combiningrightarrowabove, MathItalic 0x7e
        // \combiningthreedotsabove (U+20DB): not in Computer Modern, StixFallback sets it
        // \combiningfourdotsabove (U+20DC): not in Computer Modern, StixFallback sets it
        // \overleftrightarrow (U+20E1): not in Computer Modern, StixFallback sets it
        // \BbbC (U+2102): not in Computer Modern, StixFallback sets it
        // \scrg (U+210A): not in Computer Modern, StixFallback sets it
        // \scrH (U+210B): not in Computer Modern, StixFallback sets it
        // \hslash (U+210F): not in Computer Modern, StixFallback sets it
        // \scrI (U+2110): not in Computer Modern, StixFallback sets it
        ['\u2111'] = new(Symbols, '\u003D'), // \Im, Symbols 0x3d
        // \scrL (U+2112): not in Computer Modern, StixFallback sets it
        ['\u2113'] = new(MathItalic, '\u0060'), // \ell, MathItalic 0x60
        // \BbbN (U+2115): not in Computer Modern, StixFallback sets it
        ['\u2118'] = new(MathItalic, '\u007D'), // \wp, MathItalic 0x7d
        // \BbbP (U+2119): not in Computer Modern, StixFallback sets it
        // \BbbQ (U+211A): not in Computer Modern, StixFallback sets it
        // \scrR (U+211B): not in Computer Modern, StixFallback sets it
        ['\u211C'] = new(Symbols, '\u003C'), // \Re, Symbols 0x3c
        // \BbbR (U+211D): not in Computer Modern, StixFallback sets it
        // \BbbZ (U+2124): not in Computer Modern, StixFallback sets it
        // \mho (U+2127): not in Computer Modern, StixFallback sets it
        // \frakZ (U+2128): not in Computer Modern, StixFallback sets it
        // \scrB (U+212C): not in Computer Modern, StixFallback sets it
        // \frakC (U+212D): not in Computer Modern, StixFallback sets it
        // \scre (U+212F): not in Computer Modern, StixFallback sets it
        // \scrE (U+2130): not in Computer Modern, StixFallback sets it
        // \scrF (U+2131): not in Computer Modern, StixFallback sets it
        // \Finv (U+2132): not in Computer Modern, StixFallback sets it
        // \scrM (U+2133): not in Computer Modern, StixFallback sets it
        // \scro (U+2134): not in Computer Modern, StixFallback sets it
        ['\u2135'] = new(Symbols, '\u0040'), // \aleph, Symbols 0x40
        // \beth (U+2136): not in Computer Modern, StixFallback sets it
        // \gimel (U+2137): not in Computer Modern, StixFallback sets it
        // \daleth (U+2138): not in Computer Modern, StixFallback sets it
        // \Game (U+2141): not in Computer Modern, StixFallback sets it
        ['\u2190'] = new(Symbols, '\u00C3'), // \leftarrow, Symbols 0xc3
        ['\u2191'] = new(Symbols, '\u0022'), // \uparrow, Symbols 0x22
        ['\u2192'] = new(Symbols, '\u0021'), // \rightarrow, Symbols 0x21
        ['\u2193'] = new(Symbols, '\u0023'), // \downarrow, Symbols 0x23
        ['\u2194'] = new(Symbols, '\u0024'), // \leftrightarrow, Symbols 0x24
        ['\u2195'] = new(Symbols, '\u006C'), // \updownarrow, Symbols 0x6c
        ['\u2196'] = new(Symbols, '\u002D'), // \nwarrow, Symbols 0x2d
        ['\u2197'] = new(Symbols, '\u0025'), // \nearrow, Symbols 0x25
        ['\u2198'] = new(Symbols, '\u0026'), // \searrow, Symbols 0x26
        ['\u2199'] = new(Symbols, '\u002E'), // \swarrow, Symbols 0x2e
        // \nleftarrow (U+219A): not in Computer Modern, StixFallback sets it
        // \nrightarrow (U+219B): not in Computer Modern, StixFallback sets it
        // \leftsquigarrow (U+219C): not in Computer Modern, StixFallback sets it
        // \rightsquigarrow (U+219D): not in Computer Modern, StixFallback sets it
        // \twoheadleftarrow (U+219E): not in Computer Modern, StixFallback sets it
        // \twoheaduparrow (U+219F): not in Computer Modern, StixFallback sets it
        // \twoheadrightarrow (U+21A0): not in Computer Modern, StixFallback sets it
        // \twoheaddownarrow (U+21A1): not in Computer Modern, StixFallback sets it
        // \leftarrowtail (U+21A2): not in Computer Modern, StixFallback sets it
        // \rightarrowtail (U+21A3): not in Computer Modern, StixFallback sets it
        // \mapsfrom (U+21A4): not in Computer Modern, StixFallback sets it
        // \mapsup (U+21A5): not in Computer Modern, StixFallback sets it
        // \mapsto (U+21A6): not in Computer Modern, StixFallback sets it
        // \mapsdown (U+21A7): not in Computer Modern, StixFallback sets it
        // \updownarrowbar (U+21A8): not in Computer Modern, StixFallback sets it
        // \hookleftarrow (U+21A9): not in Computer Modern, StixFallback sets it
        // \hookrightarrow (U+21AA): not in Computer Modern, StixFallback sets it
        // \looparrowleft (U+21AB): not in Computer Modern, StixFallback sets it
        // \looparrowright (U+21AC): not in Computer Modern, StixFallback sets it
        // \leftrightsquigarrow (U+21AD): not in Computer Modern, StixFallback sets it
        // \nleftrightarrow (U+21AE): not in Computer Modern, StixFallback sets it
        // \downzigzagarrow (U+21AF): not in Computer Modern, StixFallback sets it
        // \Lsh (U+21B0): not in Computer Modern, StixFallback sets it
        // \Rsh (U+21B1): not in Computer Modern, StixFallback sets it
        // \Ldsh (U+21B2): not in Computer Modern, StixFallback sets it
        // \Rdsh (U+21B3): not in Computer Modern, StixFallback sets it
        // \carriagereturn (U+21B5): not in Computer Modern, StixFallback sets it
        // \curvearrowleft (U+21B6): not in Computer Modern, StixFallback sets it
        // \curvearrowright (U+21B7): not in Computer Modern, StixFallback sets it
        // \acwopencirclearrow (U+21BA): not in Computer Modern, StixFallback sets it
        // \circlearrowright (U+21BB): not in Computer Modern, StixFallback sets it
        ['\u21BC'] = new(MathItalic, '\u0028'), // \leftharpoonup, MathItalic 0x28
        ['\u21BD'] = new(MathItalic, '\u0029'), // \leftharpoondown, MathItalic 0x29
        // \upharpoonright (U+21BE): not in Computer Modern, StixFallback sets it
        // \upharpoonleft (U+21BF): not in Computer Modern, StixFallback sets it
        ['\u21C0'] = new(MathItalic, '\u002A'), // \rightharpoonup, MathItalic 0x2a
        ['\u21C1'] = new(MathItalic, '\u002B'), // \rightharpoondown, MathItalic 0x2b
        // \downharpoonright (U+21C2): not in Computer Modern, StixFallback sets it
        // \downharpoonleft (U+21C3): not in Computer Modern, StixFallback sets it
        // \rightleftarrows (U+21C4): not in Computer Modern, StixFallback sets it
        // \updownarrows (U+21C5): not in Computer Modern, StixFallback sets it
        // \leftrightarrows (U+21C6): not in Computer Modern, StixFallback sets it
        // \leftleftarrows (U+21C7): not in Computer Modern, StixFallback sets it
        // \upuparrows (U+21C8): not in Computer Modern, StixFallback sets it
        // \rightrightarrows (U+21C9): not in Computer Modern, StixFallback sets it
        // \downdownarrows (U+21CA): not in Computer Modern, StixFallback sets it
        // \leftrightharpoons (U+21CB): not in Computer Modern, StixFallback sets it
        // \rightleftharpoons (U+21CC): not in Computer Modern, StixFallback sets it
        // \nLeftarrow (U+21CD): not in Computer Modern, StixFallback sets it
        // \nLeftrightarrow (U+21CE): not in Computer Modern, StixFallback sets it
        // \nRightarrow (U+21CF): not in Computer Modern, StixFallback sets it
        ['\u21D0'] = new(Symbols, '\u0028'), // \Leftarrow, Symbols 0x28
        ['\u21D1'] = new(Symbols, '\u002A'), // \Uparrow, Symbols 0x2a
        ['\u21D2'] = new(Symbols, '\u0029'), // \Rightarrow, Symbols 0x29
        ['\u21D3'] = new(Symbols, '\u002B'), // \Downarrow, Symbols 0x2b
        ['\u21D4'] = new(Symbols, '\u002C'), // \Leftrightarrow, Symbols 0x2c
        ['\u21D5'] = new(Symbols, '\u006D'), // \Updownarrow, Symbols 0x6d
        // \Nwarrow (U+21D6): not in Computer Modern, StixFallback sets it
        // \Nearrow (U+21D7): not in Computer Modern, StixFallback sets it
        // \Searrow (U+21D8): not in Computer Modern, StixFallback sets it
        // \Swarrow (U+21D9): not in Computer Modern, StixFallback sets it
        // \Lleftarrow (U+21DA): not in Computer Modern, StixFallback sets it
        // \Rrightarrow (U+21DB): not in Computer Modern, StixFallback sets it
        // \leadsto (U+21DD): not in Computer Modern, StixFallback sets it
        // \barleftarrow (U+21E4): not in Computer Modern, StixFallback sets it
        // \rightarrowbar (U+21E5): not in Computer Modern, StixFallback sets it
        ['\u2200'] = new(Symbols, '\u0038'), // \forall, Symbols 0x38
        // \complement (U+2201): not in Computer Modern, StixFallback sets it
        // \partial (U+2202): not in Computer Modern, StixFallback sets it
        ['\u2203'] = new(Symbols, '\u0039'), // \exists, Symbols 0x39
        // \nexists (U+2204): not in Computer Modern, StixFallback sets it
        ['\u2205'] = new(Symbols, '\u003B'), // \emptyset, Symbols 0x3b
        // \increment (U+2206): not in Computer Modern, StixFallback sets it
        ['\u2207'] = new(Symbols, '\u0072'), // \nabla, Symbols 0x72
        ['\u2208'] = new(Symbols, '\u0032'), // \in, Symbols 0x32
        // \notin (U+2209): not in Computer Modern, StixFallback sets it
        // \smallin (U+220A): not in Computer Modern, StixFallback sets it
        ['\u220B'] = new(Symbols, '\u0033'), // \ni, Symbols 0x33
        // \notsmallowns (U+220C): not in Computer Modern, StixFallback sets it
        // \smallowns (U+220D): not in Computer Modern, StixFallback sets it
        // \QED (U+220E): not in Computer Modern, StixFallback sets it
        ['\u220F'] = new(Extension, '\u0059'), // \prod, Extension 0x59
        ['\u2210'] = new(Extension, '\u0061'), // \coprod, Extension 0x61
        ['\u2211'] = new(Extension, '\u0058'), // \sum, Extension 0x58
        // \minus (U+2212): not in Computer Modern, StixFallback sets it
        ['\u2213'] = new(Symbols, '\u00A8'), // \mp, Symbols 0xa8
        // \dotplus (U+2214): not in Computer Modern, StixFallback sets it
        ['\u2215'] = new(Symbols, '\u0036'), // \slash, Symbols 0x36
        // \setminus (U+2216): not in Computer Modern, StixFallback sets it
        ['\u2217'] = new(Symbols, '\u00A4'), // \ast, Symbols 0xa4
        ['\u2218'] = new(Symbols, '\u00B1'), // \circ, Symbols 0xb1
        ['\u2219'] = new(Symbols, '\u00B2'), // \bullet, Symbols 0xb2
        // \__sqrt__ (U+221A): not in Computer Modern, no face holds it, the label's face
        ['\u221D'] = new(Symbols, '\u002F'), // \propto, Symbols 0x2f
        ['\u221E'] = new(Symbols, '\u0031'), // \infty, Symbols 0x31
        ['\u221F'] = new(Extension, '\u00AE'), // \rightangle, Extension 0xae
        // \angle (U+2220): not in Computer Modern, StixFallback sets it
        // \measuredangle (U+2221): not in Computer Modern, StixFallback sets it
        // \sphericalangle (U+2222): not in Computer Modern, StixFallback sets it
        ['\u2223'] = new(Symbols, '\u006A'), // \mid, Symbols 0x6a
        // \nmid (U+2224): not in Computer Modern, StixFallback sets it
        // \parallel (U+2225): not in Computer Modern, StixFallback sets it
        // \nparallel (U+2226): not in Computer Modern, StixFallback sets it
        ['\u2227'] = new(Symbols, '\u005E'), // \wedge, Symbols 0x5e
        ['\u2228'] = new(Symbols, '\u005F'), // \vee, Symbols 0x5f
        ['\u2229'] = new(Symbols, '\u005C'), // \cap, Symbols 0x5c
        ['\u222A'] = new(Symbols, '\u005B'), // \cup, Symbols 0x5b
        ['\u222B'] = new(Extension, '\u005A'), // \int, Extension 0x5a
        // \iint (U+222C): not in Computer Modern, StixFallback sets it
        // \iiint (U+222D): not in Computer Modern, StixFallback sets it
        ['\u222E'] = new(Extension, '\u0049'), // \oint, Extension 0x49
        // \oiint (U+222F): not in Computer Modern, StixFallback sets it
        // \oiiint (U+2230): not in Computer Modern, StixFallback sets it
        // \smallintclockwise (U+2231): not in Computer Modern, StixFallback sets it
        // \smallvarointclockwise (U+2232): not in Computer Modern, StixFallback sets it
        // \smallointctrcclockwise (U+2233): not in Computer Modern, StixFallback sets it
        // \therefore (U+2234): not in Computer Modern, StixFallback sets it
        // \because (U+2235): not in Computer Modern, StixFallback sets it
        // \ratio (U+2236): not in Computer Modern, StixFallback sets it
        // \Colon (U+2237): not in Computer Modern, StixFallback sets it
        // \dotminus (U+2238): not in Computer Modern, StixFallback sets it
        // \minuscolon (U+2239): not in Computer Modern, StixFallback sets it
        // \dotsminusdots (U+223A): not in Computer Modern, StixFallback sets it
        // \kernelcontraction (U+223B): not in Computer Modern, StixFallback sets it
        ['\u223C'] = new(Symbols, '\u00BB'), // \sim, Symbols 0xbb
        // \backsim (U+223D): not in Computer Modern, StixFallback sets it
        // \ac (U+223E): not in Computer Modern, StixFallback sets it
        // \sinewave (U+223F): not in Computer Modern, StixFallback sets it
        ['\u2240'] = new(Symbols, '\u006F'), // \wr, Symbols 0x6f
        // \nsim (U+2241): not in Computer Modern, StixFallback sets it
        // \eqsim (U+2242): not in Computer Modern, StixFallback sets it
        ['\u2243'] = new(Symbols, '\u0027'), // \simeq, Symbols 0x27
        // \nsimeq (U+2244): not in Computer Modern, StixFallback sets it
        // \cong (U+2245): not in Computer Modern, StixFallback sets it
        // \simneqq (U+2246): not in Computer Modern, StixFallback sets it
        // \ncong (U+2247): not in Computer Modern, StixFallback sets it
        ['\u2248'] = new(Symbols, '\u00BC'), // \approx, Symbols 0xbc
        // \napprox (U+2249): not in Computer Modern, StixFallback sets it
        // \approxeq (U+224A): not in Computer Modern, StixFallback sets it
        // \approxident (U+224B): not in Computer Modern, StixFallback sets it
        // \backcong (U+224C): not in Computer Modern, StixFallback sets it
        ['\u224D'] = new(Symbols, '\u00B3'), // \asymp, Symbols 0xb3
        // \Bumpeq (U+224E): not in Computer Modern, StixFallback sets it
        // \bumpeq (U+224F): not in Computer Modern, StixFallback sets it
        // \doteq (U+2250): not in Computer Modern, StixFallback sets it
        // \Doteq (U+2251): not in Computer Modern, StixFallback sets it
        // \fallingdotseq (U+2252): not in Computer Modern, StixFallback sets it
        // \risingdotseq (U+2253): not in Computer Modern, StixFallback sets it
        // \coloneq (U+2254): not in Computer Modern, StixFallback sets it
        // \eqcolon (U+2255): not in Computer Modern, StixFallback sets it
        // \eqcirc (U+2256): not in Computer Modern, StixFallback sets it
        // \circeq (U+2257): not in Computer Modern, StixFallback sets it
        // \arceq (U+2258): not in Computer Modern, StixFallback sets it
        // \wedgeq (U+2259): not in Computer Modern, StixFallback sets it
        // \veeeq (U+225A): not in Computer Modern, StixFallback sets it
        // \stareq (U+225B): not in Computer Modern, StixFallback sets it
        // \triangleeq (U+225C): not in Computer Modern, StixFallback sets it
        // \eqdef (U+225D): not in Computer Modern, StixFallback sets it
        // \measeq (U+225E): not in Computer Modern, StixFallback sets it
        // \questeq (U+225F): not in Computer Modern, StixFallback sets it
        // \ne (U+2260): not in Computer Modern, StixFallback sets it
        ['\u2261'] = new(Symbols, '\u00B4'), // \equiv, Symbols 0xb4
        // \nequiv (U+2262): not in Computer Modern, StixFallback sets it
        // \Equiv (U+2263): not in Computer Modern, StixFallback sets it
        ['\u2264'] = new(Symbols, '\u2219'), // \leq, Symbols 0x2219
        ['\u2265'] = new(Symbols, '\u00B8'), // \geq, Symbols 0xb8
        // \leqq (U+2266): not in Computer Modern, StixFallback sets it
        // \geqq (U+2267): not in Computer Modern, StixFallback sets it
        // \lneqq (U+2268): not in Computer Modern, StixFallback sets it
        // \gneqq (U+2269): not in Computer Modern, StixFallback sets it
        ['\u226A'] = new(Symbols, '\u00BF'), // \ll, Symbols 0xbf
        ['\u226B'] = new(Symbols, '\u00C0'), // \gg, Symbols 0xc0
        // \between (U+226C): not in Computer Modern, StixFallback sets it
        // \nless (U+226E): not in Computer Modern, StixFallback sets it
        // \ngtr (U+226F): not in Computer Modern, StixFallback sets it
        // \nleq (U+2270): not in Computer Modern, StixFallback sets it
        // \ngeq (U+2271): not in Computer Modern, StixFallback sets it
        // \lesssim (U+2272): not in Computer Modern, StixFallback sets it
        // \gtrsim (U+2273): not in Computer Modern, StixFallback sets it
        // \nlesssim (U+2274): not in Computer Modern, StixFallback sets it
        // \ngtrsim (U+2275): not in Computer Modern, StixFallback sets it
        // \lessgtr (U+2276): not in Computer Modern, StixFallback sets it
        // \gtrless (U+2277): not in Computer Modern, StixFallback sets it
        // \nlessgtr (U+2278): not in Computer Modern, StixFallback sets it
        // \ngtrless (U+2279): not in Computer Modern, StixFallback sets it
        ['\u227A'] = new(Symbols, '\u00C1'), // \prec, Symbols 0xc1
        ['\u227B'] = new(Symbols, '\u00C2'), // \succ, Symbols 0xc2
        ['\u227C'] = new(Symbols, '\u00B9'), // \preceq, Symbols 0xb9
        ['\u227D'] = new(Symbols, '\u00BA'), // \succeq, Symbols 0xba
        // \precsim (U+227E): not in Computer Modern, StixFallback sets it
        // \succsim (U+227F): not in Computer Modern, StixFallback sets it
        // \nprec (U+2280): not in Computer Modern, StixFallback sets it
        // \nsucc (U+2281): not in Computer Modern, StixFallback sets it
        ['\u2282'] = new(Symbols, '\u00BD'), // \subset, Symbols 0xbd
        ['\u2283'] = new(Symbols, '\u00BE'), // \supset, Symbols 0xbe
        // \nsubset (U+2284): not in Computer Modern, StixFallback sets it
        // \nsupset (U+2285): not in Computer Modern, StixFallback sets it
        ['\u2286'] = new(Symbols, '\u00B5'), // \subseteq, Symbols 0xb5
        ['\u2287'] = new(Symbols, '\u00B6'), // \supseteq, Symbols 0xb6
        // \nsubseteq (U+2288): not in Computer Modern, StixFallback sets it
        // \nsupseteq (U+2289): not in Computer Modern, StixFallback sets it
        // \subsetneq (U+228A): not in Computer Modern, StixFallback sets it
        // \supsetneq (U+228B): not in Computer Modern, StixFallback sets it
        // \cupleftarrow (U+228C): not in Computer Modern, StixFallback sets it
        // \cupdot (U+228D): not in Computer Modern, StixFallback sets it
        ['\u228E'] = new(Symbols, '\u005D'), // \uplus, Symbols 0x5d
        // \sqsubset (U+228F): not in Computer Modern, StixFallback sets it
        // \sqsupset (U+2290): not in Computer Modern, StixFallback sets it
        ['\u2291'] = new(Symbols, '\u0076'), // \sqsubseteq, Symbols 0x76
        ['\u2292'] = new(Symbols, '\u0077'), // \sqsupseteq, Symbols 0x77
        ['\u2293'] = new(Symbols, '\u0075'), // \sqcap, Symbols 0x75
        ['\u2294'] = new(Symbols, '\u0074'), // \sqcup, Symbols 0x74
        ['\u2295'] = new(Symbols, '\u00A9'), // \oplus, Symbols 0xa9
        ['\u2296'] = new(Symbols, '\u00AA'), // \ominus, Symbols 0xaa
        ['\u2297'] = new(Symbols, '\u00AD'), // \otimes, Symbols 0xad
        ['\u2298'] = new(Symbols, '\u00AE'), // \oslash, Symbols 0xae
        ['\u2299'] = new(Symbols, '\u00AF'), // \odot, Symbols 0xaf
        // \circledcirc (U+229A): not in Computer Modern, StixFallback sets it
        // \circledast (U+229B): not in Computer Modern, StixFallback sets it
        // \oequal (U+229C): not in Computer Modern, StixFallback sets it
        // \circleddash (U+229D): not in Computer Modern, StixFallback sets it
        // \boxplus (U+229E): not in Computer Modern, StixFallback sets it
        // \boxminus (U+229F): not in Computer Modern, StixFallback sets it
        // \boxtimes (U+22A0): not in Computer Modern, StixFallback sets it
        // \boxdot (U+22A1): not in Computer Modern, StixFallback sets it
        ['\u22A2'] = new(Symbols, '\u0060'), // \vdash, Symbols 0x60
        ['\u22A3'] = new(Symbols, '\u0061'), // \dashv, Symbols 0x61
        ['\u22A4'] = new(Symbols, '\u003E'), // \top, Symbols 0x3e
        ['\u22A5'] = new(Symbols, '\u003F'), // \bot, Symbols 0x3f
        // \rightassert (U+22A6): not in Computer Modern, StixFallback sets it
        // \models (U+22A7): not in Computer Modern, StixFallback sets it
        // \vDash (U+22A8): not in Computer Modern, StixFallback sets it
        // \Vdash (U+22A9): not in Computer Modern, StixFallback sets it
        // \Vvdash (U+22AA): not in Computer Modern, StixFallback sets it
        // \rightModels (U+22AB): not in Computer Modern, StixFallback sets it
        // \nvdash (U+22AC): not in Computer Modern, StixFallback sets it
        // \nvDash (U+22AD): not in Computer Modern, StixFallback sets it
        // \nVdash (U+22AE): not in Computer Modern, StixFallback sets it
        // \nVDash (U+22AF): not in Computer Modern, StixFallback sets it
        // \prurel (U+22B0): not in Computer Modern, StixFallback sets it
        // \scurel (U+22B1): not in Computer Modern, StixFallback sets it
        // \vartriangleleft (U+22B2): not in Computer Modern, StixFallback sets it
        // \vartriangleright (U+22B3): not in Computer Modern, StixFallback sets it
        // \trianglelefteq (U+22B4): not in Computer Modern, StixFallback sets it
        // \trianglerighteq (U+22B5): not in Computer Modern, StixFallback sets it
        // \origof (U+22B6): not in Computer Modern, StixFallback sets it
        // \imageof (U+22B7): not in Computer Modern, StixFallback sets it
        // \multimap (U+22B8): not in Computer Modern, StixFallback sets it
        // \hermitmatrix (U+22B9): not in Computer Modern, StixFallback sets it
        // \intercal (U+22BA): not in Computer Modern, StixFallback sets it
        // \veebar (U+22BB): not in Computer Modern, StixFallback sets it
        // \barwedge (U+22BC): not in Computer Modern, StixFallback sets it
        // \barvee (U+22BD): not in Computer Modern, StixFallback sets it
        // \measuredrightangle (U+22BE): not in Computer Modern, StixFallback sets it
        // \varlrtriangle (U+22BF): not in Computer Modern, StixFallback sets it
        ['\u22C0'] = new(Extension, '\u005E'), // \bigwedge, Extension 0x5e
        ['\u22C1'] = new(Extension, '\u005F'), // \bigvee, Extension 0x5f
        ['\u22C2'] = new(Extension, '\u005C'), // \bigcap, Extension 0x5c
        ['\u22C3'] = new(Extension, '\u005B'), // \bigcup, Extension 0x5b
        ['\u22C4'] = new(Symbols, '\u00A6'), // \diamond, Symbols 0xa6
        ['\u22C5'] = new(Symbols, '\u00A2'), // \cdot, Symbols 0xa2
        // \star (U+22C6): not in Computer Modern, StixFallback sets it
        // \divideontimes (U+22C7): not in Computer Modern, StixFallback sets it
        // \bowtie (U+22C8): not in Computer Modern, StixFallback sets it
        // \ltimes (U+22C9): not in Computer Modern, StixFallback sets it
        // \rtimes (U+22CA): not in Computer Modern, StixFallback sets it
        // \leftthreetimes (U+22CB): not in Computer Modern, StixFallback sets it
        // \rightthreetimes (U+22CC): not in Computer Modern, StixFallback sets it
        // \backsimeq (U+22CD): not in Computer Modern, StixFallback sets it
        // \curlyvee (U+22CE): not in Computer Modern, StixFallback sets it
        // \curlywedge (U+22CF): not in Computer Modern, StixFallback sets it
        // \Subset (U+22D0): not in Computer Modern, StixFallback sets it
        // \Supset (U+22D1): not in Computer Modern, StixFallback sets it
        // \Cap (U+22D2): not in Computer Modern, StixFallback sets it
        // \Cup (U+22D3): not in Computer Modern, StixFallback sets it
        // \pitchfork (U+22D4): not in Computer Modern, StixFallback sets it
        // \equalparallel (U+22D5): not in Computer Modern, StixFallback sets it
        // \lessdot (U+22D6): not in Computer Modern, StixFallback sets it
        // \gtrdot (U+22D7): not in Computer Modern, StixFallback sets it
        // \lll (U+22D8): not in Computer Modern, StixFallback sets it
        // \ggg (U+22D9): not in Computer Modern, StixFallback sets it
        // \lesseqgtr (U+22DA): not in Computer Modern, StixFallback sets it
        // \gtreqless (U+22DB): not in Computer Modern, StixFallback sets it
        // \eqless (U+22DC): not in Computer Modern, StixFallback sets it
        // \eqgtr (U+22DD): not in Computer Modern, StixFallback sets it
        // \curlyeqprec (U+22DE): not in Computer Modern, StixFallback sets it
        // \curlyeqsucc (U+22DF): not in Computer Modern, StixFallback sets it
        // \npreccurlyeq (U+22E0): not in Computer Modern, StixFallback sets it
        // \nsucccurlyeq (U+22E1): not in Computer Modern, StixFallback sets it
        // \nsqsubseteq (U+22E2): not in Computer Modern, StixFallback sets it
        // \nsqsupseteq (U+22E3): not in Computer Modern, StixFallback sets it
        // \sqsubsetneq (U+22E4): not in Computer Modern, StixFallback sets it
        // \sqsupsetneq (U+22E5): not in Computer Modern, StixFallback sets it
        // \lnsim (U+22E6): not in Computer Modern, StixFallback sets it
        // \gnsim (U+22E7): not in Computer Modern, StixFallback sets it
        // \precnsim (U+22E8): not in Computer Modern, StixFallback sets it
        // \succnsim (U+22E9): not in Computer Modern, StixFallback sets it
        // \ntriangleleft (U+22EA): not in Computer Modern, StixFallback sets it
        // \ntriangleright (U+22EB): not in Computer Modern, StixFallback sets it
        // \ntrianglelefteq (U+22EC): not in Computer Modern, StixFallback sets it
        // \ntrianglerighteq (U+22ED): not in Computer Modern, StixFallback sets it
        // \vdots (U+22EE): not in Computer Modern, StixFallback sets it
        // \cdots (U+22EF): not in Computer Modern, StixFallback sets it
        // \adots (U+22F0): not in Computer Modern, StixFallback sets it
        // \ddots (U+22F1): not in Computer Modern, StixFallback sets it
        // \disin (U+22F2): not in Computer Modern, StixFallback sets it
        // \varisins (U+22F3): not in Computer Modern, StixFallback sets it
        // \isins (U+22F4): not in Computer Modern, StixFallback sets it
        // \isindot (U+22F5): not in Computer Modern, StixFallback sets it
        // \varisinobar (U+22F6): not in Computer Modern, StixFallback sets it
        // \isinobar (U+22F7): not in Computer Modern, StixFallback sets it
        // \isinvb (U+22F8): not in Computer Modern, StixFallback sets it
        // \isinE (U+22F9): not in Computer Modern, StixFallback sets it
        // \nisd (U+22FA): not in Computer Modern, StixFallback sets it
        // \varnis (U+22FB): not in Computer Modern, StixFallback sets it
        // \nis (U+22FC): not in Computer Modern, StixFallback sets it
        // \varniobar (U+22FD): not in Computer Modern, StixFallback sets it
        // \niobar (U+22FE): not in Computer Modern, StixFallback sets it
        // \bagmember (U+22FF): not in Computer Modern, StixFallback sets it
        // \doublebarwedge (U+2306): not in Computer Modern, StixFallback sets it
        ['\u2308'] = new(Symbols, '\u0064'), // \lceil, Symbols 0x64
        ['\u2309'] = new(Symbols, '\u0065'), // \rceil, Symbols 0x65
        ['\u230A'] = new(Symbols, '\u0062'), // \lfloor, Symbols 0x62
        ['\u230B'] = new(Symbols, '\u0063'), // \rfloor, Symbols 0x63
        // \invnot (U+2310): not in Computer Modern, StixFallback sets it
        // \turnednot (U+2319): not in Computer Modern, StixFallback sets it
        // \ulcorner (U+231C): not in Computer Modern, StixFallback sets it
        // \urcorner (U+231D): not in Computer Modern, StixFallback sets it
        // \llcorner (U+231E): not in Computer Modern, StixFallback sets it
        // \lrcorner (U+231F): not in Computer Modern, StixFallback sets it
        ['\u2322'] = new(MathItalic, '\u005F'), // \frown, MathItalic 0x5f
        ['\u2323'] = new(MathItalic, '\u005E'), // \smile, MathItalic 0x5e
        // \obar (U+233D): not in Computer Modern, StixFallback sets it
        // \solbar (U+233F): not in Computer Modern, StixFallback sets it
        // \circledS (U+24C8): not in Computer Modern, StixFallback sets it
        // \blacksquare (U+25A0): not in Computer Modern, StixFallback sets it
        ['\u25B3'] = new(Symbols, '\u0034'), // \bigtriangleup, Symbols 0x34
        // \blacktriangle (U+25B4): not in Computer Modern, StixFallback sets it
        // \vartriangle (U+25B5): not in Computer Modern, StixFallback sets it
        // \blacktriangleright (U+25B6): not in Computer Modern, StixFallback sets it
        ['\u25B7'] = new(MathItalic, '\u002E'), // \triangleright, MathItalic 0x2e
        ['\u25BD'] = new(Symbols, '\u0035'), // \bigtriangledown, Symbols 0x35
        // \blacktriangledown (U+25BE): not in Computer Modern, StixFallback sets it
        // \triangledown (U+25BF): not in Computer Modern, StixFallback sets it
        // \blacktriangleleft (U+25C0): not in Computer Modern, StixFallback sets it
        ['\u25C1'] = new(MathItalic, '\u002F'), // \triangleleft, MathItalic 0x2f
        ['\u25CB'] = new(Symbols, '\u00B0'), // \bigcirc, Symbols 0xb0
        // \boxbar (U+25EB): not in Computer Modern, StixFallback sets it
        // \bigstar (U+2605): not in Computer Modern, StixFallback sets it
        // \danger (U+2621): not in Computer Modern, StixFallback sets it
        ['\u2660'] = new(Symbols, '\u00C4'), // \spadesuit, Symbols 0xc4
        ['\u2661'] = new(Symbols, '\u007E'), // \heartsuit, Symbols 0x7e
        ['\u2662'] = new(Symbols, '\u007D'), // \diamondsuit, Symbols 0x7d
        ['\u2663'] = new(Symbols, '\u007C'), // \clubsuit, Symbols 0x7c
        // \spadesuitopen (U+2664): not in Computer Modern, StixFallback sets it
        // \clubsuitopen (U+2667): not in Computer Modern, StixFallback sets it
        ['\u266D'] = new(MathItalic, '\u005B'), // \flat, MathItalic 0x5b
        ['\u266E'] = new(MathItalic, '\u005C'), // \natural, MathItalic 0x5c
        ['\u266F'] = new(MathItalic, '\u005D'), // \sharp, MathItalic 0x5d
        // \checkmark (U+2713): not in Computer Modern, StixFallback sets it
        // \maltese (U+2720): not in Computer Modern, StixFallback sets it
        // \perp (U+27C2): not in Computer Modern, StixFallback sets it
        ['\u27E8'] = new(Extension, '\u00AD'), // \langle, Extension 0xad
        ['\u27E9'] = new(Extension, '\u00AE'), // \rangle, Extension 0xae
        // \lgroup (U+27EE): not in Computer Modern, StixFallback sets it
        // \rgroup (U+27EF): not in Computer Modern, StixFallback sets it
        // \longleftarrow (U+27F5): not in Computer Modern, StixFallback sets it
        // \longrightarrow (U+27F6): not in Computer Modern, StixFallback sets it
        // \longleftrightarrow (U+27F7): not in Computer Modern, StixFallback sets it
        // \Longleftarrow (U+27F8): not in Computer Modern, StixFallback sets it
        // \Longrightarrow (U+27F9): not in Computer Modern, StixFallback sets it
        // \Longleftrightarrow (U+27FA): not in Computer Modern, StixFallback sets it
        // \longmapsto (U+27FC): not in Computer Modern, StixFallback sets it
        // \dashleftarrow (U+290E): not in Computer Modern, StixFallback sets it
        // \dashrightarrow (U+290F): not in Computer Modern, StixFallback sets it
        ['\u2A00'] = new(Extension, '\u004B'), // \bigodot, Extension 0x4b
        ['\u2A01'] = new(Extension, '\u004D'), // \bigoplus, Extension 0x4d
        ['\u2A02'] = new(Extension, '\u004F'), // \bigotimes, Extension 0x4f
        ['\u2A04'] = new(Extension, '\u005D'), // \biguplus, Extension 0x5d
        // \bigsqcup (U+2A06): not in Computer Modern, StixFallback sets it
        // \iiiint (U+2A0C): not in Computer Modern, StixFallback sets it
        // \Join (U+2A1D): not in Computer Modern, StixFallback sets it
        // \amalg (U+2A3F): not in Computer Modern, StixFallback sets it
        // \merge (U+2A55): not in Computer Modern, StixFallback sets it
        // \leqslant (U+2A7D): not in Computer Modern, StixFallback sets it
        // \geqslant (U+2A7E): not in Computer Modern, StixFallback sets it
        // \lessapprox (U+2A85): not in Computer Modern, StixFallback sets it
        // \gtrapprox (U+2A86): not in Computer Modern, StixFallback sets it
        // \lnapprox (U+2A89): not in Computer Modern, StixFallback sets it
        // \gnapprox (U+2A8A): not in Computer Modern, StixFallback sets it
        // \lesseqqgtr (U+2A8B): not in Computer Modern, StixFallback sets it
        // \gtreqqless (U+2A8C): not in Computer Modern, StixFallback sets it
        // \eqslantless (U+2A95): not in Computer Modern, StixFallback sets it
        // \eqslantgtr (U+2A96): not in Computer Modern, StixFallback sets it
        // \precapprox (U+2AB7): not in Computer Modern, StixFallback sets it
        // \succapprox (U+2AB8): not in Computer Modern, StixFallback sets it
        // \precnapprox (U+2AB9): not in Computer Modern, StixFallback sets it
        // \succnapprox (U+2ABA): not in Computer Modern, StixFallback sets it
        // \subseteqq (U+2AC5): not in Computer Modern, StixFallback sets it
        // \supseteqq (U+2AC6): not in Computer Modern, StixFallback sets it
        // \subsetneqq (U+2ACB): not in Computer Modern, StixFallback sets it
        // \supsetneqq (U+2ACC): not in Computer Modern, StixFallback sets it
    }.ToFrozenDictionary();

    /// <summary>The glyphs matplotlib's AutoHeightChar chooses among to grow each delimiter and the radical sign under 'cm', size 0
    /// first (<c>BakomaFonts.get_sized_alternatives_for_symbol</c>), by the character the layout hands it.</summary>
    private static FrozenDictionary<char, MathSizes> SizedTable() => new Dictionary<char, MathSizes>
    {
        ['\u0028'] = new([new(Roman, 0x28), new(Extension, 0xA1), new(Extension, 0xB3), new(Extension, 0xB5), new(Extension, 0xC3)], false),
        ['\u0029'] = new([new(Roman, 0x29), new(Extension, 0xA2), new(Extension, 0xB4), new(Extension, 0xB6), new(Extension, 0x21)], false),
        ['\u002F'] = new([new(MathItalic, 0x3D), new(Extension, 0xB1), new(Extension, 0x2E), new(Extension, 0xC1), new(Extension, 0x2C)], false),
        ['\u003C'] = new([new(Symbols, 0x68), new(Extension, 0xAD), new(Extension, 0x44), new(Extension, 0xBF), new(Extension, 0x2A)], false),
        ['\u003E'] = new([new(Symbols, 0x69), new(Extension, 0xAE), new(Extension, 0x45), new(Extension, 0xC0), new(Extension, 0x2B)], false),
        ['\u005B'] = new([new(Roman, 0x5B), new(Extension, 0xA3), new(Extension, 0x68), new(Extension, 0x2219), new(Extension, 0x22)], false),
        ['\u21D3'] = new([new(Symbols, 0x2B)], false),
        ['\u21D1'] = new([new(Symbols, 0x2A)], false),
        ['\u21D5'] = new([new(Symbols, 0x6D)], false),
        ['\u2016'] = new([new(Symbols, 0x6B)], false),
        ['\u005C'] = new([new(Extension, 0xB2), new(Extension, 0x2F), new(Extension, 0xC2), new(Extension, 0x2D)], false),
        ['\u2193'] = new([new(Symbols, 0x23)], false),
        ['\u27E8'] = new([new(Symbols, 0x68), new(Extension, 0xAD), new(Extension, 0x44), new(Extension, 0xBF), new(Extension, 0x2A)], false),
        ['\u007B'] = new([new(Extension, 0xA9), new(Extension, 0x6E), new(Extension, 0xBD), new(Extension, 0x28)], false),
        ['\u2308'] = new([new(Extension, 0xA7), new(Extension, 0x6C), new(Extension, 0xBB), new(Extension, 0x26)], false),
        ['\u230A'] = new([new(Extension, 0xA5), new(Extension, 0x6A), new(Extension, 0xB9), new(Extension, 0x24)], false),
        ['\u27EE'] = new([new(StixFallback.FaceOf(StixFace.Regular), 0x27EE)], false),
        ['\u27E9'] = new([new(Symbols, 0x69), new(Extension, 0xAE), new(Extension, 0x45), new(Extension, 0xC0), new(Extension, 0x2B)], false),
        ['\u007D'] = new([new(Extension, 0xAA), new(Extension, 0x6F), new(Extension, 0xBE), new(Extension, 0x29)], false),
        ['\u005D'] = new([new(Roman, 0x5D), new(Extension, 0xA4), new(Extension, 0x69), new(Extension, 0xB8), new(Extension, 0x23)], false),
        ['\u2309'] = new([new(Extension, 0xA8), new(Extension, 0x6D), new(Extension, 0xBC), new(Extension, 0x27)], false),
        ['\u230B'] = new([new(Extension, 0xA6), new(Extension, 0x6B), new(Extension, 0xBA), new(Extension, 0x25)], false),
        ['\u27EF'] = new([new(StixFallback.FaceOf(StixFace.Regular), 0x27EF)], false),
        ['\u2191'] = new([new(Symbols, 0x22)], false),
        ['\u2195'] = new([new(Symbols, 0x6C)], false),
        ['\u007C'] = new([new(Symbols, 0x6A)], false),
        ['\u221A'] = new([new(Extension, 0x70), new(Extension, 0x71), new(Extension, 0x72), new(Extension, 0x73)], false),
    }.ToFrozenDictionary();
}
