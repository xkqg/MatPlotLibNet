#!/usr/bin/env python3
"""Give every built page one address and a link preview.

docfx writes a title and a description into each page and stops there. Two things are missing from the
result, and neither can be added from docfx's own configuration: a page reachable at two addresses never
says which one is the real one, and a link posted anywhere renders as a bare URL instead of a card.

Measured against docfx 2.78.5 with the default+modern template: `_canonicalUrlPrefix` in globalMetadata
emits nothing at all, and a frontmatter key the template does not know (og_title, image, keywords) is
dropped without a warning. The only other way in is to fork the template's head, which means owning a
copy of docfx's page layout forever. This runs over the built site instead, so it is coupled to nothing
but the presence of a </head>.

    python3 tools/seo/page-metadata.py --self-test
    python3 tools/seo/page-metadata.py --site docs/_site --base-url https://example.org/x/ --apply
    python3 tools/seo/page-metadata.py --site docs/_site --base-url https://example.org/x/ --verify
"""

import argparse
import os
import re
import sys

CARD_PATH = "images/social-card.png"
SITE_NAME = "MatPlotLibNet"

_TITLE_META = re.compile(r'<meta name="title" content="([^"]*)"', re.IGNORECASE)
_TITLE_TAG = re.compile(r"<title>(.*?)</title>", re.IGNORECASE | re.DOTALL)
_DESCRIPTION = re.compile(r'<meta name="description" content="([^"]*)"', re.IGNORECASE)
_HEAD_END = re.compile(r"</head>", re.IGNORECASE)
_CANONICAL = re.compile(r'<link rel="canonical"', re.IGNORECASE)
_OG_IMAGE = re.compile(r'<meta property="og:image"', re.IGNORECASE)


def attr_escape(text):
    """Escape text for use inside a double-quoted HTML attribute."""
    return (text.replace("&", "&amp;").replace("<", "&lt;")
                .replace(">", "&gt;").replace('"', "&quot;"))


def page_url(relative_path, base_url):
    """The one address a built page is served at.

    A directory's index.html is served for the directory itself, and that is the form the sitemap names,
    so index.html collapses to its directory. Every other page keeps its file name.
    """
    parts = [p for p in relative_path.replace("\\", "/").split("/") if p and p != "."]
    if parts and parts[-1].lower() == "index.html":
        parts = parts[:-1]
        return base_url + ("/".join(parts) + "/" if parts else "")
    return base_url + "/".join(parts)


def head_additions(relative_path, html, base_url, card_url):
    """The tags this page is missing, as one indented block, or an empty string if it already has them."""
    if _CANONICAL.search(html):
        return ""

    url = page_url(relative_path, base_url)

    found = _TITLE_META.search(html)
    if found:
        title = found.group(1).strip()
    else:
        tag = _TITLE_TAG.search(html)
        title = attr_escape(tag.group(1).strip()) if tag else SITE_NAME

    described = _DESCRIPTION.search(html)

    lines = [
        '<link rel="canonical" href="%s">' % attr_escape(url),
        '<meta property="og:type" content="website">',
        '<meta property="og:site_name" content="%s">' % SITE_NAME,
        '<meta property="og:title" content="%s">' % title,
    ]
    if described:
        lines.append('<meta property="og:description" content="%s">' % described.group(1))
    lines += [
        '<meta property="og:url" content="%s">' % attr_escape(url),
        '<meta property="og:image" content="%s">' % attr_escape(card_url),
        '<meta name="twitter:card" content="summary_large_image">',
        '<meta name="twitter:title" content="%s">' % title,
    ]
    if described:
        lines.append('<meta name="twitter:description" content="%s">' % described.group(1))
    lines.append('<meta name="twitter:image" content="%s">' % attr_escape(card_url))

    return "".join("    " + line + "\n" for line in lines)


def inject(relative_path, html, base_url, card_url):
    """The page with its missing tags in place, or unchanged when it has them or has no head."""
    block = head_additions(relative_path, html, base_url, card_url)
    if not block or not _HEAD_END.search(html):
        return html
    return _HEAD_END.sub(lambda m: block + "  " + m.group(0), html, count=1)


def pages(site):
    for folder, _, names in os.walk(site):
        for name in sorted(names):
            if name.lower().endswith(".html"):
                full = os.path.join(folder, name)
                yield os.path.relpath(full, site), full


def apply(site, base_url, card_url):
    changed = 0
    total = 0
    for relative, full in pages(site):
        total += 1
        with open(full, encoding="utf-8") as handle:
            html = handle.read()
        updated = inject(relative, html, base_url, card_url)
        if updated != html:
            with open(full, "w", encoding="utf-8") as handle:
                handle.write(updated)
            changed += 1
    print("page-metadata: %d of %d files given an address and a preview card" % (changed, total))
    return 0


def is_page(html):
    """Whether this file is a page at all. docfx also writes head-less fragments — every section's toc.html
    is one — and a fragment is never served on its own, so it has nothing to be indexed under."""
    return _HEAD_END.search(html) is not None


def page_problem(relative_path, html):
    """What is wrong with this page, or None when nothing is."""
    if not is_page(html):
        return None

    canonical = len(_CANONICAL.findall(html))
    image = len(_OG_IMAGE.findall(html))
    if canonical != 1 or image != 1:
        return "%s (canonical=%d, og:image=%d)" % (relative_path, canonical, image)
    return None


def verify(site):
    """Every page carries exactly one canonical address and one preview image, or this fails the build."""
    total = 0
    bad = []
    for relative, full in pages(site):
        with open(full, encoding="utf-8") as handle:
            html = handle.read()
        if not is_page(html):
            continue
        total += 1
        problem = page_problem(relative, html)
        if problem:
            bad.append(problem)

    if total == 0:
        print("page-metadata: no pages found under the site directory", file=sys.stderr)
        return 1
    if bad:
        print("page-metadata: %d of %d pages are wrong:" % (len(bad), total), file=sys.stderr)
        for line in bad[:20]:
            print("  " + line, file=sys.stderr)
        return 1
    print("page-metadata: all %d pages carry one canonical address and one preview card" % total)
    return 0


# ---- the cases this has to satisfy, run before it is used ------------------------------------------------

BASE = "https://xkqg.github.io/MatPlotLibNet/"
CARD = BASE + CARD_PATH

PAGE = ('<html><head><meta charset="utf-8">'
        '<title>Line charts in C# | MatPlotLibNet </title>'
        '<meta name="title" content="Line charts in C# | MatPlotLibNet ">'
        '<meta name="description" content="Draw a line chart in C#: matplotlib&#39;s plot, in .NET.">'
        '</head><body>x</body></html>')

BARE = '<html><head><title>No description here</title></head><body>x</body></html>'


def self_test():
    failures = []

    def check(name, condition, detail=""):
        if not condition:
            failures.append(name + (": " + detail if detail else ""))

    # An address: the site root, a section's index and an ordinary page each have exactly one.
    check("root index collapses to the site root", page_url("index.html", BASE) == BASE, page_url("index.html", BASE))
    check("a section index collapses to its directory",
          page_url("cookbook/index.html", BASE) == BASE + "cookbook/", page_url("cookbook/index.html", BASE))
    check("an ordinary page keeps its file name",
          page_url("cookbook/line-charts.html", BASE) == BASE + "cookbook/line-charts.html")
    check("a windows path separator is still a URL separator",
          page_url("api\\MatPlotLibNet.Plt.html", BASE) == BASE + "api/MatPlotLibNet.Plt.html")

    done = inject("cookbook/line-charts.html", PAGE, BASE, CARD)

    check("the canonical address is written",
          '<link rel="canonical" href="%scookbook/line-charts.html">' % BASE in done)
    check("the title is taken from the attribute docfx already escaped, without its trailing space",
          '<meta property="og:title" content="Line charts in C# | MatPlotLibNet">' in done)
    check("the description is carried over verbatim, entities and all",
          '<meta property="og:description" content="Draw a line chart in C#: matplotlib&#39;s plot, in .NET.">' in done)
    check("the preview points at the card", '<meta property="og:image" content="%s">' % CARD in done)
    check("the preview is the wide kind", '<meta name="twitter:card" content="summary_large_image">' in done)
    check("the tags land inside the head", done.index("og:image") < done.index("</head>"))
    check("the body is untouched", done.endswith("<body>x</body></html>"))

    check("running twice changes nothing", inject("cookbook/line-charts.html", done, BASE, CARD) == done)

    plain = inject("about.html", BARE, BASE, CARD)
    check("a page without a description still gets an address", 'rel="canonical"' in plain)
    check("a page without a description claims none", "og:description" not in plain)
    check("a title read from the element is escaped", '<meta property="og:title" content="No description here">' in plain)

    check("a page with no head is left alone",
          inject("x.html", "<p>fragment</p>", BASE, CARD) == "<p>fragment</p>")

    # Measured against a real docfx build: every section also gets a toc.html, which is a fragment with no
    # head. Judging those as pages fails the build over files nobody is ever served.
    check("a head-less fragment is not a page", not is_page("<div>nav</div>"))
    check("a fragment is never judged", page_problem("cookbook/toc.html", "<div>nav</div>") is None)
    check("a finished page has nothing wrong with it", page_problem("cookbook/line-charts.html", done) is None)
    check("a page that was never given an address is wrong",
          page_problem("about.html", PAGE) is not None)
    check("a page carrying two addresses is wrong",
          page_problem("about.html", done + done) is not None)

    for line in failures:
        print("FAIL " + line, file=sys.stderr)
    print("page-metadata self-test: %d checks, %d failed" % (23, len(failures)))
    return 1 if failures else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--site", help="the built site directory")
    parser.add_argument("--base-url", help="the address the site is served at, with a trailing slash")
    parser.add_argument("--self-test", action="store_true", help="run this file's own cases and stop")
    parser.add_argument("--apply", action="store_true", help="write the missing tags into every page")
    parser.add_argument("--verify", action="store_true", help="fail unless every page carries them")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    if not args.site or not os.path.isdir(args.site):
        parser.error("--site must name a directory that exists")
    if args.apply and not args.base_url:
        parser.error("--apply needs --base-url")
    if args.base_url and not args.base_url.endswith("/"):
        parser.error("--base-url must end with a slash")

    if args.apply:
        return apply(args.site, args.base_url, args.base_url + CARD_PATH)
    if args.verify:
        return verify(args.site)
    parser.error("nothing to do: pass --self-test, --apply or --verify")


if __name__ == "__main__":
    sys.exit(main())
