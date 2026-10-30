#!/usr/bin/env python3
"""Compare strict Java/CLR observations against fixture-owned inventory."""
import argparse
import sys
from pathlib import Path
# Support the existing isolated/embedded Python runtime without installing helpers.
sys.path.insert(0,str(Path(__file__).resolve().parent))
from verification_contract import load_rows as load, load_cases, outcome, tolerances, equal, check
def compare(java,dotnet,cases=None):
 problems=[]
 if not java or not dotnet: return ['Empty comparison inventory']
 try:
  for rows in (java,dotnet):
   for key,row in rows.items():
    outcome(row)
    if row['id']!=key: raise ValueError('Record ID does not match inventory key')
  if cases is not None: problems.extend(check(java,cases,'java'));problems.extend(check(dotnet,cases,'clr'))
  for key in sorted(set(java)|set(dotnet)):
   if key not in java or key not in dotnet: problems.append(f'{key}: missing case');continue
   a,b=java[key],dotnet[key]
   if a.get('error')!=b.get('error'): problems.append(f'{key}: error categories differ');continue
   if 'error' in a: continue
   if not equal(a['result'],b['result'],*tolerances(cases[key] if cases is not None else a)): problems.append(f'{key}: value mismatch')
 except (ValueError,TypeError,KeyError) as error: problems.append(str(error))
 return problems
def main():
 p=argparse.ArgumentParser();p.add_argument('java');p.add_argument('dotnet');p.add_argument('--cases',required=True)
 a=p.parse_args();errors=compare(load(a.java),load(a.dotnet),load_cases(a.cases))
 print(f'Differential comparison: {len(errors)} mismatch(es)')
 for error in errors: print(error)
 if errors: raise SystemExit(1)
if __name__=='__main__': main()
