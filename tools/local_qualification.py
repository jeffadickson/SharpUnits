"""Reviewed local evidence, explicitly distinct from remotely attested CI.

Integrity checks cannot authenticate an operator or host. Closure depends on a
separate reviewer of the measured producer context, raw reports and artifact hashes.
"""
import datetime,hashlib,io,pathlib,re,xml.etree.ElementTree as ET,zipfile
from verification_contract import parse
from qualification_evidence import source_proof,git,need

def read_pins(sources,revision):
 pins=parse(git(sources,'show',revision+':conformance/upstream-pins.json').decode('utf-8'))
 return {name:pins[name]['commit'] for name in ('tck','indriya')}

def strict_trx(data):
 tree=ET.fromstring(data);counters=tree.findall('.//{*}Counters')
 need(len(counters)==1,'One TRX counter set required')
 results=tree.findall('.//{*}UnitTestResult');c=counters[0]
 need(bool(results) and all(r.get('outcome')=='Passed' for r in results),'Every TRX result must pass')
 ids=[r.get('testId') for r in results]
 executions=[r.get('executionId') for r in results]
 # VSTest assigns one testId to some xUnit theories, but a distinct executionId
 # to each row. Reject repeated executions, not legitimate parameterized rows.
 need(all(ids) and all(executions) and len(set(executions))==len(executions),'Duplicate or missing TRX execution identity')
 need(all(int(c.get(k,'-1'))==len(results) for k in ('total','executed','passed')) and all(int(c.get(k,'0'))==0 for k in ('failed','error','timeout','aborted','notExecuted')),'TRX counters/results disagree')
 definitions=tree.findall('.//{*}UnitTest');names={}
 for item in definitions:
  identifier=item.get('id');method=item.find('.//{*}TestMethod')
  need(identifier not in names and method is not None,'Duplicate or missing TRX definition')
  names[identifier]=method.get('className','').split(',')[0]+'.'+method.get('name','')
 need(set(ids)==set(names),'TRX definition/result inventory differs')
 return set(names.values())

def java_methods(data):
 tree=ET.fromstring(data);need(tree.tag=='testsuite','Expected raw Surefire suite')
 cases=tree.findall('testcase')
 need(int(tree.get('tests','-1'))==len(cases) and all(int(tree.get(k,'-1'))==0 for k in ('failures','errors','skipped')),'Surefire counters/results disagree')
 need(all(not any(t.find(k) is not None for k in ('failure','error','skipped')) for t in cases),'Surefire contains failed/skipped tests')
 methods=[t.get('classname','')+'.'+t.get('name','') for t in cases]
 need(len(methods)==len(set(methods)),'Duplicate Surefire method')
 return set(methods)

def java_runtime(data):
 tree=ET.fromstring(data)
 properties={p.get('name'):p.get('value') for p in tree.findall('./properties/property')}
 runtime={key:properties.get(key,'') for key in ('java.version','java.vm.name','os.name','os.arch')}
 need(all(runtime.values()) and re.match(r'17\.',runtime['java.version']) is not None,'Measured pinned Java17 runtime properties required')
 return runtime

def attest_local(q,root,sources,pin,entry):
 proof=source_proof(sources,q['testedCommit'])
 need(q.get('sourceSha256')==proof,'Local source inventory differs from tested commit')
 pins=read_pins(sources,q['testedCommit']);need(pins['tck']==pin,'Local TCK pin differs')
 roles=set();digests={}
 for evidence in q.get('localEvidence',[]):
  role=evidence.get('role');need(role in {'windows','ubuntu','java'} and role not in roles,'Unique local Windows/Ubuntu/Java roles required');roles.add(role)
  spec=evidence.get('artifact',{});path=(root/spec.get('path','')).resolve()
  need(path.is_relative_to(root.resolve()) and path.is_file(),'Local artifact missing or outside evidence root')
  data=path.read_bytes();digest=hashlib.sha256(data).hexdigest().upper()
  need(digest==str(spec.get('sha256','')).upper(),'Local artifact digest differs');digests[role]=digest
  with zipfile.ZipFile(io.BytesIO(data)) as archive:
   need(len(archive.infolist())<=1024 and sum(i.file_size for i in archive.infolist())<=64*1024*1024,'Oversized local bundle')
   need(len(archive.namelist())==len(set(archive.namelist())),'Duplicate local archive entry')
   b=parse(archive.read('qualification.json').decode('utf-8'))
   need(b.get('schemaVersion')==2 and b.get('producerContext')=='LOCAL_CAPTURE' and b.get('role')==role,'Wrong local producer context')
   need(b.get('testedCommit')==q['testedCommit'] and b.get('sourceSha256')==proof and b.get('tckCommit')==pin and b.get('upstreamPins')==pins,'Local bundle source/pins differ')
   env=b.get('environment',{})
   need(env.get('os') and env.get('architecture') in {'X64','Arm64'} and re.fullmatch(r'\.NET 11\.\d+(?:\.\d+)?',env.get('framework','')),'Measured .NET11 host context required')
   datetime.datetime.fromisoformat(env.get('capturedAtUtc','').replace('Z','+00:00'))
   need(isinstance(env.get('wsl'),bool),'Explicit WSL context required')
   if role in {'windows','ubuntu'}:need(env.get('platform')==role,'Wrong local execution platform')
   if role=='ubuntu':need(re.search(r'^ID=(?:"ubuntu"|ubuntu)$',env.get('osRelease',''),re.M) is not None,'Ubuntu distribution evidence required')
   def content(item):
    name=item.get('path','');need(name in archive.namelist() and '..' not in pathlib.PurePosixPath(name).parts,'Missing/invalid local payload')
    payload=archive.read(name);need(hashlib.sha256(payload).hexdigest().upper()==str(item.get('sha256','')).upper(),'Local payload digest differs');return payload
   payload=content(b.get('payload',{}))
   if role=='java':
    need(b['payload']['path']=='java-report.json','Expected Java execution index')
    report=parse(payload.decode('utf-8'));need(report.get('tckCommit')==pin and report.get('failed')==0 and entry['id'] in report.get('assertions',[]) and entry['sourceMethod'] in report.get('passedMethods',[]),'Java assertion/method index absent')
    raw=b.get('rawReports',[]);need(bool(raw) and len({r['path'] for r in raw})==len(raw),'Unique raw Surefire reports required')
    methods=set();runtimes=[]
    for item in raw:
     need(re.fullmatch(r'surefire/TEST-[^/\\]+\.xml',item['path']) is not None,'Invalid Surefire path')
     xml=content(item);found=java_methods(xml);need(not methods.intersection(found),'Duplicate Java execution method');methods.update(found)
     runtime=java_runtime(xml)
     if runtime not in runtimes:runtimes.append(runtime)
    expected=entry['source'].removeprefix('src/main/java/').removesuffix('.java').replace('/','.')+'.'+entry['sourceMethod'].split('.')[-1]
    need(expected in methods and report.get('passed')==len(methods),'Raw Java method/counter absent or inconsistent')
    need(b.get('javaRuntimes')==runtimes,'Java capture/raw runtime properties differ')
   else:
    need(b['payload']['path']=='results.trx','Expected raw TRX payload')
    need({'SharpUnits.Tests.'+ref for ref in q['behaviorTests']}<=strict_trx(payload),'Full behavioral method absent from local platform run')
 need(roles=={'windows','ubuntu','java'},'Complete local Windows/Ubuntu/Java evidence required')
 need(q.get('review',{}).get('localArtifactSha256')==digests,'Independent local review must bind exact artifact digests')
 review=q['review'];spec=review.get('record',{});path=(root/spec.get('path','')).resolve()
 need(path.is_relative_to(root.resolve()) and path.is_file(),'Structured local review artifact required')
 data=path.read_bytes();need(hashlib.sha256(data).hexdigest().upper()==str(spec.get('sha256','')).upper(),'Local review record digest differs')
 record=parse(data.decode('utf-8'))
 need(record.get('testedCommit')==q['testedCommit'] and record.get('artifactSha256')==digests,'Reviewed revision/artifacts differ')
 need(record.get('decision')==review.get('decision')=='APPROVED' and record.get('independent') is review.get('independent') is True and record.get('reviewer')==review.get('reviewer') and record.get('date')==review.get('date'),'Local review decision/identity differs')
 expected={key:entry['id'] if key=='id' else q[key] for key in ('id','sourceMethod','expectedSemantics','apiMapping','behaviorTests')}
 need(expected in record.get('assertions',[]),'Exact assertion semantics/API mapping/tests not approved in review artifact')
