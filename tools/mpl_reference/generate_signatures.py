"""
Writes the table of matplotlib's calls that the documents are held to.

The documents put a pyplot call beside the MatPlotLibNet call that draws the same chart (the cookbook's page from matplotlib to C#,
the cheat sheet, the wiki), and the XML documentation names the matplotlib function, class or setting a rule follows. The wiki suite
reads each such Python call and asks this table whether matplotlib 3.11.2 has it with the parameters the document writes. The suite
never runs Python: it reads the file this script writes.

Usage:
    pip install -r requirements.txt
    python generate_signatures.py

Output: ../../Tst/MatPlotLibNet.Wiki/Python/signatures.txt, the same on every run. Each section starts with a line
`@ <owner> <module or class> <version>`; each line below it is one name of that owner:
  - a call, its name and its parameters as inspect.signature gives them (self left out, no annotations), a class written as its
    constructor and each of its methods as `Class.method(...)`; after a call that takes **kwargs, the keywords its documentation
    lists in brackets (`[steps integer prune]`), because a signature does not name them;
  - an attribute or a property, its name alone.
A class owner (the objects a document calls methods on: Axes, Figure, ...) holds every member a call reaches, inherited ones included;
a module owner holds what the module declares itself. Section `@ rcParams` holds every setting with its default under matplotlib's
default style, `@ rcParams.classic` the settings the classic style changes. The last section, `@ keywords`, holds the keywords an
artist takes as a property (`set_color` is `color`), with their short forms (`lw`, `c`): a call that passes its **kwargs on to an
artist takes them.
"""

import enum
import functools
import importlib
import importlib.metadata
import inspect
import pkgutil
import re
import sys
import warnings
from pathlib import Path

import matplotlib

matplotlib.use("Agg")

import matplotlib.artist  # noqa: E402
import matplotlib.pyplot  # noqa: E402,F401
import matplotlib.style  # noqa: E402
from mpl_toolkits.mplot3d import axes3d  # noqa: E402,F401

OUT = Path(__file__).parent.parent.parent / "Tst" / "MatPlotLibNet.Wiki" / "Python" / "signatures.txt"

# The objects a document calls methods on, by the name the wiki suite gives each: every member a call reaches is written.
CLASSES = [
    ("Axes", "matplotlib.axes:Axes"),
    ("PolarAxes", "matplotlib.projections.polar:PolarAxes"),
    ("Axes3D", "mpl_toolkits.mplot3d.axes3d:Axes3D"),
    ("Figure", "matplotlib.figure:Figure"),
    ("FigureCanvasBase", "matplotlib.backend_bases:FigureCanvasBase"),
    ("Axis", "matplotlib.axis:Axis"),
    ("XAxis", "matplotlib.axis:XAxis"),
    ("YAxis", "matplotlib.axis:YAxis"),
    ("Spine", "matplotlib.spines:Spine"),
    ("Colorbar", "matplotlib.colorbar:Colorbar"),
    ("Line2D", "matplotlib.lines:Line2D"),
    ("ContourSet", "matplotlib.contour:ContourSet"),
    ("Series", "pandas:Series"),
]

# The packages beside matplotlib whose calls a comparison names, each written as its modules.
PACKAGES = ["matplotlib", "mpl_toolkits.mplot3d", "numpy", "numpy.fft", "builtins", "colorsys", "seaborn", "scipy.stats", "scipy.cluster.hierarchy", "pandas", "pandas.plotting",
            "squarify", "plotly.express", "mplfinance", "pywaffle", "joypy", "cartopy.crs"]

# Modules of matplotlib a document has no business with: the drawing backends, the tests, the documentation build.
SKIPPED = re.compile(r"\.(backends|tests|testing|sphinxext|pylab)(\.|$)")


def resolve(target):
    module, _, attribute = target.partition(":")
    owner = importlib.import_module(module)
    return getattr(owner, attribute) if attribute else owner


def default_text(value):
    """A default as a document writes it, the same on every run: a function or a class by its name, a member of an enumeration by its
    name, anything else by its repr on one line, an address it carries left out."""
    if inspect.isfunction(value) or inspect.isclass(value) or inspect.isbuiltin(value):
        return value.__qualname__
    if isinstance(value, enum.Enum):
        return f"{type(value).__name__}.{value.name}"
    return re.sub(r" at 0x[0-9A-Fa-f]+", "", " ".join(repr(value).split()))


def signature_of(member, bound=False):
    """The parameters of a call as a document writes them: each name, a default after =, * and / where Python puts them, no
    annotations; the object a method is called on left out (self, or the axes of a function an Axes holds as a method, Axes.table), as
    a call does not write it."""
    try:
        signature = inspect.signature(member)
    except (TypeError, ValueError):
        return None
    parts = []
    keyword_only_marked = False
    parameters = list(signature.parameters.values())
    if parameters and (bound or parameters[0].name in ("self", "cls")) and parameters[0].kind in (parameters[0].POSITIONAL_ONLY, parameters[0].POSITIONAL_OR_KEYWORD):
        parameters = parameters[1:]
    for at, parameter in enumerate(parameters):
        if parameter.kind == parameter.VAR_POSITIONAL:
            parts.append("*" + parameter.name)
            keyword_only_marked = True
        elif parameter.kind == parameter.VAR_KEYWORD:
            parts.append("**" + parameter.name)
        else:
            if parameter.kind == parameter.KEYWORD_ONLY and not keyword_only_marked:
                parts.append("*")
                keyword_only_marked = True
            text = parameter.name
            if parameter.default is not parameter.empty:
                text += "=" + default_text(parameter.default)
            parts.append(text)
            if parameter.kind == parameter.POSITIONAL_ONLY and (at + 1 == len(parameters) or parameters[at + 1].kind != parameter.POSITIONAL_ONLY):
                parts.append("/")
    text = "(" + ", ".join(parts) + ")"
    if any(parameter.kind == parameter.VAR_KEYWORD for parameter in parameters):
        documented = documented_keywords(member)
        if documented:
            text += " [" + " ".join(documented) + "]"
    return text


DOCUMENTED = re.compile(r"^\s*([A-Za-z_]\w*)(?:\s*,\s*[A-Za-z_]\w*)*\s*:", re.MULTILINE)


def documented_keywords(member):
    """The keywords the documentation of a call lists as its parameters (numpydoc `name : type` lines): the ones **kwargs takes."""
    doc = (inspect.getdoc(member) or "") + "\n" + ((inspect.getdoc(member.__init__) or "") if inspect.isclass(member) else "")
    names = set()
    for line in doc.splitlines():
        match = re.match(r"^\s*([A-Za-z_][\w, ]*?)\s*:\s*\S", line)
        if match and len(line) - len(line.lstrip()) <= 4:
            names.update(name.strip() for name in match.group(1).split(",") if re.fullmatch(r"[A-Za-z_]\w*", name.strip()))
    return sorted(names)


def is_property(member):
    return isinstance(member, property) or inspect.isdatadescriptor(member)


def members_of_class(cls, prefix, inherited):
    lines = []
    if not prefix:
        # A class owner's constructor, which a document names as __init__.
        constructor = signature_of(cls)
        lines.append("__init__" + (constructor if constructor is not None else "(*args, **kwargs)"))
    if prefix:
        constructor = signature_of(cls)
        bases = " ".join(f"{base.__module__}.{base.__qualname__}" for base in cls.__mro__[1:] if base is not object)
        lines.append(prefix + (constructor if constructor is not None else "(*args, **kwargs)") + (" : " + bases if bases else ""))
    # A class owner is written with every member a call reaches; a class of a module with what it declares, and the classes it derives
    # from after its constructor, which the suite reads for what it inherits.
    members = inspect.getmembers_static(cls) if inherited else list(vars(cls).items())
    for name, member in sorted(members, key=lambda each: each[0]):
        if name.startswith("__"):
            continue
        written = (prefix + "." if prefix else "") + name
        # A function a class holds is called on an instance, which it takes first; a static method takes no instance.
        bound = inspect.isfunction(member) or isinstance(member, classmethod)
        if isinstance(member, (staticmethod, classmethod)):
            member = member.__func__
        if is_property(member):
            lines.append(written)
        elif inspect.isclass(member):
            continue
        elif callable(member):
            signature = signature_of(member, bound)
            lines.append(written + (signature if signature is not None else "(*args, **kwargs)"))
    return lines


def members_of_module(module):
    """What a module declares itself (what it imports from elsewhere is written under the module that declares it): its functions,
    its classes, its attributes; a package's own functions however deep it declares them (numpy.clip lives in numpy._core). Of the
    packages beside matplotlib (numpy, pandas, scipy) only the functions and the constructors: their classes' methods are no call a
    document compares with, but for the classes the suite calls methods on (CLASSES)."""
    lines = []
    package = module.__name__.split(".")[0]
    ours = package in ("matplotlib", "mpl_toolkits")
    for name, member in sorted(vars(module).items(), key=lambda each: each[0]):
        if name.startswith("__") or inspect.ismodule(member):
            continue
        declared_in = getattr(member, "__module__", None) or ""
        if inspect.isclass(member):
            if declared_in == module.__name__ or (declared_in.startswith(package) and module.__name__ == package):
                lines.extend(members_of_class(member, name, inherited=False) if ours or package not in ("numpy", "pandas", "scipy") else [name + (signature_of(member) or "(*args, **kwargs)")])
        elif callable(member):
            # A function a module makes from another (mlab.magnitude_spectrum is a functools.partial) is the module's own.
            if declared_in == module.__name__ or declared_in.startswith(package) or declared_in == "" or isinstance(member, functools.partial):
                signature = signature_of(member)
                lines.append(name + (signature if signature is not None else "(*args, **kwargs)"))
        elif ours:
            lines.append(name)
    return lines


def modules_of(package):
    root = importlib.import_module(package)
    yield root
    if not hasattr(root, "__path__") or package in ("numpy", "numpy.fft", "pandas"):
        return
    ours = package.split(".")[0] in ("matplotlib", "mpl_toolkits")
    for info in sorted(pkgutil.walk_packages(root.__path__, package + ".", onerror=lambda name: None), key=lambda each: each.name):
        # The private modules of a package beside matplotlib hold what its public modules give (scipy.stats._continuous_distns).
        if SKIPPED.search(info.name) or (not ours and "._" in info.name):
            continue
        try:
            yield importlib.import_module(info.name)
        except Exception:  # noqa: BLE001 - a module that cannot load here is no module a document calls
            continue


def keywords():
    """Every property an artist of matplotlib takes as a keyword, and the short forms of their names."""
    names = set()
    for cls in all_subclasses(matplotlib.artist.Artist):
        inspector = matplotlib.artist.ArtistInspector(cls)
        names.update(inspector.get_setters())
        for aliases in inspector.aliasd.values():
            names.update(alias[len("set_"):] if alias.startswith("set_") else alias for alias in aliases)
    return sorted(names)


def all_subclasses(cls):
    seen = {cls}
    pending = [cls]
    while pending:
        for sub in pending.pop().__subclasses__():
            if sub not in seen and sub.__module__.startswith(("matplotlib", "mpl_toolkits")):
                seen.add(sub)
                pending.append(sub)
    return sorted(seen, key=lambda each: each.__module__ + "." + each.__qualname__)


def made_instances():
    """One object of each class a document names the attributes of, made as matplotlib makes it."""
    import matplotlib.pyplot as plt
    figure = plt.figure()
    axes = figure.add_subplot(1, 3, 1)
    return {
        "Axes": axes,
        "PolarAxes": figure.add_subplot(1, 3, 2, projection="polar"),
        "Axes3D": figure.add_subplot(1, 3, 3, projection="3d"),
        "Figure": figure,
        "XAxis": axes.xaxis,
        "YAxis": axes.yaxis,
        "Colorbar": figure.colorbar(axes.imshow([[0, 1]]), ax=axes),
        "Line2D": axes.plot([0, 1])[0],
        "ContourSet": axes.contour([[0, 1], [1, 2]]),
    }


def version_of(name):
    root = name.partition(":")[0].split(".")[0]
    try:
        return importlib.metadata.version(root)
    except importlib.metadata.PackageNotFoundError:
        return matplotlib.__version__ if root == "mpl_toolkits" else "unknown"


def main():
    warnings.simplefilter("ignore")
    out = [
        "# The calls, attributes and settings of matplotlib and of the Python packages the documents compare with. Written by",
        "# tools/mpl_reference/generate_signatures.py: never edited by hand.",
    ]
    axes = set()
    instances = made_instances()
    for owner, target in CLASSES:
        out.append(f"@ {owner} {target} {version_of(target)}")
        lines = members_of_class(resolve(target), "", inherited=True)
        # What an object holds that its class does not declare: the attributes its constructor sets (Axes.patch, Axes3D._dist).
        known = {line.split("(")[0] for line in lines}
        lines += sorted(name for name in vars(instances[owner]) if not name.startswith("__") and name not in known) if owner in instances else []
        # A polar or 3-D axes is an Axes: it is written with the members it declares or changes, a call is read against all three.
        if owner == "Axes":
            axes = set(lines)
        elif owner in ("PolarAxes", "Axes3D"):
            lines = [line for line in lines if line not in axes]
        out.extend(lines)
    seen = set()
    for package in PACKAGES:
        for module in modules_of(package):
            if module.__name__ in seen:
                continue
            seen.add(module.__name__)
            # A module is written even when it declares nothing itself: a document may name it (mpl_toolkits.mplot3d).
            out.append(f"@ {module.__name__} {module.__name__} {version_of(module.__name__)}")
            out.extend(members_of_module(module))
    out.append(f"@ rcParams matplotlib {matplotlib.__version__}")
    out.extend(f"{key}={default_text(value)}" for key, value in sorted(matplotlib.rcParamsDefault.items()))
    out.append(f"@ rcParams.classic matplotlib {matplotlib.__version__}")
    classic = matplotlib.style.library["classic"]
    out.extend(f"{key}={default_text(value)}" for key, value in sorted(classic.items()) if matplotlib.rcParamsDefault.get(key) != value)
    out.append(f"@ keywords matplotlib.artist {matplotlib.__version__}")
    out.append(" ".join(keywords()))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(line.replace("\n", " ") for line in out) + "\n", encoding="utf-8", newline="\n")
    print(f"{OUT}: {len(out)} lines", file=sys.stderr)


if __name__ == "__main__":
    main()
