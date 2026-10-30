$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$units=(Resolve-Path src/SharpUnits/JavaUnitTextParser.cs).Path
$arithmetic=(Resolve-Path src/SharpUnits/JavaDoubleArithmetic.cs).Path
$original=@{};foreach($file in @($units,$arithmetic)){$original[$file]=[IO.File]::ReadAllBytes($file)}
# Compiling semantic mutations challenge the implemented grammar and exact-factor path.
# This bounded campaign does not measure exhaustive library mutation coverage.
$mutants=@(
 @{id='SuperscriptDigit';file=$units;pattern="'²'=>2";replacement="'²'=>3"},
 @{id='ZeroSentinel';file=$units;pattern='if\(magnitude==0\)magnitude=1;';replacement='if(magnitude==0)magnitude=0;'},
 @{id='NegativeRoot';file=$units;pattern='if\(root<0\)';replacement='if(false)'},
 @{id='IntegerContinuation';file=$units;pattern="left=operation=='/'\?left/scalar:left\*scalar;\r?\n                    continue;";replacement="left=operation=='/'?left/scalar:left*scalar;`n                    return left;"},
 @{id='ExactScaleMetadata';file=$arithmetic;pattern='unit\.ExactLinearScale\?\?ScaleOf\(unit\.Converter\)';replacement='ScaleOf(unit.Converter)'},
 @{id='PrefixSign';file=$units;pattern='entry.Value>=0';replacement='entry.Value<0'},
 @{id='ExactRegistration';file=$units;pattern='try\{return registry.Resolve\(symbol\);\}';replacement='try{if(symbol=="nA")throw new KeyNotFoundException();return registry.Resolve(symbol);}'},
 @{id='PrecisionBudget';file=$units;pattern='>16384';replacement='>32768';matches=2}
)
$records=[Collections.Generic.List[object]]::new()
try {
 foreach($mutant in $mutants){
  foreach($file in @($units,$arithmetic)){[IO.File]::WriteAllBytes($file,$original[$file])}
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  $expectedMatches=if($mutant.matches){[int]$mutant.matches}else{1}
  if([regex]::Matches($text,$mutant.pattern).Count -ne $expectedMatches){throw "Unexpected mutation target count: $($mutant.id)"}
  [IO.File]::WriteAllText($mutant.file,[regex]::Replace($text,$mutant.pattern,$mutant.replacement),[Text.UTF8Encoding]::new($false))
  $log="../grammar100-mutation-$($mutant.id).log"
  & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
  if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
  & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~ParserReferenceFixtureTests|FullyQualifiedName~ParserGrammarTests' *>> $log
  $output=Get-Content -LiteralPath $log -Raw
  $failed=[regex]::Match($output,'Failed:\s+(\d+)')
  if(!$failed.Success){throw "Tests did not execute for $($mutant.id)"}
  $record=[ordered]@{id=$mutant.id;compiled=$true;failedTests=[int]$failed.Groups[1].Value;
   result=if([int]$failed.Groups[1].Value -gt 0){'KILLED'}else{'SURVIVED'};
   testEvidence=$output.Substring($output.IndexOf('Test run for'));logSha256=(Get-FileHash $log -Algorithm SHA256).Hash}
  $records.Add($record)
  [pscustomobject]@{id=$record.id;result=$record.result;failedTests=$record.failedTests}|ConvertTo-Json -Compress
 }
}
finally {foreach($file in @($units,$arithmetic)){[IO.File]::WriteAllBytes($file,$original[$file])}}
[ordered]@{method='Eight targeted manual semantic mutations';mutants=@($records);
 limitations='Not exhaustive; Java execution, tests and automation are not mutation-instrumented.'}|ConvertTo-Json -Depth 10|Set-Content conformance/parser-grammar-mutations.json
if(@($records|Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'A mutation survived; inspect evidence'}
