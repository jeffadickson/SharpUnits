$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$units=(Resolve-Path src/SharpUnits/JavaServices.cs).Path
$arithmetic=(Resolve-Path src/SharpUnits/JavaUnitCatalogData.cs).Path
$original=@{};foreach($file in @($units,$arithmetic)){$original[$file]=[IO.File]::ReadAllBytes($file)}
# Each non-equivalent mutation must compile, execute the targeted tests, and be detected.
$mutants=@(
 @{id='ProviderPriority';file=$units;pattern='Priority=>10';replacement='Priority=>0'},
 @{id='UnitLookupCase';file=$units;pattern='string.Equals\(format.Format\(unit\),text,StringComparison.Ordinal\)';replacement='string.Equals(format.Format(unit),text,StringComparison.OrdinalIgnoreCase)'},
 @{id='NullTypeLookup';file=$units;pattern='quantityType is null\?null:types.GetValueOrDefault\(quantityType\)';replacement='quantityType is null?throw new ArgumentNullException(nameof(quantityType)):types.GetValueOrDefault(quantityType)'},
 @{id='FormatLocale';file=$units;pattern='name.ToUpper\(System.Globalization.CultureInfo.CurrentCulture\)';replacement='name.ToUpperInvariant()';matches=2},
 @{id='BinaryMetadata';file=$units;pattern='new JavaPrefix\(symbol,1024,index\+1\)';replacement='new JavaPrefix(symbol,2,10*(index+1))'},
 @{id='TemperatureType';file=$arithmetic;pattern='new\("Temperature","K"\)';replacement='new("Temperature","℃")'},
 @{id='CapacitanceDimension';file=$arithmetic;pattern='new Dimension\(-2,-1,4,2,0,0,0\)';replacement='new Dimension(-2,-1,3,2,0,0,0)'},
 @{id='ExactSpeedFactor';file=$arithmetic;pattern='new Rational\(5,18\)';replacement='new Rational(2777777777777778L,10000000000000000L)'}
)
$records=[Collections.Generic.List[object]]::new()
try {
 foreach($mutant in $mutants){
  foreach($file in @($units,$arithmetic)){[IO.File]::WriteAllBytes($file,$original[$file])}
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  $expectedMatches=if($mutant.matches){[int]$mutant.matches}else{1}
  if([regex]::Matches($text,$mutant.pattern).Count -ne $expectedMatches){throw "Unexpected mutation target count: $($mutant.id)"}
  [IO.File]::WriteAllText($mutant.file,[regex]::Replace($text,$mutant.pattern,$mutant.replacement),[Text.UTF8Encoding]::new($false))
  $log="../services100-mutation-$($mutant.id).log"
  & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
  if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
  & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~ServiceReferenceFixtureTests|FullyQualifiedName~JavaServiceRobustnessTests' *>> $log
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
 limitations='Not exhaustive; Java execution, tests and automation are not mutation-instrumented.'}|ConvertTo-Json -Depth 10|Set-Content conformance/service-gap-mutations.json
if(@($records|Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'A mutation survived; inspect evidence'}
