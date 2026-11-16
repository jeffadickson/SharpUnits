param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container) -or !(Test-Path -LiteralPath $OutputDirectory -PathType Container)){throw 'Missing source/output directory'}
Push-Location -LiteralPath $root
try {
 $jdk=Join-Path (Split-Path -Parent $root) 'tools-runtime/jdk/jdk-17.0.20.1+1'
 if(!(Test-Path -LiteralPath $jdk -PathType Container)){throw 'Missing existing JDK'}
 $jar=@((Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim().Split([IO.Path]::PathSeparator)|Where-Object {$_ -match '[\\/]javax[\\/]measure[\\/]unit-api[\\/]2\.2[\\/]unit-api-2\.2\.jar$'})
 if($jar.Count -ne 1 -or !(Test-Path -LiteralPath $jar[0] -PathType Leaf)){throw 'Expected pinned unit-api2.2 jar absent/ambiguous'}
 $types=@('javax.measure.Quantity','javax.measure.Unit','javax.measure.UnitConverter','javax.measure.Dimension','javax.measure.Prefix','javax.measure.format.UnitFormat','javax.measure.format.QuantityFormat','javax.measure.spi.ServiceProvider','javax.measure.spi.FormatService','javax.measure.spi.QuantityFactory','javax.measure.spi.SystemOfUnits','javax.measure.spi.SystemOfUnitsService')
 $entries=@(foreach($type in $types){
  $lines=@(& (Join-Path $jdk 'bin/javap.exe') -public -classpath $jar[0] $type)
  if($LASTEXITCODE){throw ('javap failed: '+$type)}
  $lines|Set-Content -LiteralPath (Join-Path $OutputDirectory ($type+'.txt'))
  [ordered]@{type=$type;declarations=@($lines|Where-Object {$_ -match '^  public '});scope='Declared public members only; not inherited API, documentation semantics or conformance.'}
 })
 [ordered]@{api='javax.measure unit-api2.2';jarSha256=(Get-FileHash -LiteralPath $jar[0]).Hash;types=$entries;note='Surface inventory complements the113 TCK occurrences; not an exhaustive specification/behavior audit.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'api-surface.json')
} finally {Pop-Location}
