param([ValidateSet('Validate','Capture')][string]$Mode='Validate')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
    $pins=Get-Content -LiteralPath conformance/upstream-pins.json -Raw|ConvertFrom-Json
    foreach($name in @('tck','indriya')){
        $directory=if($name -eq 'tck'){'upstream/unit-tck'}else{'upstream/indriya'}
        if(!(Test-Path -LiteralPath $directory -PathType Container)){throw 'Missing pinned Java checkout'}
        & git -C $directory diff --quiet HEAD
        if($LASTEXITCODE){throw ('Pinned Java checkout has tracked edits: '+$directory)}
        $actual=(& git -C $directory rev-parse HEAD).Trim()
        if($LASTEXITCODE -or $actual -ne $pins.$name.commit){throw ('Upstream pin differs: '+$name)}
    }
    foreach($directory in @('src','tests','conformance/java-reference/target/classes/org/sharpunits')){if(!(Test-Path -LiteralPath $directory -PathType Container)){throw ('Missing source/output directory: '+$directory)}}
    $source=[ordered]@{}
    # Canonical UTF-8 text/LF hashes are portable across Git's Windows newline conversion.
    $paths=@(Get-ChildItem -LiteralPath src,tests -Recurse -File|Where-Object {$_.Extension -in @('.cs','.csproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'}|ForEach-Object {[IO.Path]::GetRelativePath($root,$_.FullName).Replace('\','/')})
    $paths+=@(Get-ChildItem -LiteralPath tools -File|Where-Object {$_.Extension -in @('.py','.cs','.ps1','.json','.coveragerc','.txt')}|ForEach-Object {'tools/'+$_.Name})
    $paths+=@(Get-ChildItem -LiteralPath conformance -Recurse -File|Where-Object {$_.Name -like '*cases.json' -or $_.Extension -eq '.java' -or $_.Name -eq 'pom.xml'}|ForEach-Object {[IO.Path]::GetRelativePath($root,$_.FullName).Replace('\','/')})
    $paths+=@('.github/workflows/ci.yml','.github/workflows/java-reference.yml','global.json','Directory.Build.props')|Where-Object {Test-Path -LiteralPath $_ -PathType Leaf}
    foreach($path in ($paths|Sort-Object -Unique)){
        $text=[IO.File]::ReadAllText((Join-Path $root $path)).Replace("`r`n","`n")
        $source[$path]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
    }
    $jars=[ordered]@{}
    foreach($path in (Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim().Split([IO.Path]::PathSeparator)){
        if(!(Test-Path -LiteralPath $path -PathType Leaf)){throw 'Missing Java dependency'}
        $key=[IO.Path]::GetFileName($path);if($jars.Contains($key)){throw 'Ambiguous duplicate Java artifact name'}
        $jars[$key]=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    }
    $classes=[ordered]@{}
    foreach($file in (Get-ChildItem -LiteralPath conformance/java-reference/target/classes/org/sharpunits -Filter '*.class')){
        $classes[$file.Name]=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    if(!$classes.Count){throw 'Compile the Java core observer before capturing/validating its lock'}
    $path='conformance/verification-reference-lock.json'
    $actual=[ordered]@{schemaVersion=1;tckCommit=$pins.tck.commit;indriyaCommit=$pins.indriya.commit;canonicalTextSha256=$source;javaArtifactSha256=$jars;javaObserverClassSha256=$classes}
    if($Mode -eq 'Capture'){
        $actual|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $path
        Write-Output 'Captured authoring lock; this is not execution or reviewer approval.'
    } else {
        $expected=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json -AsHashtable
        foreach($name in @('tckCommit','indriyaCommit')){if($expected[$name] -ne $actual[$name]){throw ('Reference pin drift: '+$name)}}
        foreach($name in @('canonicalTextSha256','javaArtifactSha256','javaObserverClassSha256')){
            if($expected[$name].Count -ne $actual[$name].Count){throw ('Reference inventory drift: '+$name)}
            foreach($key in $actual[$name].Keys){if($expected[$name][$key] -ne $actual[$name][$key]){throw ('Reference content drift: '+$key)}}
        }
        Write-Output 'Pinned runtime, compiled observer, source, test and corpus lock validated.'
    }
} finally {Pop-Location}
