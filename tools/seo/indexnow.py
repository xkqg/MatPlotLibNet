#!/usr/bin/env python3
"""Tell Bing, Yandex, DuckDuckGo and Copilot which pages changed.

A search engine finds a change by coming back and looking. IndexNow inverts that: the site says which
addresses changed and the engine fetches them, usually within hours. It needs no account and no console —
the proof of ownership is a file served from the site whose content is its own name.

That key file has been published since the site gained a sitemap, and nothing ever submitted a URL with
it, so it told nobody anything. This is the half that was missing.

The key is never written here. It is read from the file that is published, so the two cannot drift apart.

    python3 tools/seo/indexnow.py --self-test
    git diff --name-only A B | python3 tools/seo/indexnow.py --base-url ... --key-dir docs --changed - --submit
    python3 tools/seo/indexnow.py --base-url ... --key-dir docs --sitemap --submit
"""

import argparse
import glob
import json
import os
import re
import sys
import urllib.error
import urllib.request

ENDPOINT = "https://api.indexnow.org/IndexNow"

_KEY_NAME = re.compile(r"^[0-9a-f]{32}$")
_LOC = re.compile(r"<loc>\s*([^<\s]+)\s*</loc>", re.IGNORECASE)


def read_key(key_dir):
    """The published key, read from the file that proves it. One file, or this is not a working setup."""
    found = [path for path in sorted(glob.glob(os.path.join(key_dir, "*.txt")))
             if _KEY_NAME.match(os.path.splitext(os.path.basename(path))[0])]
    if len(found) != 1:
        raise SystemExit("indexnow: expected exactly one key file in %s, found %d" % (key_dir, len(found)))

    name = os.path.splitext(os.path.basename(found[0]))[0]
    with open(found[0], encoding="utf-8") as handle:
        content = handle.read().strip()
    if content != name:
        raise SystemExit("indexnow: %s does not say its own name; IndexNow refuses every submission with it"
                         % os.path.basename(found[0]))
    return name


def site_urls(changed_paths, base_url):
    """The addresses that changed, from the repository paths that changed in this push.

    A deleted page is submitted too: the engine fetches it, gets a 404 and drops it from the index, which
    is what IndexNow is for. Source changes are not mapped page by page — the API reference is 729 of the
    766 pages and one entry point is a truer statement than a guessed list.
    """
    urls = set()
    for raw in changed_paths:
        path = raw.strip().replace("\\", "/")
        if not path:
            continue

        if path.startswith("Src/") and path.endswith(".cs"):
            urls.add(base_url + "api/")
            continue

        if not path.startswith("docs/"):
            continue

        rest = path[len("docs/"):]
        if rest.endswith(".yml"):
            # A table of contents is the site's navigation; the page that shows it is the section's index.
            section = rest.rsplit("/", 1)[0] + "/" if "/" in rest else ""
            urls.add(base_url + section)
        elif rest.endswith(".md"):
            page = rest[: -len(".md")]
            folder, _, name = page.rpartition("/")
            if name != "index":
                urls.add(base_url + page + ".html")
            elif folder:
                urls.add(base_url + folder + "/")
            else:
                urls.add(base_url)

    return sorted(urls)


def sitemap_urls(base_url):
    """Every address the published sitemap names — the full-resubmission path, for a manual run."""
    with urllib.request.urlopen(base_url + "sitemap.xml", timeout=30) as response:
        return sorted(set(_LOC.findall(response.read().decode("utf-8-sig"))))


def submit(urls, base_url, key, timeout=30):
    """Hand the list over. A refusal is ours to fix; an outage is not, and must not fail a deploy."""
    host = base_url.split("//", 1)[-1].split("/", 1)[0]
    payload = {"host": host, "key": key, "keyLocation": base_url + key + ".txt", "urlList": urls}
    body = json.dumps(payload).encode("utf-8")

    request = urllib.request.Request(ENDPOINT, data=body, method="POST",
                                     headers={"Content-Type": "application/json; charset=utf-8"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            print("indexnow: %d urls submitted, answered %d" % (len(urls), response.status))
            return 0
    except urllib.error.HTTPError as refused:
        detail = refused.read().decode("utf-8", "replace").strip()[:400]
        if 400 <= refused.code < 500 and refused.code != 429:
            print("indexnow: refused with %d — the key, the host or the urls are wrong: %s"
                  % (refused.code, detail), file=sys.stderr)
            return 1
        print("indexnow: %d from the service, nothing to fix here: %s" % (refused.code, detail), file=sys.stderr)
        return 0
    except (urllib.error.URLError, TimeoutError) as unreachable:
        print("indexnow: service unreachable, skipped: %s" % unreachable, file=sys.stderr)
        return 0


# ---- the cases this has to satisfy, run before it is used ------------------------------------------------

BASE = "https://xkqg.github.io/MatPlotLibNet/"


def self_test():
    failures = []

    def check(name, condition, detail=""):
        if not condition:
            failures.append(name + (": " + detail if detail else ""))

    got = site_urls(["docs/index.md"], BASE)
    check("the home page is the site root", got == [BASE], str(got))

    got = site_urls(["docs/cookbook/line-charts.md"], BASE)
    check("a recipe is its own page", got == [BASE + "cookbook/line-charts.html"], str(got))

    got = site_urls(["docs/cookbook/index.md"], BASE)
    check("a section's index is the section", got == [BASE + "cookbook/"], str(got))

    got = site_urls(["docs/COVERAGE.md"], BASE)
    check("a top-level page keeps its name", got == [BASE + "COVERAGE.html"], str(got))

    got = site_urls(["docs/cookbook/toc.yml"], BASE)
    check("a changed table of contents points at the section it belongs to",
          got == [BASE + "cookbook/"], str(got))

    got = site_urls(["docs/toc.yml"], BASE)
    check("the site's own navigation points at the root", got == [BASE], str(got))

    got = site_urls(["Src/MatPlotLibNet/Plt.cs", "Src/MatPlotLibNet.Geo/Projection.cs"], BASE)
    check("source changes name the api reference once", got == [BASE + "api/"], str(got))

    got = site_urls(["README.md", "CHANGELOG.md", "Src/MatPlotLibNet/x.resx", "Tst/a/b.cs"], BASE)
    check("what the site does not publish is not submitted", got == [], str(got))

    got = site_urls(["docs/cookbook/a.md", "docs/cookbook/a.md", "docs/cookbook/b.md"], BASE)
    check("the same page is submitted once",
          got == [BASE + "cookbook/a.html", BASE + "cookbook/b.html"], str(got))

    got = site_urls(["", "   ", "docs\\cookbook\\c.md"], BASE)
    check("a windows path is still a url", got == [BASE + "cookbook/c.html"], str(got))

    sample = ('﻿<?xml version="1.0" encoding="utf-8"?><urlset><url><loc>https://a/x.html</loc>'
              "<lastmod>x</lastmod></url><url><loc>https://a/</loc></url></urlset>")
    check("every address in a sitemap is found",
          sorted(set(_LOC.findall(sample))) == ["https://a/", "https://a/x.html"])

    for line in failures:
        print("FAIL " + line, file=sys.stderr)
    print("indexnow self-test: %d checks, %d failed" % (11, len(failures)))
    return 1 if failures else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", help="the address the site is served at, with a trailing slash")
    parser.add_argument("--key-dir", help="the directory the published key file lives in")
    parser.add_argument("--changed", help="a file of changed repository paths, or - for standard input")
    parser.add_argument("--sitemap", action="store_true", help="submit every address in the published sitemap")
    parser.add_argument("--self-test", action="store_true", help="run this file's own cases and stop")
    parser.add_argument("--submit", action="store_true", help="actually hand the list to IndexNow")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    if not args.base_url or not args.base_url.endswith("/"):
        parser.error("--base-url must be given and must end with a slash")
    if not args.key_dir:
        parser.error("--key-dir must name the directory the key file is in")

    if args.sitemap:
        urls = sitemap_urls(args.base_url)
    elif args.changed:
        source = sys.stdin if args.changed == "-" else open(args.changed, encoding="utf-8")
        with source:
            urls = site_urls(source.readlines(), args.base_url)
    else:
        parser.error("nothing to submit: pass --changed or --sitemap")

    if not urls:
        print("indexnow: nothing the site publishes changed in this push")
        return 0

    for url in urls:
        print("  " + url)

    if not args.submit:
        print("indexnow: %d urls, not submitted (no --submit)" % len(urls))
        return 0

    return submit(urls, args.base_url, read_key(args.key_dir))


if __name__ == "__main__":
    sys.exit(main())
