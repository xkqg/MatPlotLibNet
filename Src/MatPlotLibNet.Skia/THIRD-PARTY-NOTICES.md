# Third-party notices

MatPlotLibNet.Skia carries third-party material of three kinds inside its assembly, embedded unchanged as resources: the typefaces
of the DejaVu Sans family, so that a chart is measured and drawn with the same font on every machine (DejaVu Sans Display is
the face matplotlib sets large operators in under its default style), the Computer Modern typefaces that matplotlib sets math in
under its classic style, so that math under that style is drawn in them on every machine, and the STIX typefaces that matplotlib
takes the math characters those faces have no glyph for from (a not-equal sign under the classic style, a script or a double-struck
letter under either) and the taller sizes it grows a delimiter or a radical sign in under its default style, so that they are drawn in
them on every machine.

| File | Typeface |
|---|---|
| DejaVuSans.ttf | DejaVu Sans |
| DejaVuSans-Bold.ttf | DejaVu Sans Bold |
| DejaVuSans-Oblique.ttf | DejaVu Sans Oblique |
| DejaVuSans-BoldOblique.ttf | DejaVu Sans Bold Oblique |
| DejaVuSansDisplay.ttf | DejaVu Sans Display |
| cmr10.ttf | Computer Modern Roman (cmr10) |
| cmmi10.ttf | Computer Modern Math Italic (cmmi10) |
| cmti10.ttf | Computer Modern Text Italic (cmti10) |
| cmb10.ttf | Computer Modern Bold (cmb10) |
| cmsy10.ttf | Computer Modern Math Symbols (cmsy10) |
| cmex10.ttf | Computer Modern Math Extension (cmex10) |
| cmtt10.ttf | Computer Modern Typewriter (cmtt10) |
| STIXGeneral.ttf | STIXGeneral Regular |
| STIXGeneralItalic.ttf | STIXGeneral Italic |
| STIXGeneralBol.ttf | STIXGeneral Bold |
| STIXNonUni.ttf | STIXNonUnicode Regular |
| STIXNonUniIta.ttf | STIXNonUnicode Italic |
| STIXSizOneSymReg.ttf | STIXSizeOneSym Regular |
| STIXSizTwoSymReg.ttf | STIXSizeTwoSym Regular |
| STIXSizThreeSymReg.ttf | STIXSizeThreeSym Regular |
| STIXSizFourSymReg.ttf | STIXSizeFourSym Regular |

The DejaVu fonts are (c) Bitstream, Inc. (Bitstream Vera) and Tavmjong Bah (glyphs imported from Arev); the changes made by the
DejaVu project are in the public domain. The DejaVu fonts licence below applies to them, and its permission notice has to stay with
every copy of the fonts.

The Computer Modern fonts are the TrueType fonts of the BaKoMa Fonts Collection by Basil K. Malyshev, as the matplotlib project ships
them in its mpl-data/fonts/ttf folder. The original fonts are at http://www.ctan.org/tex-archive/fonts/cm/ps-type1/bakoma. The BaKoMa
fonts licence below applies to them, and its copyright notice, author statement and permission notice have to stay with every copy
of the fonts.

The STIX fonts are (c) 2001-2010 by the STI Pub Companies (the American Institute of Physics, the American Chemical Society, the
American Mathematical Society, the American Physical Society, Elsevier, Inc. and the Institute of Electrical and Electronics
Engineers, Inc., www.stixfonts.org), as the matplotlib project ships them in its mpl-data/fonts/ttf folder, converted from OTF to TTF
(the note at the head of the licence below says how). The STIX fonts licence below, the SIL Open Font License, applies to them, and
has to stay with every copy of the fonts.

SkiaSharp and HarfBuzzSharp are not part of this package: NuGet restores them as dependencies, each with its own licence
and notices.

## DejaVu fonts licence

~~~~text
Fonts are (c) Bitstream (see below). DejaVu changes are in public domain.
Glyphs imported from Arev fonts are (c) Tavmjong Bah (see below)

Bitstream Vera Fonts Copyright
------------------------------

Copyright (c) 2003 by Bitstream, Inc. All Rights Reserved. Bitstream Vera is
a trademark of Bitstream, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of the fonts accompanying this license ("Fonts") and associated
documentation files (the "Font Software"), to reproduce and distribute the
Font Software, including without limitation the rights to use, copy, merge,
publish, distribute, and/or sell copies of the Font Software, and to permit
persons to whom the Font Software is furnished to do so, subject to the
following conditions:

The above copyright and trademark notices and this permission notice shall
be included in all copies of one or more of the Font Software typefaces.

The Font Software may be modified, altered, or added to, and in particular
the designs of glyphs or characters in the Fonts may be modified and
additional glyphs or characters may be added to the Fonts, only if the fonts
are renamed to names not containing either the words "Bitstream" or the word
"Vera".

This License becomes null and void to the extent applicable to Fonts or Font
Software that has been modified and is distributed under the "Bitstream
Vera" names.

The Font Software may be sold as part of a larger software package but no
copy of one or more of the Font Software typefaces may be sold by itself.

THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF COPYRIGHT, PATENT,
TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL BITSTREAM OR THE GNOME
FOUNDATION BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, INCLUDING
ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM OTHER DEALINGS IN THE
FONT SOFTWARE.

Except as contained in this notice, the names of Gnome, the Gnome
Foundation, and Bitstream Inc., shall not be used in advertising or
otherwise to promote the sale, use or other dealings in this Font Software
without prior written authorization from the Gnome Foundation or Bitstream
Inc., respectively. For further information, contact: fonts at gnome dot
org. 

Arev Fonts Copyright
------------------------------

Copyright (c) 2006 by Tavmjong Bah. All Rights Reserved.

Permission is hereby granted, free of charge, to any person obtaining
a copy of the fonts accompanying this license ("Fonts") and
associated documentation files (the "Font Software"), to reproduce
and distribute the modifications to the Bitstream Vera Font Software,
including without limitation the rights to use, copy, merge, publish,
distribute, and/or sell copies of the Font Software, and to permit
persons to whom the Font Software is furnished to do so, subject to
the following conditions:

The above copyright and trademark notices and this permission notice
shall be included in all copies of one or more of the Font Software
typefaces.

The Font Software may be modified, altered, or added to, and in
particular the designs of glyphs or characters in the Fonts may be
modified and additional glyphs or characters may be added to the
Fonts, only if the fonts are renamed to names not containing either
the words "Tavmjong Bah" or the word "Arev".

This License becomes null and void to the extent applicable to Fonts
or Font Software that has been modified and is distributed under the 
"Tavmjong Bah Arev" names.

The Font Software may be sold as part of a larger software package but
no copy of one or more of the Font Software typefaces may be sold by
itself.

THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL
TAVMJONG BAH BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.

Except as contained in this notice, the name of Tavmjong Bah shall not
be used in advertising or otherwise to promote the sale, use or other
dealings in this Font Software without prior written authorization
from Tavmjong Bah. For further information, contact: tavmjong @ free
. fr.

$Id: LICENSE 2133 2007-11-28 02:46:28Z lechimp $
~~~~

## BaKoMa fonts licence

~~~~text

			BaKoMa Fonts Licence
			--------------------

  This licence covers two font packs (known as BaKoMa Fonts Collection,
  which is available at `CTAN:fonts/cm/ps-type1/bakoma/'):

    1) BaKoMa-CM (1.1/12-Nov-94)
       Computer Modern Fonts in PostScript Type 1 and TrueType font formats.

    2) BaKoMa-AMS (1.2/19-Jan-95)
       AMS TeX fonts in PostScript Type 1 and TrueType font formats.
   
  Copyright (C) 1994, 1995, Basil K. Malyshev. All Rights Reserved.

  Permission to copy and distribute these fonts for any purpose is 
  hereby granted without fee, provided that the above copyright notice, 
  author statement and this permission notice appear in all copies of 
  these fonts and related documentation.

  Permission to modify and distribute modified fonts for any purpose is 
  hereby granted without fee, provided that the copyright notice, 
  author statement, this permission notice and location of original 
  fonts (http://www.ctan.org/tex-archive/fonts/cm/ps-type1/bakoma)
  appear in all copies of modified fonts and related documentation.

  Permission to use these fonts (embedding into PostScript, PDF, SVG
  and printing by using any software) is hereby granted without fee. 
  It is not required to provide any notices about using these fonts.

 Basil K. Malyshev
 INSTITUTE FOR HIGH ENERGY PHYSICS
 IHEP, OMVT
 Moscow Region
 142281 PROTVINO
 RUSSIA

 E-Mail:	bakoma@mail.ru
      or	malyshev@mail.ihep.ru
~~~~

## STIX fonts licence

~~~~text
The STIX fonts distributed with matplotlib have been modified from
their canonical form.  They have been converted from OTF to TTF format
using Fontforge and this script:

  #!/usr/bin/env fontforge
  i=1
  while ( i<$argc )
    Open($argv[i])
    Generate($argv[i]:r + ".ttf")
    i = i+1
  endloop

The original STIX Font License begins below.

-----------------------------------------------------------

STIX Font License

24 May 2010

Copyright (c) 2001-2010 by the STI Pub Companies, consisting of the American
Institute of Physics, the American Chemical Society, the American Mathematical
Society, the American Physical Society, Elsevier, Inc., and The Institute of
Electrical and Electronic Engineers, Inc. (www.stixfonts.org), with Reserved
Font Name STIX Fonts, STIX Fonts (TM) is a  trademark of The Institute of
Electrical and Electronics Engineers, Inc.

Portions copyright (c) 1998-2003 by MicroPress, Inc. (www.micropress-inc.com),
with Reserved Font Name TM Math. To obtain additional mathematical fonts, please
contact MicroPress, Inc., 68-30 Harrow Street, Forest Hills, NY 11375, USA,
Phone: (718) 575-1816.

Portions copyright (c) 1990 by Elsevier, Inc.

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
https://scripts.sil.org/OFL

-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded,
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
~~~~
