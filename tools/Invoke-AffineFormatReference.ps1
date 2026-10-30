param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'),
      [string]$Java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe',
      [string]$Python='../tools-runtime/python/python.exe',
      [string]$OutputDirectory='../affine-format-output')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Repository directory is missing'}
Push-Location -LiteralPath $root
try {
    if(!(Test-Path -LiteralPath 'conformance/java-reference/target/classes' -PathType Container)){
        throw 'Compile the pinned Java reference with Maven before running this comparison.'
    }
    if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){
        $parent=Split-Path -Parent $OutputDirectory
        if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Output parent is missing'}
        New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
    }
    & $Dotnet build src/SharpUnits.ConformanceRunner -c Release --no-restore --nologo
    if($LASTEXITCODE){throw 'CLR runner compilation failed'}
    $classpath='conformance/java-reference/target/classes;'+(Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim()
    $javaOutput=Join-Path $OutputDirectory 'java.jsonl'
    $clrOutput=Join-Path $OutputDirectory 'dotnet.jsonl'
    & $Java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.UnitFormatReferenceRunner conformance/unitformat-affine-cases.json > $javaOutput
    if($LASTEXITCODE){throw 'Java observation failed'}
    & $Dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/unitformat-affine-cases.json > $clrOutput
    if($LASTEXITCODE){throw 'CLR observation failed'}
    & $Python tools/compare_java_dotnet.py $javaOutput $clrOutput --cases conformance/unitformat-affine-cases.json
    if($LASTEXITCODE){throw 'Affine unit-format difference detected'}
    # Expected fixtures are retained observations, never regenerated from the CLR implementation.
    $expected=@{};(Get-Content -LiteralPath conformance/unitformat-affine-cases.json -Raw|ConvertFrom-Json).cases |
        ForEach-Object {$expected[$_.id]=$_.expected}
    $rows=@(Get-Content -LiteralPath $javaOutput|ForEach-Object {$_|ConvertFrom-Json})
    if($rows.Count -ne $expected.Count){throw 'Incomplete Java observations'}
    foreach($row in $rows){if(!$expected.ContainsKey($row.id)-or $expected[$row.id] -cne $row.result){throw "Java fixture drift: $($row.id)"}}
} finally {Pop-Location}
