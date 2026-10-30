#!/usr/bin/env python3
"""Validate pinned occurrence accounting, source/test links and VERIFIED evidence.

PARTIAL is not closure. A VERIFIED entry additionally requires integrity-checked
execution artifacts, revision-bound live Windows/Ubuntu CI and explicit review.
This does not attest reviewer identity cryptographically or certify JSR conformance.
"""
import argparse,hashlib,json,pathlib,re,subprocess,sys,xml.etree.ElementTree as ET
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from verification_contract import parse
from qualification_evidence import pinned_occurrences,source_proof,attest
ROOT=pathlib.Path(__file__).resolve().parents[1]
ALLOWED={'GAP','IMPLEMENTED_UNVERIFIED','PARTIAL_BEHAVIORAL_MAPPING','CI_PASS_BEHAVIORAL_REVIEW_PENDING','VERIFIED','NOT_APPLICABLE'}
def require(condition,message):
 if not condition: raise ValueError(message)
def read(path): return parse(path.read_text(encoding='utf-8'))
def git(root,*args): return subprocess.check_output(['git','-C',str(root),*args],text=True,stderr=subprocess.PIPE).strip()
def test_reference(name,sources,revision=None):
 require(isinstance(name,str) and re.fullmatch(r'\w+\.\w+',name),f'Invalid test reference: {name}')
 owner,method=name.split('.');file=sources/'tests/SharpUnits.Tests'/f'{owner}.cs'
 require(revision is not None or file.is_file(),f'Missing test class: {name}')
 source=(subprocess.check_output(['git','-C',str(sources),'show',revision+':tests/SharpUnits.Tests/'+owner+'.cs']).decode('utf-8-sig') if revision else file.read_text(encoding='utf-8'))
 require(re.search(r'\bclass\s+'+re.escape(owner)+r'\b',source),f'Missing test class declaration: {name}')
 require(re.search(r'\bpublic\s+(?:async\s+)?(?:void|Task)\s+'+re.escape(method)+r'\s*\(',source),f'Missing test method: {name}')
def artifact(spec,root):
 require(isinstance(spec,dict) and set(('path','sha256'))<=spec.keys(),'Artifact path/hash required')
 file=(root/spec['path']).resolve();require(file.is_file(),f'Missing evidence artifact: {file}')
 require(hashlib.sha256(file.read_bytes()).hexdigest().upper()==str(spec['sha256']).upper(),f'Artifact hash mismatch: {file}')
 return file
def closure(entry,root,sources,pin):
 q=entry.get('qualification');require(isinstance(q,dict),'VERIFIED requires structured qualification, not evidence strings')
 revision=q.get('testedCommit','');require(re.fullmatch(r'[0-9a-f]{40}',revision) is not None,'Full tested commit required')
 require(git(sources,'rev-parse',revision)==revision,'Tested revision does not resolve')
 require(q.get('tckCommit')==pin,'Qualification TCK pin differs')
 require(bool(q.get('behaviorTests')),'Assertion-specific behavioral tests required')
 for ref in q['behaviorTests']:test_reference(ref,sources,revision)
 mapping=pinned_occurrences(sources,pin,entry['source'])
 require(mapping.get(entry['id'])==q.get('sourceMethod')==entry.get('sourceMethod'),'Assertion occurrence/source method association differs')
 require(bool(q.get('expectedSemantics')) and bool(q.get('apiMapping')),'Expected semantics and API adaptation decision required')
 review=q.get('review',{});require(review.get('decision')=='APPROVED' and review.get('independent') is True and bool(review.get('reviewer')) and re.fullmatch(r'\d{4}-\d{2}-\d{2}',review.get('date','')),'Independent reviewer/date/decision required')
 artifact(review.get('record'),root)
 attest(q,root,sources,pin,entry)
def validate(root,sources):
 inventory=read(root/'conformance/tck-assertions.json');ledger=read(root/'conformance/tck-closure-ledger.json')
 pin=inventory['upstream']['commit'];require(ledger.get('upstreamCommit')==pin,'Ledger pin mismatch')
 require(git(sources/'upstream/unit-tck','rev-parse','HEAD')==pin,'Pinned upstream checkout drift')
 expected=set();methods={}
 for suite in inventory['suites']:
  mapping=pinned_occurrences(sources,pin,suite['path'])
  require(sorted(mapping)==sorted(suite['assertions']),'Retained occurrence inventory differs from pinned Git source: '+suite['path'])
  expected.update((suite['path'],aid) for aid in mapping)
  methods[suite['path']]=mapping
 actual={(item['source'],item['id']) for item in ledger['entries']}
 require(len(actual)==len(ledger['entries']),'Duplicate ledger occurrence')
 require(expected==actual and ledger['totalAssertions']==len(expected),'Inventory/ledger mismatch')
 for entry in ledger['entries']:
  require(entry['status'] in ALLOWED,'Invalid assertion status')
  if entry.get('sourceMethod'):require(methods[entry['source']][entry['id']]==entry['sourceMethod'],'Wrong source-method association')
  if entry.get('test'):test_reference(entry['test'],sources)
  if entry.get('exactCoreTest'):
   test_reference(entry['exactCoreTest'],sources)
   for name in entry.get('exactCoreBehaviorTests',[]): test_reference(name,sources)
   require(bool(entry.get('exactCoreBehaviorTests')),'Exact-core behavior mapping required')
   corpus=read(sources/'conformance/work-package-one-cases.json')['cases']
   ids={case['id'] for case in corpus if case.get('domain')=='quantity'}
   require(bool(entry.get('exactCoreCases')) and set(entry['exactCoreCases'])<=ids,'Missing or stale exact-core case references')
  if entry['status']=='VERIFIED':closure(entry,root,sources,pin)
 return ledger
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--root',type=pathlib.Path,default=ROOT);parser.add_argument('--sources',type=pathlib.Path)
 args=parser.parse_args()
 try:
  ledger=validate(args.root,args.sources or args.root)
  print(f"TCK accounting/source links valid: {ledger['totalAssertions']} occurrences; VERIFIED={sum(e['status']=='VERIFIED' for e in ledger['entries'])}; full qualification enforced only for VERIFIED entries")
 except (ValueError,KeyError,OSError,subprocess.SubprocessError,ET.ParseError) as error:
  raise SystemExit(str(error))
