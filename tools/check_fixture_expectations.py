#!/usr/bin/env python3
"""Validate observations using the shared strict, fixture-owned contract."""
import argparse
import sys
from pathlib import Path
# Support the existing isolated/embedded Python runtime without installing helpers.
sys.path.insert(0,str(Path(__file__).resolve().parent))
from verification_contract import load_cases,load_rows,check
def main():
 p=argparse.ArgumentParser();p.add_argument('cases');p.add_argument('actual');p.add_argument('--observer',choices=('java','clr'));a=p.parse_args()
 cases=load_cases(a.cases);errors=check(load_rows(a.actual),cases,a.observer)
 if errors: raise SystemExit('\n'.join(errors))
 print(f'Validated {len(cases)} shared conformance fixtures')
if __name__=='__main__': main()
