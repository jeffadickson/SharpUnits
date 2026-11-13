$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $repository -PathType Container)){throw 'Missing checkout'}
$source=[IO.File]::ReadAllText((Join-Path $repository 'src/SharpUnits/JavaNumber.cs'))
$ranges=@('**/NumericResult.cs')
foreach($span in @(
 @('public static JavaNumber Decimal(string text)','/// <summary>Creates and narrows a finite Java Double'),
 @('public static JavaNumber FromNumeric(object value)','/// <summary>Adds values with decimal promotion'),
 @('private static void Check(Rational value)','/// <summary>Projects to binary64; rational values')
)){
 $start=$source.IndexOf($span[0],[StringComparison]::Ordinal)
 $end=if($start -ge 0){$source.IndexOf($span[1],$start,[StringComparison]::Ordinal)}else{-1}
 if($start -lt 0 -or $end -le $start){throw ('Changed source span not found: '+$span[0])}
 $ranges+='**/JavaNumber.cs{'+$start+'..'+$end+'}'
}
$config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'stryker-typed-errors.json') -Raw|ConvertFrom-Json
$config.'stryker-config'.mutate=$ranges
$config.'stryker-config'|Add-Member -NotePropertyName 'mutation-level' -NotePropertyValue 'Standard' -Force
$path=Join-Path $PSScriptRoot 'stryker-typed-errors-focused.json'
$config|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $path
@{configSha256=(Get-FileHash -LiteralPath $path).Hash;javaNumberSourceSha256=(Get-FileHash -LiteralPath (Join-Path $repository 'src/SharpUnits/JavaNumber.cs')).Hash;numericResultSourceSha256=(Get-FileHash -LiteralPath (Join-Path $repository 'src/SharpUnits/NumericResult.cs')).Hash;scope='Standard mutation level; full NumericResult and JavaNumber Decimal/ParseDecimal, FromNumeric/ConvertNumeric, Check/WithinBudget spans; other methods excluded; offsets regenerated for current line endings'}|
 ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $repository 'conformance/typed-errors-focused-freeze.json')
