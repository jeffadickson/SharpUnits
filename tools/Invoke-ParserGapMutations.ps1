$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$quantity=(Resolve-Path src/SharpUnits/JavaQuantityTextParser.cs).Path
$units=(Resolve-Path src/SharpUnits/JavaUnitTextParser.cs).Path
$original=@{};foreach($file in @($quantity,$units)){$original[$file]=[IO.File]::ReadAllBytes($file)}
# These compiling changes challenge each repaired contract; the campaign is targeted,
# not an exhaustive library mutation score. Original bytes are restored on every exit.
$mutants=@(
 @{id='DenominatorWhitespace';file=$quantity;pattern='position\+\+;\r?\n            SkipWhitespace\(text,ref position\);';replacement='position++;'},
 @{id='CelsiusAlias';file=$units;pattern='if\(symbol=="℃"\)return registry.Resolve\("°C"\);';replacement='if(symbol=="℃")return registry.Resolve("K");'},
 @{id='ReverseDivision';file=$units;pattern='left/right';replacement='right/left'},
 @{id='LosePrefixScale';file=$units;pattern='UnitConversion.Linear\(expression.Scale\)';replacement='UnitConversion.Linear(1)'},
 @{id='NestingLimit';file=$units;pattern='depth>64';replacement='depth>128'}
)
$records=[Collections.Generic.List[object]]::new()
try {
 foreach($mutant in $mutants){
  foreach($file in @($quantity,$units)){[IO.File]::WriteAllBytes($file,$original[$file])}
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  if([regex]::Matches($text,$mutant.pattern).Count -ne 1){throw "Nonunique target: $($mutant.id)"}
  [IO.File]::WriteAllText($mutant.file,[regex]::Replace($text,$mutant.pattern,$mutant.replacement),[Text.UTF8Encoding]::new($false))
  $log="../gap50-mutation-$($mutant.id).log"
  & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
  if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
  & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~ParserReferenceFixtureTests|FullyQualifiedName~ParserGapTests' *>> $log
  $output=Get-Content -LiteralPath $log -Raw
  $failed=[regex]::Match($output,'Failed:\s+(\d+)')
  if(!$failed.Success){throw "Tests did not run for $($mutant.id)"}
  $record=[ordered]@{id=$mutant.id;compiled=$true;failedTests=[int]$failed.Groups[1].Value;
   result=if([int]$failed.Groups[1].Value -gt 0){'KILLED'}else{'SURVIVED'};
   testEvidence=$output.Substring($output.IndexOf('Test run for'));logSha256=(Get-FileHash $log -Algorithm SHA256).Hash}
  $records.Add($record)
  [pscustomobject]@{id=$record.id;result=$record.result;failedTests=$record.failedTests}|ConvertTo-Json -Compress
 }
}
finally {foreach($file in @($quantity,$units)){[IO.File]::WriteAllBytes($file,$original[$file])}}
[ordered]@{method='Five targeted manual semantic mutations of the repaired parser gaps';mutants=@($records);
 limitations='Not exhaustive; test code and Java runtime are not mutation-tested.'}|ConvertTo-Json -Depth 10|Set-Content conformance/parser-gap-mutations.json
if(@($records|Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'Mutation survived; inspect evidence'}
