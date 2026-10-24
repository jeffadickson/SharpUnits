$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$units=(Resolve-Path src/SharpUnits/QuantityTextFormat.cs).Path
$arithmetic=(Resolve-Path src/SharpUnits/JavaNumberText.cs).Path
$original=@{};foreach($file in @($units,$arithmetic)){$original[$file]=[IO.File]::ReadAllBytes($file)}
# Each semantic mutation must compile and be detected by a formatting assertion.
$mutants=@(
 @{id='RetainedValue';file=$units;pattern='Format\(quantity.Value\)';replacement='Format(quantity.SiQuantity.SiValue)'},
 @{id='CelsiusLabel';file=$units;pattern='return "℃";';replacement='return "°C";'},
 @{id='DimensionlessLabel';file=$units;pattern='return "";';replacement='return "1";'},
 @{id='LocaleFlag';file=$units;pattern='IsLocaleSensitive=>false';replacement='IsLocaleSensitive=>true'},
 @{id='AppendPrefix';file=$units;pattern='return destination.Append';replacement='return destination.Clear().Append'},
 @{id='WriterIdentity';file=$units;pattern='return destination;';replacement='return TextWriter.Null;'},
 @{id='LongNarrowing';file=$arithmetic;pattern='value>=longUpper\?long.MaxValue:\(long\)value';replacement='value>=longUpper?long.MinValue:(long)value'},
 @{id='ScientificBoundary';file=$arithmetic;pattern='if\(exponent is < -3 or >=7\)';replacement='if(exponent is < -4 or >=7)'}
)
$records=[Collections.Generic.List[object]]::new()
try {
 foreach($mutant in $mutants){
  foreach($file in @($units,$arithmetic)){[IO.File]::WriteAllBytes($file,$original[$file])}
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  $expectedMatches=if($mutant.matches){[int]$mutant.matches}else{1}
  if([regex]::Matches($text,$mutant.pattern).Count -ne $expectedMatches){throw "Unexpected mutation target count: $($mutant.id)"}
  [IO.File]::WriteAllText($mutant.file,[regex]::Replace($text,$mutant.pattern,$mutant.replacement),[Text.UTF8Encoding]::new($false))
  $log="../format100-mutation-$($mutant.id).log"
  & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
  if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
  & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~FormatReferenceFixtureTests|FullyQualifiedName~FormattingRobustnessTests' *>> $log
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
 equivalentProbe='Removing the explicit positive-long saturation ternary survived: .NET 10 double-to-long also saturates at this boundary. Excluded from eight non-equivalent mutants; incorrect clamp tested instead.';
 limitations='Not exhaustive; Java execution, tests and automation are not mutation-instrumented.'}|ConvertTo-Json -Depth 10|Set-Content conformance/format-mutations.json
if(@($records|Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'A mutation survived; inspect evidence'}
