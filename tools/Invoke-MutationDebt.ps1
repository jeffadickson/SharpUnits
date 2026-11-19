param([ValidateSet('Broad','Final','Hash','Typed','Guards','Number')][string]$Phase='Final',[string]$OutputDirectory='../debt-stryker-final-v2',[ValidateRange(1,4)][int]$Workers=4)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 $parent=Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory));if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'}
 if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a fresh output directory; preserve previous mutation evidence'}
 $config=Get-Content -LiteralPath tools/stryker-core.json -Raw|ConvertFrom-Json
 $config.'stryker-config'.concurrency=$Workers
 if($Phase -eq 'Final'){
  $source=[IO.File]::ReadAllText((Resolve-Path -LiteralPath src/SharpUnits/JavaUnit.cs));$ranges=@('**/JavaNumber.cs','**/JavaDimensionAdapter.cs','**/SiPrefixes.cs')
  foreach($spec in @(@('private static readonly string[] baseNames=',"`n"),@('public static JavaUnit FromCatalog','public static IReadOnlyDictionary<string,JavaUnit>'),@('public JavaUnitConverterAdapter GetConverterTo(JavaUnit target)','public JavaUnitConverterAdapter GetConverterToAny'),@('public UnitDefinition ToDefinition','private static IReadOnlyDictionary<string,JavaUnit> BuildCatalog'))){
   $start=$source.IndexOf($spec[0]);$end=if($start -ge 0){$source.IndexOf($spec[1],$start)}else{-1}
   if($start -lt 0 -or $end -le $start){throw ('Missing reviewed statement span: '+$spec[0])}
   $ranges+='**/JavaUnit.cs{'+$start+'..'+$end+'}'
  }
  $config.'stryker-config'.mutate=$ranges
 }
 if($Phase -eq 'Hash'){
  $source=[IO.File]::ReadAllText((Resolve-Path -LiteralPath src/SharpUnits/JavaUnitConverterAdapter.cs))
  $start=$source.IndexOf('public override int GetHashCode()');$end=$source.IndexOf("`n",$start)
  if($start -lt 0 -or $end -le $start){throw 'Missing converter hash method'}
  $config.'stryker-config'.mutate=@('**/JavaUnitConverterAdapter.cs{'+$start+'..'+$end+'}')
  $number=[IO.File]::ReadAllText((Resolve-Path -LiteralPath src/SharpUnits/JavaNumber.cs))
  $start=$number.IndexOf('public override int GetHashCode()=>');$end=$number.IndexOf("`n",$start)
  if($start -lt 0 -or $end -le $start){throw 'Missing numeric hash method'}
  $config.'stryker-config'.mutate+=@('**/JavaNumber.cs{'+$start+'..'+$end+'}')
 }
 if($Phase -eq 'Typed'){
  $source=[IO.File]::ReadAllText((Resolve-Path -LiteralPath src/SharpUnits/JavaUnit.cs))
  $start=$source.IndexOf('public JavaUnitConverterAdapter GetConverterTo(JavaUnit target)');$end=$source.IndexOf('public JavaUnitConverterAdapter GetConverterToAny',$start)
  if($start -lt 0 -or $end -le $start){throw 'Missing typed converter method'}
  $config.'stryker-config'.mutate=@('**/JavaUnit.cs{'+$start+'..'+$end+'}')
 }
 if($Phase -eq 'Guards'){
  $source=[IO.File]::ReadAllText((Resolve-Path -LiteralPath src/SharpUnits/JavaNumberQuantity.cs))
  $start=$source.IndexOf('ArgumentNullException.ThrowIfNull(scalar)');$end=$source.IndexOf("`n",$start)
  if($start -lt 0 -or $end -le $start){throw 'Missing scalar guard'}
  $config.'stryker-config'.mutate=@('**/JavaNumberQuantity.cs{'+$start+'..'+$end+'}')
 }
 if($Phase -eq 'Number'){$config.'stryker-config'.mutate=@('**/JavaNumber.cs')}
 $configuration='tools/stryker-debt-'+$Phase.ToLowerInvariant()+'.json'
 $config|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $configuration
 $hashes=[ordered]@{}
 foreach($directory in @('src','tests')){
  foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in @('.cs','.csproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'}){
   $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
   $hashes[$relative]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 $cases=[ordered]@{};foreach($file in Get-ChildItem -LiteralPath conformance -Recurse -Filter '*cases.json'){$cases[[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $file.FullName).Hash}
 @{canonicalSourceSha256=$hashes;caseBytesSha256=$cases;configuration=$configuration;configSha256=(Get-FileHash -LiteralPath $configuration).Hash;phase=$Phase;workers=$Workers;policy='Freeze all source/test and fixture inputs before replay; never infer kills from ignored or uncompiled mutations'}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath ('conformance/mutation-debt-'+$Phase.ToLowerInvariant()+'-freeze.json')
 $previous=$env:VSTEST_CONNECTION_TIMEOUT
 try {$env:VSTEST_CONNECTION_TIMEOUT='180';& ../tools-runtime/stryker/dotnet-stryker.exe --config-file $configuration --skip-version-check --output $OutputDirectory;if($LASTEXITCODE){throw 'Stryker execution failed; retain diagnostics'}}
 finally {$env:VSTEST_CONNECTION_TIMEOUT=$previous}
} finally {Pop-Location}
