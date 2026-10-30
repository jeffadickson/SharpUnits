"""Revision-bound, CI-attested execution evidence. Never dispatches workflows.

The trust boundary is a reviewed CI producer and GitHub artifact digest, not an
arbitrary local JSON string. Synthetic unit fixtures are not live CI evidence.
"""
import functools,hashlib,io,pathlib,re,subprocess,xml.etree.ElementTree as ET,zipfile
from verification_contract import parse

def need(condition,message):
 if not condition:raise ValueError(message)
def git(root,*args):return subprocess.check_output(['git','-C',str(root),*args],stderr=subprocess.PIPE)
def canonical(data):return data.replace(b'\r\n',b'\n')
@functools.lru_cache(maxsize=8)
def source_proof(root,revision):
 # Bind every tracked blob, including build inputs, resources and dependency locks.
 # Gitlinks are separately pinned upstream repositories, not readable source blobs.
 records=git(root,'ls-tree','-r',revision).decode().splitlines()
 selected=[line.split('\t',1)[1] for line in records if line.split()[1]=='blob']
 need(any(p.startswith('src/') for p in selected) and any(p.startswith('tests/') for p in selected),'Complete implementation/test source inventory required')
 return {p:hashlib.sha256(git(root,'show',revision+':'+p)).hexdigest().upper() for p in selected}
def pinned_occurrences(root,pin,path):
 text=git(root/'upstream/unit-tck','show',pin+':'+path).decode('utf-8-sig')
 annotations=list(re.finditer(r'@SpecAssertion\s*\(([\s\S]*?)\)',text));found=[]
 for index,match in enumerate(annotations):
  identifier=re.search(r'\bid\s*=\s*"([^"]+)"',match[1]);need(identifier is not None,'Unrecognized pinned assertion annotation')
  end=annotations[index+1].start() if index+1<len(annotations) else len(text)
  method=re.search(r'\bpublic\s+void\s+(\w+)\s*\(',text[match.end():end]);need(method is not None,'Pinned assertion lacks a method')
  found.append((identifier[1],pathlib.Path(path).stem+'.'+method[1]))
 counts={};result={}
 for identifier,method in found:
  counts[identifier]=counts.get(identifier,0)+1
  key=identifier+'-'+str(counts[identifier]) if sum(x[0]==identifier for x in found)>1 else identifier
  result[key]=method
 return result

def passed_methods(data):
 trx=ET.fromstring(data);counters=trx.find('.//{*}Counters')
 need(counters is not None and int(counters.get('passed','0'))>0 and all(int(counters.get(k,'0'))==0 for k in ('failed','error','timeout','aborted')),'Passing nonempty TRX required')
 ids={n.get('testId') for n in trx.findall('.//{*}UnitTestResult') if n.get('outcome')=='Passed'}
 result=set()
 for test in trx.findall('.//{*}UnitTest'):
  method=test.find('.//{*}TestMethod')
  if test.get('id') in ids and method is not None:result.add(method.get('className','').split(',')[0].split('.')[-1]+'.'+method.get('name',''))
 return result

def remote(path):return parse(subprocess.check_output(['gh','api','repos/jeffadickson/SharpUnits/'+path],text=True,timeout=30))
def attest(q,root,sources,pin,entry):
 revision=q['testedCommit'];proof=source_proof(sources,revision)
 need(q.get('sourceSha256')==proof,'Qualification source/test inventory or hashes differ from tested Git revision')
 roles=set()
 for evidence in q.get('ciEvidence',[]):
  role=evidence.get('role');need(role in {'windows','ubuntu','java'} and role not in roles,'Unique Windows/Ubuntu/Java evidence required');roles.add(role)
  match=re.fullmatch(r'https://github.com/jeffadickson/SharpUnits/actions/runs/(\d+)',evidence.get('runUrl',''));need(match is not None,'Invalid CI run URL')
  run=remote('actions/runs/'+match[1]);java=role=='java'
  need(run.get('head_sha')==revision and run.get('conclusion')=='success','CI source revision or conclusion differs')
  need(run.get('name')==('Java reference TCK' if java else '.NET CI') and run.get('path')==('.github/workflows/java-reference.yml' if java else '.github/workflows/ci.yml'),'Wrong qualification workflow')
  jobs=remote('actions/runs/'+match[1]+'/jobs?per_page=100').get('jobs',[])
  wanted='java-reference' if java else f'Build and test ({role}-latest)'
  label='ubuntu' if java else role
  need(any(j.get('name')==wanted and j.get('conclusion')=='success' and any(re.fullmatch(label+r'-(latest|\d{2,4}(?:\.\d+)?)',x.lower()) for x in j.get('labels',[])) for j in jobs),'Intended test job/platform runner labels not established')
  spec=evidence.get('artifact',{});path=(root/spec.get('path','')).resolve();need(path.is_file(),'Missing downloaded qualification artifact')
  data=path.read_bytes();digest=hashlib.sha256(data).hexdigest().upper();need(digest==str(spec.get('sha256','')).upper(),'Local CI artifact digest differs')
  header=remote('actions/artifacts/'+str(evidence.get('artifactId')))
  need(header.get('name')=='qualification-'+role and str(header.get('workflow_run',{}).get('id'))==match[1] and str(header.get('digest','')).upper()=='SHA256:'+digest,'Artifact is not attested by the matching CI run')
  with zipfile.ZipFile(io.BytesIO(data)) as archive:
   need(sum(item.file_size for item in archive.infolist())<=64*1024*1024 and len(archive.infolist())<=1024,'Oversized qualification archive')
   need(len(archive.namelist())==len(set(archive.namelist())),'Duplicate archive entry')
   bundle=parse(archive.read('qualification.json').decode('utf-8'))
   need(bundle.get('producerContext')=='CI' and bundle.get('testedCommit')==revision and bundle.get('sourceSha256')==proof and bundle.get('tckCommit')==pin and bundle.get('role')==role and str(bundle.get('runId'))==match[1],'Execution bundle provenance differs')
   payload=bundle.get('payload',{});name=payload.get('path');need(name in {'results.trx','java-report.json'},'Invalid qualification payload name')
   content=archive.read(name);need(hashlib.sha256(content).hexdigest().upper()==str(payload.get('sha256','')).upper(),'Execution payload digest differs')
   if java:
    report=parse(content.decode('utf-8'));need(report.get('tckCommit')==pin and report.get('failed')==0 and report.get('passed',0)>0 and entry['id'] in report.get('assertions',[]) and entry['sourceMethod'] in report.get('passedMethods',[]),'Pinned Java assertion/method execution absent')
    raw=bundle.get('rawReports',[]);need(bool(raw),'Raw Java execution reports required')
    methods=set()
    for item in raw:
     name=item.get('path','');need(re.fullmatch(r'surefire/TEST-[^/\\]+\.xml',name) is not None,'Invalid raw Java report path')
     xml=archive.read(name);need(hashlib.sha256(xml).hexdigest().upper()==str(item.get('sha256','')).upper(),'Raw Java execution report digest differs')
     tree=ET.fromstring(xml)
     for test in tree.findall('.//testcase'):
      need(test.find('failure') is None and test.find('error') is None,'Raw Java execution contains failures')
      if test.find('skipped') is None:methods.add(test.get('classname','').split('.')[-1]+'.'+test.get('name',''))
    need(entry['sourceMethod'] in methods,'Pinned Java method absent from raw execution reports')
   else:need(set(q['behaviorTests'])<=passed_methods(content),'Behavior tests absent from platform execution')
 need(roles=={'windows','ubuntu','java'},'Complete attested Windows/Ubuntu/Java evidence required')
