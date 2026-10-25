$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$units=(Resolve-Path src/SharpUnits/JavaUnitTextFormat.cs).Path
$arithmetic=(Resolve-Path src/SharpUnits/UnitRegistry.cs).Path
$services=(Resolve-Path src/SharpUnits/JavaServices.cs).Path
$original=@{};foreach($file in @($units,$arithmetic,$services)){$original[$file]=[IO.File]::ReadAllBytes($file)}
# Compile semantic faults, require executed tests, and restore byte-for-byte in finally.
$mutants=@(
 @{id='RelabelHistory';file=$units;pattern='names\[label\]=unit;labels\[unit\]=label;';replacement='if(labels.TryGetValue(unit,out var old))names.Remove(old);names[label]=unit;labels[unit]=label;'},
 @{id='AliasDisplay';file=$units;pattern='lock\(sync\)names\[alias\]=unit;';replacement='lock(sync){names[alias]=unit;labels[unit]=alias;}'},
 @{id='RemoveAliasOwner';file=$units;pattern='lock\(sync\)names.Remove\(alias\);';replacement='lock(sync)if(names.GetValueOrDefault(alias)==unit)names.Remove(alias);'},
 @{id='LabelRestValidation';file=$units;pattern='IdentifierStart\(value\[0\]\)';replacement='value.All(IdentifierStart)'},
 @{id='AsciiUnicodeValidation';file=$units;pattern='value.Any\(c=>c>127\)';replacement='false'},
 @{id='BuilderPrefix';file=$units;pattern='return destination.Append\(Format\(unit\)\);';replacement='return destination.Clear().Append(Format(unit));'},
 @{id='WriterException';file=$units;pattern='destination.Write\(Format\(unit\)\);';replacement='try{destination.Write(Format(unit));}catch(IOException){}'},
 @{id='ProductGraph';file=$units;pattern='case "METRE_PER_SECOND":return M\(\)\+"/"\+S\(\);';replacement='case "METRE_PER_SECOND":return "m/s";'},
 @{id='AliasIdentity';file=$arithmetic;pattern='result.units\[pair.Key\]=pair.Value;';replacement='result.units[pair.Key]=new UnitDefinition(pair.Key,pair.Value.RationalDimension,pair.Value.Converter);'},
 @{id='DynamicSystemLookup';file=$services;pattern='string.Equals\(format.Format\(unit\),text,StringComparison.Ordinal\)';replacement='string.Equals(unit.Symbol,text,StringComparison.Ordinal)'},
 @{id='AsciiFallback';file=$units;pattern='if\(fallback.TryLabel\(unit,out label\)\)return label;';replacement='if(false&&fallback.TryLabel(unit,out label))return label;'},
 @{id='JavaWhitespace';file=$units;pattern='!JavaQuantityTextParser.IsJavaWhitespace\(value\)';replacement='!char.IsWhiteSpace(value)'},
 @{id='ExplicitRegistration';file=$units;pattern='OverlayRegistrations\(units\);';replacement='/* registration overlay deliberately omitted */'}
)
$records=[Collections.Generic.List[object]]::new()
try {
 foreach($mutant in $mutants){
  foreach($file in @($units,$arithmetic,$services)){[IO.File]::WriteAllBytes($file,$original[$file])}
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  $expectedMatches=if($mutant.matches){[int]$mutant.matches}else{1}
  if([regex]::Matches($text,$mutant.pattern).Count -ne $expectedMatches){throw "Unexpected mutation target count: $($mutant.id)"}
  [IO.File]::WriteAllText($mutant.file,[regex]::Replace($text,$mutant.pattern,$mutant.replacement),[Text.UTF8Encoding]::new($false))
  $log="../unitformat100-mutation-$($mutant.id).log"
  & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
  if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
  & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~UnitFormatReferenceFixtureTests|FullyQualifiedName~UnitFormatRobustnessTests' *>> $log
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
finally {foreach($file in @($units,$arithmetic,$services)){[IO.File]::WriteAllBytes($file,$original[$file])}}
[ordered]@{method='Thirteen targeted manual semantic mutations';mutants=@($records);
 limitations='Not exhaustive; Java execution, tests and automation are not mutation-instrumented.'}|ConvertTo-Json -Depth 10|Set-Content conformance/unitformat-mutations.json
if(@($records|Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'A mutation survived; inspect evidence'}
