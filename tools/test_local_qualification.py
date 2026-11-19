"""Synthetic local contracts, never execution evidence or reviewer approval."""
import copy,hashlib,io,json,pathlib,tempfile,unittest,zipfile
from unittest.mock import patch
from local_qualification import attest_local,strict_trx

class LocalQualificationTests(unittest.TestCase):
 def test_theory_rows_share_test_identity_but_not_execution_identity(self):
  trx=b'<TestRun><UnitTestResult testId="t" executionId="row1" outcome="Passed"/><UnitTestResult testId="t" executionId="row2" outcome="Passed"/><UnitTest id="t"><TestMethod className="SharpUnits.Tests.Theory" name="Rows"/></UnitTest><Counters total="2" executed="2" passed="2" failed="0"/></TestRun>'
  self.assertEqual({'SharpUnits.Tests.Theory.Rows'},strict_trx(trx))
  for bad in (trx.replace(b'row2',b'row1'),trx.replace(b'executionId="row2"',b''),trx.replace(b'outcome="Passed"',b'outcome="Failed"',1)):
   with self.assertRaises(ValueError):strict_trx(bad)

 def test_local_capture_and_corruption_witnesses(self):
  proof={'src/test.cs':'A','tests/test.cs':'B'};revision='1'*40;pin='2'*40
  entry={'id':'51-A2','source':'src/main/java/tech/units/tck/tests/spi/QuantityFactoryTest.java','sourceMethod':'QuantityFactoryTest.testQuantityFactoryScale'}
  trx=b'<TestRun><UnitTestResult testId="t" executionId="e" outcome="Passed"/><UnitTest id="t"><TestMethod className="SharpUnits.Tests.TckLocalClosureTests" name="FactoryScaleMatchesEveryAvailableProvider"/></UnitTest><Counters total="1" executed="1" passed="1" failed="0"/></TestRun>'
  raw=b'<testsuite tests="1" failures="0" errors="0" skipped="0"><properties><property name="java.version" value="17.0.20"/><property name="java.vm.name" value="OpenJDK"/><property name="os.name" value="Windows 11"/><property name="os.arch" value="amd64"/></properties><testcase classname="tech.units.tck.tests.spi.QuantityFactoryTest" name="testQuantityFactoryScale"/></testsuite>'
  with tempfile.TemporaryDirectory() as directory:
   root=pathlib.Path(directory)
   q=dict(testedCommit=revision,sourceSha256=proof,sourceMethod=entry['sourceMethod'],expectedSemantics='Synthetic scale preservation',apiMapping='Synthetic declared CLR adaptation',behaviorTests=['TckLocalClosureTests.FactoryScaleMatchesEveryAvailableProvider'],localEvidence=[],review={})
   def bundle(role,change=lambda b,p:(b,p),raw_override=None):
    java_raw=raw if raw_override is None else raw_override
    payload=json.dumps(dict(tckCommit=pin,failed=0,passed=1,assertions=[entry['id']],passedMethods=[entry['sourceMethod']])).encode() if role=='java' else trx;name='java-report.json' if role=='java' else 'results.trx'
    b=dict(schemaVersion=2,producerContext='LOCAL_CAPTURE',testedCommit=revision,tckCommit=pin,sourceSha256=proof,role=role,upstreamPins={'tck':pin,'indriya':'3'*40},environment=dict(platform='windows' if role in ('windows','java') else 'ubuntu',framework='.NET 11.0.0',architecture='X64',os='measured host',osRelease='ID=ubuntu\nVERSION_ID="24.04"' if role=='ubuntu' else '',wsl=role=='ubuntu',capturedAtUtc='2026-11-19T00:00:00Z'),payload=dict(path=name,sha256=hashlib.sha256(payload).hexdigest()),rawReports=[])
    if role=='java':
     b['rawReports']=[dict(path='surefire/TEST-factory.xml',sha256=hashlib.sha256(java_raw).hexdigest())]
     b['javaRuntimes']=[{'java.version':'17.0.20','java.vm.name':'OpenJDK','os.name':'Windows 11','os.arch':'amd64'}]
    b,payload=change(b,payload)
    data=io.BytesIO()
    with zipfile.ZipFile(data,'w') as z:
     z.writestr('qualification.json',json.dumps(b));z.writestr(name,payload)
     if role=='java':z.writestr('surefire/TEST-factory.xml',java_raw)
    path=root/(role+'.zip');path.write_bytes(data.getvalue())
    return dict(role=role,artifact=dict(path=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest().upper()))
   def fresh():
    q['localEvidence']=[bundle(r) for r in ('windows','ubuntu','java')]
    q['review']=dict(decision='APPROVED',independent=True,reviewer='Synthetic unit actor; not real approval',date='2026-11-19',localArtifactSha256={e['role']:e['artifact']['sha256'] for e in q['localEvidence']})
    approve()
   def approve():
    # Rebind synthetic approval so corrupt report tests exercise validation,
    # rather than merely being rejected by an accidentally stale approval.
    q['review']['localArtifactSha256']={e['role']:e['artifact']['sha256'] for e in q['localEvidence']}
    record=dict(testedCommit=revision,artifactSha256=q['review']['localArtifactSha256'],decision='APPROVED',independent=True,reviewer=q['review']['reviewer'],date=q['review']['date'],assertions=[{key:entry['id'] if key=='id' else q[key] for key in ('id','sourceMethod','expectedSemantics','apiMapping','behaviorTests')}])
    path=root/'review.json';path.write_text(json.dumps(record),encoding='utf-8');q['review']['record']=dict(path=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
   def call(candidate):
    with patch('local_qualification.source_proof',return_value=proof),patch('local_qualification.read_pins',return_value={'tck':pin,'indriya':'3'*40}):attest_local(candidate,root,root,pin,entry)
   fresh();call(q)
   for field in ('localEvidence','sourceSha256','review'):
    bad=copy.deepcopy(q);bad.pop(field)
    with self.subTest(field=field),self.assertRaises((ValueError,KeyError)):call(bad)
   for mutate in (
    lambda b:b.update(testedCommit='0'*40),lambda b:b.update(sourceSha256={}),
    lambda b:b.update(producerContext='LOCAL_UNTRUSTED'),
    lambda b:b['environment'].update(platform='windows'),
    lambda b:b['environment'].update(framework='.NET 10.0.0'),
    lambda b:b['upstreamPins'].update(indriya='0'*40),
    lambda b:b['payload'].update(sha256='0'*64)):
    fresh()
    def change(b,p):mutate(b);return b,p
    q['localEvidence'][1]=bundle('ubuntu',change);approve()
    with self.subTest(mutation=mutate),self.assertRaises(ValueError):call(q)
   for corrupted in (
    trx.replace(b'outcome="Passed"',b'outcome="NotExecuted"'),
    trx.replace(b'passed="1"',b'passed="2"'),
    trx.replace(b'name="FactoryScaleMatchesEveryAvailableProvider"',b'name="Unrelated"'),
    trx.replace(b'SharpUnits.Tests.',b'Another.Namespace.'),
    trx.replace(b'<Counters',b'<UnitTestResult testId="t" outcome="Passed"/><Counters')):
    fresh()
    def change(b,p):b['payload']['sha256']=hashlib.sha256(corrupted).hexdigest();return b,corrupted
    q['localEvidence'][0]=bundle('windows',change);approve()
    with self.assertRaises(ValueError):call(q)
   for corrupted in (
    raw.replace(b'tests="1"',b'tests="2"'),
    raw.replace(b'tech.units.tck.tests.spi.',b'unrelated.namespace.'),
    raw.replace(b'name="testQuantityFactoryScale"',b'name="unrelated"'),
    raw.replace(b'failures="0"',b'failures="1"'),
    raw.replace(b'17.0.20',b'10.0.1'),
    raw.replace(b'<testcase ',b'<testcase skipped="true" ' ).replace(b'/></testsuite>',b'><skipped/></testcase></testsuite>')):
    fresh();q['localEvidence'][2]=bundle('java',raw_override=corrupted);approve()
    with self.assertRaises(ValueError):call(q)
   # Replacement bundles plus an edited ledger must not reuse old approval.
   fresh()
   q['localEvidence'][0]=bundle('windows',lambda b,p:(dict(b,environment=dict(b['environment'],os='replacement host')),p))
   q['review']['localArtifactSha256']={e['role']:e['artifact']['sha256'] for e in q['localEvidence']}
   with self.assertRaises(ValueError):call(q)
   fresh();q['localEvidence'].pop()
   with self.assertRaises(ValueError):call(q)
   fresh();q['localEvidence'][1]=q['localEvidence'][0]
   with self.assertRaises(ValueError):call(q)
   fresh();q['localEvidence'][0]['artifact']['sha256']='0'*64
   with self.assertRaises(ValueError):call(q)
   fresh();q['review']['localArtifactSha256']['windows']='0'*64
   with self.assertRaises(ValueError):call(q)
   fresh();q['expectedSemantics']='An unreviewed assertion change'
   with self.assertRaises(ValueError):call(q)
   q['expectedSemantics']='Synthetic scale preservation'
   fresh();path=root/'review.json';path.write_text('{}')
   with self.assertRaises(ValueError):call(q)

if __name__=='__main__':unittest.main()
