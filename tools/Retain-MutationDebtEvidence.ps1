param([string]$CoverageDirectory='../debt-final-coverage-v4')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 if(!(Test-Path -LiteralPath $CoverageDirectory -PathType Container)){throw 'Missing coverage output'}
 [xml]$trx=Get-Content -LiteralPath (Join-Path $CoverageDirectory debt-final.trx) -Raw
 $counter=$trx.TestRun.ResultSummary.Counters;if([int]$counter.failed -ne 0 -or [int]$counter.passed -le 0){throw 'Final test execution is not passing'}
 $coverageFile=Get-ChildItem -LiteralPath $CoverageDirectory -Filter coverage.cobertura.xml -Recurse|Select-Object -First 1
 [xml]$coverage=Get-Content -LiteralPath $coverageFile.FullName -Raw
 $latest=Get-Content -LiteralPath conformance/mutation-debt-latest.json -Raw|ConvertFrom-Json -AsHashtable
 $reviewed=Get-Content -LiteralPath conformance/mutation-debt-reviewed.json -Raw|ConvertFrom-Json -AsHashtable
 $corpora=Get-Content -LiteralPath ../debt-all-final2-corpora/summary.json -Raw|ConvertFrom-Json
 if(@($corpora|Where-Object {$_.expectedCheckExit -ne 0 -or $_.differentialExit -ne 0}).Count){throw 'Differential failures remain'}
 $sources=[ordered]@{}
 foreach($directory in @('src','tests','tools','requirements','conformance/java-reference/src')){
  foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in @('.cs','.csproj','.java','.py','.ps1','.sdoc','.json','.txt','.coveragerc') -and $_.FullName -notmatch '[\\/](bin|obj|__pycache__)[\\/]'}){
   $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
   $sources[$relative]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 $raw=[ordered]@{}
 foreach($path in @('../debt-baseline.log','../debt-numeric-red.log','../debt-numeric-green.log','../debt-chains-red.log','../debt-chains-green.log','../debt-scale-red.log','../debt-final-coverage-v4.log','../debt-final-suite.log','../debt-pinned-tck.log','../debt-all-final2-corpora.log','../debt-all-final2-corpora/summary.json','../debt-java-final.jsonl','../debt-java-final-retain.log','../debt-java-chains.jsonl','../debt-chain-capture-check.log','../debt-verifier-tests.log','../debt-ledger.log','../debt-final-strictdoc.log','../debt-final-freeze-check.log','../debt-stryker-broad.log','../debt-stryker-final.log','../debt-stryker-final-v2.log','../debt-stryker-typed-full.log','../debt-stryker-guards.log','../debt-stryker-hash-full.log','../debt-stryker-number-final.log','../debt-example.log','../debt-reference-lock.log','src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll')){
  if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw ('Missing retained evidence: '+$path)}
  $raw[$path]=(Get-FileHash -LiteralPath $path).Hash
 }
 $raw[$CoverageDirectory+'/debt-final.trx']=(Get-FileHash -LiteralPath (Join-Path $CoverageDirectory debt-final.trx)).Hash
 $raw[[IO.Path]::GetRelativePath($root,$coverageFile.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $coverageFile.FullName).Hash
 $hashTrials=Get-Content -LiteralPath conformance/mutation-debt-hash-quality.json -Raw|ConvertFrom-Json
 $result=[ordered]@{
  schemaVersion=1;result='BOUNDED_MUTATION_DEBT_REDUCTION';baseCommit=(& git rev-parse HEAD).Trim();sourcePolicy='Working source/test canonical UTF8/LF hashes and immutable phase freezes identify execution inputs. A later evidence-only commit associates the final implementation revision.'
  dotnet=@{passed=[int]$counter.passed;failed=[int]$counter.failed;skipped=[int]$counter.notExecuted;lineCoverage=@{covered=[int]$coverage.coverage.'lines-covered';total=[int]$coverage.coverage.'lines-valid'};branchCoverage=@{covered=[int]$coverage.coverage.'branches-covered';total=[int]$coverage.coverage.'branches-valid'};scope='Coverlet managed library; helper file apps, Java and workflows unmeasured'}
  java=@{pinnedTckPassed=112;failed=0;sharedCorpora=$corpora.Count;sharedCases=($corpora|Measure-Object cases -Sum).Sum;normalizedChainCases=16;independentlyAuthoredBoundaryCases=42;differentialMismatches=0}
  mutation=@{counts=$latest.counts;policy='Combined measured outcomes, not a single-run score; raw reports/phases retained and superseded only under documented matching-source rules';reviewedDispositionCounts=$reviewed.reviewedDispositionCounts;remaining='Two inconclusive mutation-induced loop timeouts. Conditional/source-bounded equivalence and observable diagnostic/hash variations are not raw kills or formal proof.'}
  randomized=@{fscheckNewConfiguredTrials=1600;properties=@(@{seed='385,901';maxTest=500;purpose='Primitive Long exact decimal multiplication'},@{seed='385,903';maxTest=500;purpose='Rational vs Binary64 projection comparison'},@{seed='385,905';maxTest=100;purpose='Hash-quality engineering criterion'},@{seed='385,907';maxTest=500;purpose='Checked scale sum BigInteger oracle'});hashTrialsActuallyRetained=$hashTrials.trials.Count;hashMinBuckets=($hashTrials.trials|Measure-Object hashBuckets -Minimum).Minimum;hashMaxNormalComparisons=($hashTrials.trials|Measure-Object normalComparisons -Maximum).Maximum;hashMinConstantComparisons=($hashTrials.trials|Measure-Object constantComparisons -Minimum).Minimum;hypothesis='37 verifier tests freshly passed; unchanged seeded property campaign retains3000attempted examples in verification-repair-properties.json'}
  toolchain=@{primarySdk='10.0.402';strykerBuildSdk='10.0.403';primaryInstalledRuntime='10.0.13';stryker='5.0.0';jdk='17.0.20.1+1';maven='3.9.9';strictdoc='0.30.4';embeddedPython='3.13.2';verifierTestPython='3.13.15';fscheck='3.2.0'}
  requirements=@{strictdocDocuments=9;ids=387;newHashQualityCriterion='Draft engineering criterion; not JSR conformance';formalAssertions=113;gap=14;partial=99;verified=0};ci='Disabled; no workflow enabled or dispatched';review='mutation-debt-review.md'
  commands=@('dotnet test tests/SharpUnits.Tests/SharpUnits.Tests.csproj -c Release --no-restore --collect:"XPlat Code Coverage" --results-directory ../debt-final-coverage-v4 --logger "trx;LogFileName=debt-final.trx"','./tools/Invoke-MutationDebt.ps1 -Phase Broad -Workers 4 -OutputDirectory <fresh-output>','./tools/Invoke-MutationDebt.ps1 -Phase Final -Workers 4 -OutputDirectory <fresh-output>','./tools/Invoke-MutationDebt.ps1 -Phase Number -Workers 4 -OutputDirectory <fresh-output>','./tools/Invoke-AllVerificationCorpora.ps1 -Runner <built-runner> -OutputDirectory <fresh-output>','python tools/run_pinned_tck.py -o (JAVA_HOME pinned JDK17, Maven3.9.9 on PATH)','python -m unittest discover -s tools -p test_*.py','./tools/ReferenceLock.ps1 -Mode Validate')
  canonicalSourceSha256=$sources;rawEvidenceSha256=$raw;referenceLockSha256=(Get-FileHash -LiteralPath conformance/verification-reference-lock.json).Hash
  limitations='No exhaustive proof, live Windows/Ubuntu qualification, external human acceptance or formal TCK closure. Internal scale primitive is directly tested; a multi-billion-operation public chain was not executed. Hash thresholds are a labeled Draft engineering choice. Compile-error/ignored mutations remain unqualified. Existing compiler/analyzer warnings remain.'
 }
 $result|ConvertTo-Json -Depth 16|Set-Content -LiteralPath conformance/mutation-debt-evidence.json
 'Retained final mutation-debt evidence'
} finally {Pop-Location}
