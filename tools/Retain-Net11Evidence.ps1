param([Parameter(Mandatory)][string]$RunDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container) -or !(Test-Path -LiteralPath $RunDirectory -PathType Container)){throw 'Missing source/run directory'}
Push-Location -LiteralPath $root
try {
 & ./tools/Validate-CompletionPlan.ps1
 $states=[ordered]@{}
 foreach($phase in @('final-tests3','reference3','requirements2','demo2','rounding-final')){
  $value=Get-Content -LiteralPath (Join-Path $RunDirectory ($phase+'/watchdog.json')) -Raw|ConvertFrom-Json
  if($value.outcome -ne 'completed' -or $value.exitCode -ne 0 -or $value.effectiveFlags -ne 8960){throw ('Incomplete phase: '+$phase)}
  $states[$phase]=$value
 }
 $coverageFile=Get-ChildItem -LiteralPath (Join-Path $RunDirectory 'final-tests3/coverage') -Recurse -Filter coverage.cobertura.xml|Select-Object -First 1
 [xml]$coverage=Get-Content -LiteralPath $coverageFile.FullName -Raw
 [xml]$trx=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-tests3/coverage/typed-errors.trx') -Raw
 $counters=$trx.TestRun.ResultSummary.Counters
 if([int]$counters.failed -ne 0 -or [int]$counters.total -ne [int]$counters.passed){throw 'Incomplete suite'}
 $corpora=Get-Content -LiteralPath (Join-Path $RunDirectory 'reference3/corpora/summary.json') -Raw|ConvertFrom-Json
 if(@($corpora|Where-Object {$_.expectedCheckExit -ne 0 -or $_.differentialExit -ne 0}).Count){throw 'Failed differential corpus'}
 $source=[ordered]@{}
 foreach($directory in @('src','tests','demo','tools','requirements','examples','.github/workflows','.config')){
  if(!(Test-Path -LiteralPath $directory -PathType Container)){throw ('Missing source directory: '+$directory)}
  foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in '.cs','.csproj','.ps1','.py','.sdoc','.json','.yml' -and $_.FullName -notmatch '[\\/](obj|bin)[\\/]'}){
   $path=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
   $source[$path]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 $source['global.json']=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $root 'global.json')).Replace("`r`n","`n"))))
 $raw=[ordered]@{}
 foreach($file in Get-ChildItem -LiteralPath $RunDirectory -Recurse -File|Where-Object {$_.Extension -in '.log','.json','.jsonl','.trx','.xml','.csv','.ps1' -and $_.FullName -notmatch '[\\/]strictdoc[\\/]|[\\/]retention[\\/]'}){
  $raw[[IO.Path]::GetRelativePath($RunDirectory,$file.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $file.FullName).Hash
 }
 $mutationLog=Get-Content -LiteralPath (Join-Path $RunDirectory 'mutation-final2/stdout.log') -Raw
 if($mutationLog -notmatch 'CS8652' -or $mutationLog -notmatch 'collection expression arguments'){throw 'Expected mutation compiler limitation not observed; reassess evidence'}
 $evidence=[ordered]@{
  date='2026-11-13';parentCommit=(& git rev-parse HEAD).Trim();sdk='11.0.100';runtime='11.0.0';language='C#15 default, no preview';target='net11.0'
  tests=@{passed=[int]$counters.passed;failed=[int]$counters.failed;skipped=0}
  coverage=@{linesCovered=[int]$coverage.coverage.'lines-covered';linesValid=[int]$coverage.coverage.'lines-valid';branchesCovered=[int]$coverage.coverage.'branches-covered';branchesValid=[int]$coverage.coverage.'branches-valid';scope='Managed SharpUnits library; demo/tools/Java unmeasured. Compiler/runtime changed: not a like-for-like .NET10 coverage comparison.'}
  reference=@{corpora=@($corpora).Count;cases=($corpora|Measure-Object cases -Sum).Sum;failed=0}
  requirements=(Get-Content -LiteralPath (Join-Path $RunDirectory 'requirements2/strictdoc-evidence.json') -Raw|ConvertFrom-Json)
  mutation=@{tool='Stryker5.1.0 local manifest';status='BLOCKED_COMPILER';diagnostic='CS8652: embedded compiler treats GA collection-expression arguments as preview';qualifiedOutcomes=0;processExitZeroIsNotSuccess=$true;scope='Attempted SiPrefixes and metric membership construction only; no .NET11 mutation closure claimed'}
  randomized=@{framework='FsCheck3.2.0';seed='385,1113';configuredTrials=300;roundingOracle='Existing Python Fraction, seed385,200 cases';coverageGuidedFuzzer='not run'}
  implementation='FrozenDictionary plus ordered ImmutableArray snapshot; C#15 comparer arguments plus FrozenSet. Public metric sequence preserved.'
  unchangedDependencies='xUnit2.9.2, FsCheck3.2.0, TestSdk17.11.1, Coverlet6.0.4 actually executed; newer candidates assessed but not auto-upgraded'
  ownershipMapSha256=(Get-FileHash conformance/completion-work-packages.json).Hash;owners=@{WP1=60;WP2=11;WP3=20;WP4=22;WP5='all113 cross-cutting'}
  budgets=$states;canonicalTextSha256=$source;rawArtifactRoot=$RunDirectory;rawSha256=$raw
  ci='Off; manual-only Windows/Ubuntu workflow source updated, no workflow enabled/dispatched';formalTckClosures=0;platform='Windows local; Linux/CI not executed';nativeAot='Not run; examples executed managed'
 }
 $evidence|ConvertTo-Json -Depth 12|Set-Content -LiteralPath conformance/net11-migration-evidence.json
} finally {Pop-Location}
