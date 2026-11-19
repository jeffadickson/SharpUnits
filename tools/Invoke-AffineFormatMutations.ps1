param([string]$Stryker='../tools-runtime/stryker/dotnet-stryker.exe',
      [string]$OutputDirectory='../affine-stryker-output-final')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing repository'}
Push-Location -LiteralPath $root
try {
    # Stryker ranges are character offsets, not line numbers. Recompute after edits/checkout
    # newline conversion so a stale numeric span cannot silently ignore the changed code.
    $source=[IO.File]::ReadAllText((Resolve-Path src/SharpUnits/JavaUnitTextFormat.cs))
    $ranges='';$count=0
    foreach($pattern in @('(?m)^        if\(IsStandardFahrenheit\(unit\)\)return[^\r\n]*',
        '(?ms)^    private static bool IsStandardFahrenheit.*?;',
        '(?m)^            if\(IsStandardFahrenheit\(unit\)\)\{[^\r\n]*')){
        foreach($match in [regex]::Matches($source,$pattern)){
            $ranges+='{'+$match.Index+'..'+($match.Index+$match.Length-1)+'}';$count++
        }
    }
    if($count -ne 4){throw 'Expected four Fahrenheit implementation spans'}
    $config=Get-Content -LiteralPath tools/stryker-affine.json -Raw|ConvertFrom-Json
    $config.'stryker-config'.mutate=@('**/JavaUnitTextFormat.cs'+$ranges)
    $temporary=Join-Path ([IO.Path]::GetTempPath()) ('sharpunits-affine-'+[guid]::NewGuid()+'.json')
    $config|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $temporary
    try {
        & $Stryker -f $temporary --skip-version-check -O $OutputDirectory
        if($LASTEXITCODE){throw 'Stryker execution failed'}
        $report=Get-Content -LiteralPath (Join-Path $OutputDirectory 'reports/mutation-report.json') -Raw|ConvertFrom-Json
        $mutants=@($report.files.PSObject.Properties|ForEach-Object {$_.Value.mutants}|Where-Object status -NotIn @('Ignored','CompileError'))
        if(!$mutants.Count){throw 'No executable mutants selected; scope configuration is invalid'}
        if(@($mutants|Where-Object status -ne 'Killed').Count){throw 'A mutant was not killed; review survivors, missing coverage and timeouts'}
    } finally {Remove-Item -LiteralPath $temporary}
} finally {Pop-Location}
