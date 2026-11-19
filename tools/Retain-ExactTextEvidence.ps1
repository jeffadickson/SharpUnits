param([Parameter(Mandatory)][string]$RunDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container) -or !(Test-Path -LiteralPath $RunDirectory -PathType Container)){throw 'Missing source/run'}
Push-Location -LiteralPath $root
try {
 $phases=[ordered]@{}
 foreach($name in @('final-tests2','final-reference','final-exact-reference','java-tck','requirements','example')){
  $b=Get-Content -LiteralPath (Join-Path $RunDirectory ($name+'/watchdog.json')) -Raw|ConvertFrom-Json
  if($b.outcome -ne 'completed' -or $b.exitCode -ne 0 -or $b.effectiveFlags -ne 8960){throw ('Incomplete phase: '+$name)}
  $phases[$name]=$b
 }
 [xml]$trx=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-tests2/coverage/typed-errors.trx') -Raw
 $c=$trx.TestRun.ResultSummary.Counters
 if([int]$c.total -ne [int]$c.passed -or [int]$c.failed -ne 0){throw 'Incomplete suite'}
 $file=Get-ChildItem -LiteralPath (Join-Path $RunDirectory 'final-tests2/coverage') -Recurse -Filter coverage.cobertura.xml|Select-Object -First 1
 [xml]$coverage=Get-Content -LiteralPath $file.FullName -Raw
 $corpora=@(Get-Content -LiteralPath (Join-Path $RunDirectory 'final-reference/corpora/summary.json') -Raw|ConvertFrom-Json)
 if(@($corpora|Where-Object {$_.differentialExit -ne 0 -or $_.expectedCheckExit -ne 0}).Count){throw 'Corpus failure'}
 $exact=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-exact-reference/comparison.json') -Raw|ConvertFrom-Json
 if($exact.failures -ne 0 -or $exact.cases -ne 16 -or $exact.pairedNumericChecks -ne 32){throw 'Exact comparison incomplete'}
 $java=Get-Content -LiteralPath (Join-Path $RunDirectory 'java-tck/stdout.log') -Raw
 if($java -notmatch 'Tests run: 112, Failures: 0, Errors: 0, Skipped: 0'){throw 'Java TCK incomplete'}
 $source=[ordered]@{}
 foreach($directory in @('src','tests','tools','requirements','examples','docs','.github','.config')){
  if(!(Test-Path -LiteralPath $directory -PathType Container)){throw ('Missing directory: '+$directory)}
  foreach($f in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in '.cs','.csproj','.ps1','.py','.sdoc','.md','.json','.yml' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'}){
   $relative=[IO.Path]::GetRelativePath($root,$f.FullName).Replace('\','/')
   $source[$relative]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($f.FullName).Replace("`r`n","`n"))))
  }
 }
 foreach($path in @('README.md','global.json','conformance/tck-closure-ledger.json','conformance/completion-work-packages.json','conformance/exact-text-reference-cases.json','conformance/exact-text-review.md','conformance/exact-text-independent-review.md','conformance/upstream-pins.json','conformance/java-reference/pom.xml','conformance/java-reference/src/main/java/org/sharpunits/ExactTextReferenceRunner.java')){
  $source[$path]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $root $path)).Replace("`r`n","`n"))))
 }
 $raw=[ordered]@{}
 foreach($f in Get-ChildItem -LiteralPath $RunDirectory -Recurse -File){
  $relative=[IO.Path]::GetRelativePath($RunDirectory,$f.FullName).Replace('\','/')
  # Match disposable descendants, not the E:/Artifacts ancestor itself.
  if($f.Extension -notin '.json','.jsonl','.log','.trx','.xml','.ps1','.txt' -or $relative -match '(^|/)(snapshot|artifacts|build|strictdoc|retention[0-9]*)(/|$)'){continue}
  $raw[$relative]=(Get-FileHash -LiteralPath $f.FullName).Hash
 }
 if($raw.Count -eq 0){throw 'No raw execution artifacts retained'}
 $mutation=Get-Content -LiteralPath (Join-Path $RunDirectory 'mutation-summary.json') -Raw|ConvertFrom-Json
 [ordered]@{date='2026-11-16';parentCommit=(& git rev-parse HEAD).Trim();sdk=(& dotnet --version).Trim();target='net11.0';platform='Windows local; Linux/CI and Native AOT not executed';tests=@{passed=[int]$c.passed;failed=[int]$c.failed;skipped=[int]$c.notExecuted};coverage=@{linesCovered=[int]$coverage.coverage.'lines-covered';linesValid=[int]$coverage.coverage.'lines-valid';branchesCovered=[int]$coverage.coverage.'branches-covered';branchesValid=[int]$coverage.coverage.'branches-valid';scope='SharpUnits managed library only; scripts/Java/demo unmeasured'};reference=@{cases=($corpora|Measure-Object cases -Sum).Sum;corpora=$corpora.Count;failed=0;javaTckTests=112;javaTckSubject='Pinned Indriya, not SharpUnits';exactNativeContracts=$exact};randomized=@{framework='FsCheck3.2.0';trials=@{exactRationalRoundtrip=200;malformedInput=300};seeds=@('385,1611','385,1613');utf16EscapeSweep=65536;coverageGuidedFuzzer='not run'};mutation=$mutation;requirements=(Get-Content -LiteralPath (Join-Path $RunDirectory 'requirements/strictdoc-evidence.json') -Raw|ConvertFrom-Json);budgets=$phases;canonicalTextSha256=$source;rawArtifactRoot=$RunDirectory;rawSha256=$raw;formalLedgerStates=@{GAP=14;PARTIAL=99;VERIFIED=0};ci='Both workflows stay disabled_manually; no dispatch';limitations='Bounded native text profile, explicit Java grammar differences, public numeric representation not hidden promotion provenance, no full normative/Java binary compatibility or formal closure claim.'}|ConvertTo-Json -Depth 18|Set-Content -LiteralPath conformance/exact-text-evidence.json
} finally {Pop-Location}
