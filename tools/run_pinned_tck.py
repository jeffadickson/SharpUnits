#!/usr/bin/env python3
"""Run the unmodified pinned TCK sources with the exact locally built Indriya artifact.

Extra arguments are forwarded to Maven (for example -s settings.xml -t toolchains.xml).
The temporary POM stays beside the upstream POM to preserve its relative paths.
"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
pins = json.loads((root / "conformance/upstream-pins.json").read_text())
for name, pin in pins.items():
    if not isinstance(pin, dict):
        continue
    source = root / "upstream" / ("unit-tck" if name == "tck" else name)
    actual = subprocess.check_output(["git", "-C", str(source), "rev-parse", "HEAD"], text=True).strip()
    if actual != pin["commit"]:
        raise SystemExit(f"{name}: expected {pin['commit']}, found {actual}")

version = ET.parse(root / "upstream/indriya/pom.xml").getroot().find("{*}version").text
pom = root / "upstream/unit-tck/pom.xml"
text = pom.read_text()
version_range = "<version>[${ri.version},)</version>"
if text.count(version_range) != 1:
    raise SystemExit("Upstream Indriya dependency declaration changed; review pinning")
text = text.replace(version_range, f"<version>{version}</version>")
print(f"Running pinned TCK {pins['tck']['commit']} against Indriya {version} ({pins['indriya']['commit']})", flush=True)
descriptor, path = tempfile.mkstemp(prefix=".sharpunits-", suffix=".xml", dir=pom.parent)
try:
    with os.fdopen(descriptor, "w", encoding="utf-8") as output:
        output.write(text)
    maven = shutil.which("mvn") or shutil.which("mvn.cmd")
    if not maven:
        raise SystemExit("Maven is required")
    subprocess.run([maven, "-B", *sys.argv[1:], "-f", path, "test"], check=True)
finally:
    Path(path).unlink(missing_ok=True)
