#!/usr/bin/env python3
"""Inventory Java test methods without claiming they have been ported.

Uses only Python's standard library. Extracts Java methods carrying TestNG/JUnit
@Test annotations, with a conservative regex; manual review is still required
for inherited, parameterized and dynamically generated tests.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCES = {
    "unit-tck": ROOT / "upstream" / "unit-tck",
    "indriya": ROOT / "upstream" / "indriya",
}
TEST_METHOD = re.compile(
    r"@(?:org\.junit\.jupiter\.api\.|org\.testng\.annotations\.)?Test\b"
    r"(?:\s*\([^)]*\))?"
    r"(?:(?!@Test)[\s\S]){0,1200}?"
    r"\b(?:public|protected|private)?\s*(?:static\s+)?"
    r"(?:void|[A-Za-z_][\w<>?,.\[\] ]*)\s+"
    r"([A-Za-z_][\w]*)\s*\(",
    re.MULTILINE,
)

def main():
    records = []
    for repo, root in SOURCES.items():
        if not root.exists():
            sys.exit(f"Missing submodule {root}; run git submodule update --init --recursive")
        for path in sorted(root.rglob("*Test.java")):
            source = path.read_text(encoding="utf-8", errors="replace")
            names = list(dict.fromkeys(TEST_METHOD.findall(source)))
            records.append({
                "source": repo,
                "path": path.relative_to(root).as_posix(),
                "detectedTestMethods": names,
                "methodCount": len(names),
                "portStatus": "UNREVIEWED",
                "notes": "Static annotation scan only; verify parameterized and inherited tests manually",
            })
    output = {
        "schemaVersion": 1,
        "testClassCount": len(records),
        "detectedMethodCount": sum(r["methodCount"] for r in records),
        "completePort": False,
        "classes": records,
    }
    target = ROOT / "conformance" / "java-method-inventory.json"
    target.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(f"Inventoried {output['testClassCount']} classes and {output['detectedMethodCount']} candidate methods")
    if len(records) < 90:
        sys.exit("Upstream test inventory unexpectedly small")

if __name__ == "__main__":
    main()
