param([Parameter(Mandatory)][string]$RunDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container) -or !(Test-Path -LiteralPath $RunDirectory -PathType Container)){throw 'Missing checkout/run'}
Push-Location -LiteralPath $root
try {
 $budgets=[ordered]@{}
 foreach($phase in @('tests','reference','java-tck','api-and-probes','requirements','validation')){
  $b=Get-Content -LiteralPath (Join-Path $RunDirectory ($phase+'/watchdog.json')) -Raw|ConvertFrom-Json
  if($b.outcome -ne 'completed' -or $b.exitCode -ne 0 -or $b.effectiveFlags -ne 8960){throw ('Incomplete guarded phase: '+$phase)}
  $budgets[$phase]=$b
 }
 [xml]$trx=Get-Content -LiteralPath (Join-Path $RunDirectory 'tests/coverage/typed-errors.trx') -Raw
 $counts=$trx.TestRun.ResultSummary.Counters
 if([int]$counts.total -ne [int]$counts.passed -or [int]$counts.failed -ne 0){throw 'Suite incomplete'}
 $cf=Get-ChildItem -LiteralPath (Join-Path $RunDirectory 'tests/coverage') -Recurse -Filter coverage.cobertura.xml|Select-Object -First 1
 [xml]$coverage=Get-Content -LiteralPath $cf.FullName -Raw
 $corpora=@(Get-Content -LiteralPath (Join-Path $RunDirectory 'reference/corpora/summary.json') -Raw|ConvertFrom-Json)
 if(@($corpora|Where-Object {$_.differentialExit -ne 0 -or $_.expectedCheckExit -ne 0}).Count){throw 'Corpus failure'}
 $java=Get-Content -LiteralPath (Join-Path $RunDirectory 'java-tck/stdout.log') -Raw
 if($java -notmatch 'Tests run: 112, Failures: 0, Errors: 0, Skipped: 0'){throw 'Java baseline not established'}
 $controls=@(Get-Content -LiteralPath (Join-Path $RunDirectory 'validator-controls.json') -Raw|ConvertFrom-Json)
 if($controls.Count -ne 6 -or @($controls|Where-Object {$_.accepted -ne $_.expectedAccept}).Count){throw 'Validator controls failed'}
 $audit=Get-Content -LiteralPath conformance/release-compliance-audit.json -Raw|ConvertFrom-Json
 $source=[ordered]@{}
 foreach($dir in @('src','tests','tools','requirements','docs','examples','.github','.config')){
  if(!(Test-Path -LiteralPath $dir -PathType Container)){throw ('Missing source directory: '+$dir)}
  foreach($file in Get-ChildItem -LiteralPath $dir -Recurse -File|Where-Object {$_.Extension -in '.cs','.csproj','.ps1','.py','.sdoc','.md','.json','.yml' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'}){
   $path=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
   $source[$path]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 foreach($path in @('README.md','global.json','conformance/tck-closure-ledger.json','conformance/completion-work-packages.json','conformance/release-compliance-audit.json','conformance/release-compliance-audit.md','conformance/release-audit-core.json','conformance/release-audit-formats.json','conformance/release-audit-services.json','conformance/release-audit-probes.json','conformance/release-audit-independent-review.md','conformance/upstream-pins.json')){
  $source[$path]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $root $path)).Replace("`r`n","`n"))))
 }
 $raw=[ordered]@{}
 foreach($file in Get-ChildItem -LiteralPath $RunDirectory -Recurse -File|Where-Object {$_.Extension -in '.log','.json','.jsonl','.trx','.xml','.ps1','.txt' -and $_.FullName -notmatch '[\\/]strictdoc[\\/]|[\\/]retention[\\/]'}){$raw[[IO.Path]::GetRelativePath($RunDirectory,$file.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $file.FullName).Hash}
 [ordered]@{date='2026-11-16';parentCommit=(& git rev-parse HEAD).Trim();sdk=(& dotnet --version).Trim();platform='Windows local; Linux/CI not executed';audit=@{occurrences=$audit.occurrences;assertionKinds=$audit.assertionKinds;assessments=$audit.implementationAssessments;formalLedgerStates=$audit.formalLedgerStates;formalClosures=0;scope='All113 inventoried occurrences, not all specification/API semantics'};tests=@{passed=[int]$counts.passed;failed=[int]$counts.failed;skipped=[int]$counts.notExecuted};coverage=@{linesCovered=[int]$coverage.coverage.'lines-covered';linesValid=[int]$coverage.coverage.'lines-valid';branchesCovered=[int]$coverage.coverage.'branches-covered';branchesValid=[int]$coverage.coverage.'branches-valid';scope='SharpUnits library only; PowerShell helpers, demo and Java unmeasured'};reference=@{cases=($corpora|Measure-Object cases -Sum).Sum;corpora=$corpora.Count;failed=0;javaTckTests=112;javaTckSubject='Pinned Indriya, not SharpUnits'};requirements=(Get-Content -LiteralPath (Join-Path $RunDirectory 'requirements/strictdoc-evidence.json') -Raw|ConvertFrom-Json);apiSurface=(Get-Content -LiteralPath (Join-Path $RunDirectory 'api-and-probes/api-surface.json') -Raw|ConvertFrom-Json);diagnostics=(Get-Content -LiteralPath (Join-Path $RunDirectory 'api-and-probes/observations.json') -Raw|ConvertFrom-Json);validatorControls=$controls;budgets=$budgets;mutation='No fresh Stryker campaign; prior C#15 compiler block unresolved. Five malformed audit-data controls ran, not production mutation qualification.';randomized='Existing FsCheck replay tests ran in full suite; no new coverage-guided fuzz campaign';sourceScope='Production/test source unchanged throughout runtime runs; audit/docs/helpers authored during audit. Earlier watchdog runs precede informational reserve-field logging change; native memory enforcement unchanged.';canonicalTextSha256=$source;rawArtifactRoot=$RunDirectory;rawSha256=$raw;ci='Off; no enable/dispatch';claim='No normative-completeness, binary-Java compatibility or certification claim'}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath conformance/release-audit-evidence.json
} finally {Pop-Location}
