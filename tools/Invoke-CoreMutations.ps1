param([string]$Stryker='../tools-runtime/stryker/dotnet-stryker.exe',
      [string]$OutputDirectory='../wp1-stryker-focused-replay')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing repository'}
Push-Location -LiteralPath $root
try {
    $outputParent=Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory))
    if(!(Test-Path -LiteralPath $outputParent -PathType Container)){throw 'Mutation output parent missing'}
    # Stryker uses character offsets. Recalculate after edits and newline conversion.
    $config=Get-Content -LiteralPath tools/stryker-core.json -Raw|ConvertFrom-Json
    $config.'stryker-config'.concurrency=1
    $config.'stryker-config'.'test-case-filter'='FullyQualifiedName~CoreRobustnessTests'
    $ranges=@()
    foreach($spec in @(
        @('JavaNumber.cs','public static JavaNumber FromDoubleAsRational','/// <summary>Accepts'),
        @('JavaUnitConverterAdapter.cs','public static JavaUnitConverterAdapter ForPrefix','/// <summary>Creates an additive'))){
        $source=[IO.File]::ReadAllText((Resolve-Path -LiteralPath (Join-Path src/SharpUnits $spec[0])))
        $start=$source.IndexOf($spec[1]);$end=$source.IndexOf($spec[2],$start)
        if($start -lt 0 -or $end -le $start){throw 'Mutation implementation span missing'}
        $ranges+='**/'+$spec[0]+'{'+$start+'..'+$end+'}'
    }
    $config.'stryker-config'.mutate=$ranges
    $temporary=Join-Path ([IO.Path]::GetTempPath()) ('sharpunits-core-'+[guid]::NewGuid()+'.json')
    $config|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $temporary
    $previousTimeout=$env:VSTEST_CONNECTION_TIMEOUT
    try {
        $env:VSTEST_CONNECTION_TIMEOUT='180'
        & $Stryker -f $temporary --skip-version-check -O $OutputDirectory
        if($LASTEXITCODE){throw 'Stryker failed; retain the log and do not claim completed results'}
    } finally {
        $env:VSTEST_CONNECTION_TIMEOUT=$previousTimeout
        Remove-Item -LiteralPath $temporary
    }
} finally {Pop-Location}
