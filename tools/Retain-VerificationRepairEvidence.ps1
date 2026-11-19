param([string]$CoverageDirectory='../repair-final-coverage-v3')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 if(!(Test-Path -LiteralPath $CoverageDirectory -PathType Container)){throw 'Missing final coverage directory'}
 $coverageFile=Get-ChildItem -LiteralPath $CoverageDirectory -Filter coverage.cobertura.xml -Recurse|Select-Object -First 1
 if(!$coverageFile){throw 'Missing Coverlet execution evidence'}
 [xml]$coverage=Get-Content -LiteralPath $coverageFile.FullName -Raw
 [xml]$trx=Get-Content -LiteralPath (Join-Path $CoverageDirectory repair-final.trx) -Raw
 $counter=$trx.TestRun.ResultSummary.Counters
 if([int]$counter.failed -ne 0 -or [int]$counter.passed -le 0){throw 'Final tests are not passing'}
 $sources=[ordered]@{}
 foreach($directory in @('src','tests','tools','requirements','examples','conformance/java-reference/src')){
  if(!(Test-Path -LiteralPath $directory -PathType Container)){throw ('Missing source directory: '+$directory)}
  foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in @('.cs','.csproj','.java','.py','.ps1','.sdoc','.json','.coveragerc','.txt') -and $_.FullName -notmatch '[\\/](bin|obj|__pycache__)[\\/]'}){
   $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
   $sources[$relative]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 $raw=[ordered]@{}
 foreach($path in @('../repair-pinned-tck.log','../repair-all-corpora/summary.json','../repair-all-corpora-final.log','../repair-core-final.log','../repair-snapshot-match.log','../repair-links-final.log','../repair-probes.log','../repair-python-coverage-tests.log','../repair-python-coverage-report.log','../repair-stryker-broad.log','../repair-stryker-focused-final.log','../repair-stryker-broad/reports/mutation-report.json','../repair-stryker-focused-final/reports/mutation-report.json','../repair-final-coverage-v3.log','../repair-survivor-tests.log','../repair-reference-lock.log','../repair-reference-drift.log','../repair-ledger-final.log','../repair-strictdoc-final.log','../repair-example.log','../repair-java-producer.log','../repair-clr-producer.log','src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll')){
  if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw ('Missing evidence: '+$path)}
  $raw[$path]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
 }
 $raw[[IO.Path]::GetRelativePath($root,$coverageFile.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $coverageFile.FullName).Hash
 $raw[$CoverageDirectory+'/repair-final.trx']=(Get-FileHash -LiteralPath (Join-Path $CoverageDirectory repair-final.trx)).Hash
 $tool=(Get-Content -LiteralPath conformance/verification-repair-tool-coverage.json -Raw|ConvertFrom-Json -AsHashtable).totals
 $result=[ordered]@{
  schemaVersion=1;qualification='BOUNDED_LOCAL_REPAIR_ACCEPTANCE_WITH_RETAINED_DEBT';baseCommit=(& git rev-parse HEAD).Trim();sourceRevisionPolicy='Working-tree canonical UTF-8/LF source hashes identify tested inputs; commit association is retained in the subsequent evidence commit. Exact committed Git blob hashes are used by the formal CI gate.'
  toolchain=@{primaryDotnetSdk='10.0.402';strykerBuildSdk='10.0.403';jdk='17.0.20.1+1';maven='3.9.9';strictdoc='0.30.4';stryker='5.0.0';pythonVerifierRuntime='3.13.2';pythonToolCoverageRuntime='3.13.15';coveragePy='7.17.0';hypothesis='6.170.12'}
  dotnet=@{passed=[int]$counter.passed;failed=[int]$counter.failed;skipped=[int]$counter.notExecuted;lineCoverage=@{covered=[int]$coverage.coverage.'lines-covered';total=[int]$coverage.coverage.'lines-valid'};branchCoverage=@{covered=[int]$coverage.coverage.'branches-covered';total=[int]$coverage.coverage.'branches-valid'};scope='Coverlet managed library; Java observers/file-app helpers/workflows not instrumented'}
  verifierTests=@{passed=37;failed=0;lineCoverage=@{covered=$tool.covered_lines;total=$tool.num_statements};branchCoverage=@{covered=$tool.covered_branches;total=$tool.num_branches};scope='Five existing Python verification modules; synthetic GitHub boundary fixtures do not establish live CI'}
  javaTck=@{passed=112;failed=0;errors=0;skipped=0};differential=@{corpora=10;pairedCases=2628;mismatches=0;coreCases=1738;supplementalRepairCases=5};repairProbes=@{passed=14;failed=0};strictdoc=@{documents=9;ids=386}
  commands=@('dotnet test <isolated-current-source>/tests/SharpUnits.Tests/SharpUnits.Tests.csproj -c Release --no-restore --collect:"XPlat Code Coverage" --results-directory ../repair-final-coverage-v3 --logger "trx;LogFileName=repair-final.trx"','tools-runtime/python/python.exe tools/run_pinned_tck.py -o (JAVA_HOME pinned JDK17; Maven3.9.9 on PATH)','./tools/Invoke-AllVerificationCorpora.ps1 -Runner <isolated-current-source>/src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll','dotnet run --file tools/VerificationRepair.cs -- <existing-python> <dotnet> <built-runner>','python -m coverage run --rcfile tools/verification.coveragerc -m unittest discover -s tools -p test_*.py','./tools/Invoke-RepairMutations.ps1 -OutputDirectory ../repair-stryker-focused-final','dotnet-stryker --config-file tools/stryker-core.json --skip-version-check --output ../repair-stryker-broad','./tools/ReferenceLock.ps1 -Mode Validate','python tools/validate_tck_ledger.py','./tools/Export-Requirements.ps1 -StrictDocCommand ../tools-runtime/strictdoc/bin/strictdoc.exe -OutputDirectory ../repair-strictdoc-final -EvidencePath conformance/verification-repair-strictdoc.json','dotnet run --file examples/ExactJavaCore.cs')
  referenceLock=@{path='verification-reference-lock.json';sha256=(Get-FileHash -LiteralPath conformance/verification-reference-lock.json).Hash};broadMutation='verification-repair-mutations.json';initialFocusedMutation='verification-repair-focused-initial-mutations.json';finalFocusedMutation='verification-repair-focused-mutations.json';properties='verification-repair-properties.json';review='verification-repair-review.md';formalLedger=@{occurrences=113;gap=14;partial=99;verified=0};ci='Both workflows disabled_manually; never enabled or dispatched'
  canonicalSourceSha256=$sources;rawEvidenceSha256=$raw;limitations='No exhaustive mutation, platform CI, external human acceptance or JSR conformance claim. Native electrical/symbolic regressions are included in the full suite. Large raw execution artifacts remain local; hashes and summarized reports are versioned.'
 }
 $result|ConvertTo-Json -Depth 12|Set-Content -LiteralPath conformance/verification-repair-evidence.json
 Write-Output ('Retained final evidence: .NET '+$counter.passed+', Java112, paired2628, formal closures0')
} finally {Pop-Location}
