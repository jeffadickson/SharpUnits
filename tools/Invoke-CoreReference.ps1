param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'),
      [string]$JavaHome='../tools-runtime/jdk/jdk-17.0.20.1+1',
      [string]$Maven='../tools-runtime/apache-maven-3.9.9/bin/mvn.cmd',
      [string]$Python='../tools-runtime/python/python.exe',
      [string]$OutputDirectory='../wp1-reference-output')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Repository directory missing'}
Push-Location -LiteralPath $root
try {
    if(!(Test-Path -LiteralPath $JavaHome -PathType Container)){throw 'Pinned Java runtime missing'}
    if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){
        $parent=Split-Path -Parent $OutputDirectory
        if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Output parent missing'}
        New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
    }
    $previousJavaHome=$env:JAVA_HOME;$previousPath=$env:PATH
    try {
        $env:JAVA_HOME=(Resolve-Path -LiteralPath $JavaHome).Path
        $env:PATH=(Join-Path $env:JAVA_HOME 'bin')+';'+$env:PATH
        & $Maven -o -q -f conformance/java-reference/pom.xml compile
        if($LASTEXITCODE){throw 'Pinned Java fixture compilation failed'}
        $classpath='conformance/java-reference/target/classes;'+(Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim()
        $javaOutput=Join-Path $OutputDirectory 'java.jsonl'
        & (Join-Path $env:JAVA_HOME 'bin/java.exe') '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.CoreReferenceRunner conformance/work-package-one-cases.json > $javaOutput
        if($LASTEXITCODE){throw 'Pinned Java execution failed'}
    } finally {$env:JAVA_HOME=$previousJavaHome;$env:PATH=$previousPath}
    & $Dotnet build src/SharpUnits.ConformanceRunner -c Release --no-restore --nologo
    if($LASTEXITCODE){throw 'CLR runner compilation failed'}
    $clrOutput=Join-Path $OutputDirectory 'dotnet.jsonl'
    & $Dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/work-package-one-cases.json > $clrOutput
    if($LASTEXITCODE){throw 'CLR observation failed'}
    $fixtures=(Get-Content -LiteralPath conformance/work-package-one-cases.json -Raw|ConvertFrom-Json).cases
    $rows=@(Get-Content -LiteralPath $javaOutput|ForEach-Object {$_|ConvertFrom-Json})
    if($rows.Count -ne $fixtures.Count){throw 'Incomplete Java observations'}
    $expected=@{};foreach($fixture in $fixtures){$expected[$fixture.id]=$fixture.expected}
    foreach($row in $rows){
        if(!$expected.ContainsKey($row.id)-or (ConvertTo-Json -InputObject $expected[$row.id] -Compress) -cne (ConvertTo-Json -InputObject $row.result -Compress)){
            throw "Retained Java expectation drifted: $($row.id)"
        }
    }
    & $Python tools/compare_java_dotnet.py $javaOutput $clrOutput
    if($LASTEXITCODE){throw 'Java/CLR difference detected'}
} finally {Pop-Location}
