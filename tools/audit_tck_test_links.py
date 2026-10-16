#!/usr/bin/env python3
"""Audit exact assertion IDs against local C# test source and report honest coverage."""
import json
import pathlib
import re

root=pathlib.Path(__file__).resolve().parents[1]
ledger=json.loads((root/"conformance/tck-closure-ledger.json").read_text())
sources={p.stem:p.read_text(encoding="utf-8") for p in (root/"tests").rglob("*.cs")}
records=[]
for e in ledger["entries"]:
    test=e.get("test")
    present=False
    if test and "." in test:
        cls,method=test.split(".",1)
        present=any(re.search(r"\b"+re.escape(method)+r"\s*\(",source) for name,source in sources.items() if cls in source)
    records.append({"id":e["id"],"status":e["status"],"test":test,"testFound":present})
missing=[x for x in records if x["test"] and not x["testFound"]]
report={"total":len(records),"mapped":sum(bool(x["test"]) for x in records),"missingTestMethods":missing,"verified":sum(x["status"]=="VERIFIED" for x in records)}
(root/"conformance/tck-audit-report.json").write_text(json.dumps(report,indent=2)+"\n")
print(json.dumps({k:v if k!="missingTestMethods" else len(v) for k,v in report.items()}))
if missing:
    raise SystemExit("Mapped test methods not found: "+", ".join(x["id"] for x in missing))
