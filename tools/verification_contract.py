"""Strict typed evidence contract; expectations/tolerances belong to fixtures.

Exact integers never pass through binary64. This validates evidence shape,
not the scientific correctness or independence of the supplied oracle.
"""
import json
import math
from fractions import Fraction
from pathlib import Path
ERRORS=frozenset({'ARITHMETIC','ARGUMENT','UNSUPPORTED','INCOMPATIBLE','INCOMPATIBLE_DIMENSION','INCOMPATIBLE_QUANTITY_TYPE','DIVIDE_BY_ZERO'})
TOLERANCES=('absoluteTolerance','relativeTolerance')
def _pairs(pairs):
 result={}
 for key,value in pairs:
  if key in result: raise ValueError(f'Duplicate JSON key: {key}')
  result[key]=value
 return result
def parse(text):
 def invalid(value): raise ValueError(f'Nonfinite JSON constant: {value}')
 return json.loads(text,object_pairs_hook=_pairs,parse_constant=invalid)
def numeric(value): return isinstance(value,(int,float)) and not isinstance(value,bool)
def finite(value):
 if isinstance(value,float) and not math.isfinite(value): raise ValueError('Nonfinite observation')
 if isinstance(value,dict):
  for item in value.values(): finite(item)
 if isinstance(value,list):
  for item in value: finite(item)
def tolerances(record):
 values=tuple(record.get(key,0) for key in TOLERANCES)
 for value in values:
  if not numeric(value) or value<0 or (isinstance(value,float) and not math.isfinite(value)): raise ValueError('Finite nonnegative numeric tolerance required')
 return values
def identifier(record):
 if not isinstance(record,dict) or not isinstance(record.get('id'),str) or not record['id'].strip(): raise ValueError('Nonempty string case ID required')
 return record['id']
def outcome(record):
 identifier(record)
 if ('result' in record)==('error' in record): raise ValueError('Exactly one result or error required')
 if 'error' in record and record['error'] not in ERRORS: raise ValueError('Unknown or missing error category')
 finite(record);tolerances(record)
def load_rows(path):
 rows={}
 # LF frames JSONL; legal Unicode label separators remain data.
 for line in Path(path).read_text(encoding='utf-8').split('\n'):
  if not line.strip(): continue
  record=parse(line);key=identifier(record);outcome(record)
  if key in rows: raise ValueError(f'Duplicate observation ID: {key}')
  rows[key]=record
 if not rows: raise ValueError('Empty observation inventory')
 return rows
def load_cases(path):
 document=parse(Path(path).read_text(encoding='utf-8'));cases=document.get('cases') if isinstance(document,dict) else None
 if not isinstance(cases,list) or not cases: raise ValueError('Nonempty expected inventory required')
 result={}
 for case in cases:
  key=identifier(case)
  if key in result: raise ValueError(f'Duplicate expected ID: {key}')
  if ('expected' in case)==('expectedError' in case): raise ValueError(f'{key}: exactly one expected result or error required')
  if 'expectedError' in case and case['expectedError'] not in ERRORS: raise ValueError(f'{key}: invalid expected error')
  finite(case);tolerances(case)
  if case.get('operation')=='core':
   oracle=case.get('oracle',{})
   if oracle.get('kind') not in {'PINNED_IMPLEMENTATION_OBSERVATION','EXPLICIT_PINNED_IMPLEMENTATION_QUIRKS'} or len(oracle.get('indriyaCommit',''))!=40 or len(oracle.get('observationSha256',''))!=64:
    raise ValueError(f'{key}: missing measured-oracle provenance')
   if 'javaRepresentations' not in case: raise ValueError(f'{key}: raw Java representation mapping required')
  result[key]=case
 return result
def equal(expected,actual,absolute=0,relative=0):
 """Typed recursive comparison with fixture-authorized numeric leaf bounds."""
 if numeric(expected) and numeric(actual):
  if not absolute and not relative: return expected==actual
  a,b=Fraction(expected),Fraction(actual)
  return abs(a-b)<=max(Fraction(absolute),abs(a)*Fraction(relative))
 if type(expected) is not type(actual): return False
 if isinstance(expected,dict): return expected.keys()==actual.keys() and all(equal(v,actual[k],absolute,relative) for k,v in expected.items())
 if isinstance(expected,list): return len(expected)==len(actual) and all(equal(a,b,absolute,relative) for a,b in zip(expected,actual))
 return expected==actual
def check(rows,cases,observer=None):
 if set(rows)!=set(cases): raise ValueError('Duplicate, missing or unexpected observation IDs')
 errors=[]
 for key,case in cases.items():
  row=rows[key];outcome(row)
  if case.get('operation')=='core' and observer not in {'java','clr'}: errors.append(f'{key}: explicit observer role required');continue
  if observer=='java' and 'javaRepresentations' in case:
   if not equal(case['javaRepresentations'],row.get('javaRepresentations')): errors.append(f'{key}: raw Java representations differ')
   if row.get('javaException')!=case.get('javaException'): errors.append(f'{key}: Java exception provenance differs')
  if observer=='clr' and 'allowedClrExceptions' in case:
   if row.get('clrException') not in case['allowedClrExceptions']: errors.append(f'{key}: unapproved CLR exception mapping')
  if observer=='clr' and 'allowedClrExceptions' not in case and row.get('clrException') is not None:
   errors.append(f'{key}: unexpected CLR exception metadata')
  if any(name in row and row[name]!=case.get(name,0) for name in TOLERANCES): errors.append(f'{key}: unapproved tolerance metadata');continue
  if 'expectedError' in case:
   if row.get('error')!=case['expectedError']: errors.append(f'{key}: wrong error')
  elif 'error' in row or not equal(case['expected'],row['result'],*tolerances(case)): errors.append(f'{key}: wrong result')
 return errors
