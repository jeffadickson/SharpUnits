"""Negative qualification tests use isolated ledger data and real pinned sources."""
import json,pathlib,subprocess,sys,tempfile,unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from verification_test_support import run_cli
ROOT=pathlib.Path(__file__).resolve().parents[1]
class LedgerRepairTests(unittest.TestCase):
 def run_ledger(self,change):
  with tempfile.TemporaryDirectory() as directory:
   root=pathlib.Path(directory);(root/'conformance').mkdir()
   inventory=json.loads((ROOT/'conformance/tck-assertions.json').read_text(encoding='utf-8'))
   ledger=json.loads((ROOT/'conformance/tck-closure-ledger.json').read_text(encoding='utf-8'))
   change(ledger)
   (root/'conformance/tck-assertions.json').write_text(json.dumps(inventory),encoding='utf-8')
   (root/'conformance/tck-closure-ledger.json').write_text(json.dumps(ledger),encoding='utf-8')
   return run_cli(ROOT/'tools/validate_tck_ledger.py',['--root',root,'--sources',ROOT])
 def test_current_unverified_accounting_is_valid(self):
  result=self.run_ledger(lambda ledger:None);self.assertEqual(0,result.returncode,result.stderr.decode())
 def test_fabricated_closure_is_rejected(self):
  def change(ledger): ledger['entries'][0].update(status='VERIFIED',test='NoSuchClass.NoSuchTest',evidence='fiction',javaEvidence='fiction')
  self.assertNotEqual(0,self.run_ledger(change).returncode)
 def test_stale_partial_test_reference_rejected(self):
  def change(ledger): next(e for e in ledger['entries'] if e['status']!='GAP')['test']='NoSuchClass.NoSuchTest'
  self.assertNotEqual(0,self.run_ledger(change).returncode)
 def test_pin_drift_rejected(self):
  self.assertNotEqual(0,self.run_ledger(lambda ledger:ledger.update(upstreamCommit='0'*40)).returncode)
 def test_duplicate_entry_rejected(self):
  self.assertNotEqual(0,self.run_ledger(lambda ledger:ledger['entries'].append(ledger['entries'][0])).returncode)


class StructuredClosureTests(unittest.TestCase):
 """Synthetic CI artifact contracts; no real CI approval is claimed."""
 def test_source_proof_preserves_binary_resource_bytes(self):
  import hashlib
  from unittest.mock import patch
  from qualification_evidence import source_proof
  source_proof.cache_clear()
  def proof(payload):
   def response(root,*args):
    if args[0]=='ls-tree':return b'100644 blob a\tsrc/resource.bin\n100644 blob b\ttests/example.cs\n100644 blob c\tglobal.json\n'
    return payload if args[-1].endswith('resource.bin') else b'{}'
   with patch('qualification_evidence.git',side_effect=response):return source_proof('synthetic',hashlib.sha256(payload).hexdigest())
  a=proof(b'\x00\r\n\xff');b=proof(b'\x00\n\xff')
  self.assertNotEqual(a['src/resource.bin'],b['src/resource.bin'])
  self.assertEqual(hashlib.sha256(b'\x00\r\n\xff').hexdigest().upper(),a['src/resource.bin'])
  self.assertIn('global.json',a)
 def test_attested_execution_contract_and_negative_witnesses(self):
  import importlib.util,hashlib,copy,io,zipfile
  from unittest.mock import patch
  spec=importlib.util.spec_from_file_location('ledger_gate',ROOT/'tools/validate_tck_ledger.py');gate=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate)
  from qualification_evidence import source_proof
  entry=next(e for e in json.loads((ROOT/'conformance/tck-closure-ledger.json').read_text(encoding='utf-8'))['entries'] if e.get('sourceMethod') and e.get('test'))
  revision=subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],text=True).strip();proof=source_proof(ROOT,revision)
  pin=json.loads((ROOT/'conformance/upstream-pins.json').read_text())['tck']['commit']
  with tempfile.TemporaryDirectory() as directory:
   root=pathlib.Path(directory);owner,method=entry['test'].split('.')
   trx=f'<TestRun><Results><UnitTestResult testId="t" outcome="Passed"/></Results><TestDefinitions><UnitTest id="t"><TestMethod className="SharpUnits.Tests.{owner}" name="{method}"/></UnitTest></TestDefinitions><Counters passed="1" failed="0"/></TestRun>'.encode()
   review=root/'review.md';review.write_text('Synthetic contract actor, not a human or live CI approval')
   q=dict(testedCommit=revision,tckCommit=pin,behaviorTests=[entry['test']],sourceMethod=entry['sourceMethod'],expectedSemantics='Synthetic contract',apiMapping='Declared CLR adaptation',sourceSha256=proof,review=dict(decision='APPROVED',independent=True,reviewer='Synthetic unit actor',date='2026-10-29',record=dict(path='review.md',sha256=hashlib.sha256(review.read_bytes()).hexdigest())),ciEvidence=[])
   artifacts={}
   for index,role in enumerate(('windows','ubuntu','java')):
    run=124 if role=='java' else 123;artifact_id=1000+index
    payload=json.dumps(dict(tckCommit=pin,failed=0,passed=1,assertions=[entry['id']],passedMethods=[entry['sourceMethod']])).encode() if role=='java' else trx
    name='java-report.json' if role=='java' else 'results.trx'
    bundle=dict(producerContext='CI',testedCommit=revision,tckCommit=pin,sourceSha256=proof,role=role,runId=str(run),payload=dict(path=name,sha256=hashlib.sha256(payload).hexdigest()))
    raw=f'<testsuite><testcase classname="{entry["sourceMethod"].split(".")[0]}" name="{entry["sourceMethod"].split(".")[1]}"/></testsuite>'.encode()
    if role=='java':bundle['rawReports']=[dict(path='surefire/TEST-synthetic.xml',sha256=hashlib.sha256(raw).hexdigest())]
    output=io.BytesIO()
    with zipfile.ZipFile(output,'w') as archive:
     archive.writestr('qualification.json',json.dumps(bundle));archive.writestr(name,payload)
     if role=='java':archive.writestr('surefire/TEST-synthetic.xml',raw)
    data=output.getvalue();file=root/(role+'.zip');file.write_bytes(data);digest=hashlib.sha256(data).hexdigest()
    artifacts[artifact_id]=dict(name='qualification-'+role,expired=False,workflow_run=dict(id=run),digest='sha256:'+digest)
    q['ciEvidence'].append(dict(role=role,runUrl=f'https://github.com/jeffadickson/SharpUnits/actions/runs/{run}',artifactId=artifact_id,artifact=dict(path=file.name,sha256=digest)))
   original=subprocess.check_output
   def gh_success(command,**kwargs):
    if command[0]!='gh':return original(command,**kwargs)
    path=command[-1]
    if '/artifacts/' in path:return json.dumps(artifacts[int(path.split('/')[-1])])
    java='/runs/124' in path
    if '/jobs?' in path:
     jobs=[dict(name='java-reference',conclusion='success',labels=['ubuntu-latest'])] if java else [dict(name=f'Build and test ({role}-latest)',conclusion='success',labels=[role+'-latest']) for role in ('windows','ubuntu')]
     return json.dumps(dict(jobs=jobs))
    return json.dumps(dict(head_sha=revision,conclusion='success',name='Java reference TCK' if java else '.NET CI',path='.github/workflows/java-reference.yml' if java else '.github/workflows/ci.yml'))
   def call(candidate,gh=gh_success):
    item=copy.deepcopy(entry);item['qualification']=candidate
    with patch.object(gate.subprocess,'check_output',side_effect=gh):gate.closure(item,root,ROOT,pin)
   call(q) # Positive schema/logic control with synthetic ZIPs and remote responses.
   for field in ('testedCommit','tckCommit','behaviorTests','sourceMethod','expectedSemantics','apiMapping','sourceSha256','review','ciEvidence'):
    bad=copy.deepcopy(q);bad.pop(field)
    with self.subTest(field=field),self.assertRaises((ValueError,KeyError,TypeError)):call(bad)
   bad=copy.deepcopy(q);bad['sourceSha256']={next(iter(proof)):next(iter(proof.values()))}
   with self.assertRaises(ValueError):call(bad)
   bad=copy.deepcopy(q);bad['sourceMethod']='OtherExistingMethod'
   with self.assertRaises(ValueError):call(bad)
   bad=copy.deepcopy(q);bad['ciEvidence'][0]['artifact']['sha256']='0'*64
   with self.assertRaises(ValueError):call(bad)
   def gh_wrong_revision(command,**kwargs):
    result=gh_success(command,**kwargs)
    if command[0]=='gh' and '/runs/' in command[-1] and '/jobs?' not in command[-1]:
     data=json.loads(result);data['head_sha']='0'*40;return json.dumps(data)
    return result
   with self.assertRaises(ValueError):call(q,gh_wrong_revision)
   def gh_wrong_runner_label(command,**kwargs):
    result=gh_success(command,**kwargs)
    if command[0]=='gh' and '/jobs?' in command[-1]:
     data=json.loads(result)
     for job in data['jobs']:job['labels']=['some-other-runner']
     return json.dumps(data)
    return result
   with self.assertRaises(ValueError):call(q,gh_wrong_runner_label)
   def gh_wrong_artifact_digest(command,**kwargs):
    result=gh_success(command,**kwargs)
    if command[0]=='gh' and '/artifacts/' in command[-1]:
     data=json.loads(result);data['digest']='sha256:'+'0'*64;return json.dumps(data)
    return result
   with self.assertRaises(ValueError):call(q,gh_wrong_artifact_digest)

if __name__=='__main__':unittest.main()
