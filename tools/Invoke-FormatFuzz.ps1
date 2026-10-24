param([string]$Dotnet='C:/Users/jeffd/.dotnet/dotnet.exe',
      [string]$Java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe')
$ErrorActionPreference='Stop'
# Compare exact text, including Java's Number narrowing. No string tolerance is applied.
& $Dotnet run --file tools/Generate-FormatFuzz.cs -- ../format100-fuzz-input.json
if($LASTEXITCODE){throw 'Input generation failed'}
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
& $Java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.ReferenceRunner ../format100-fuzz-input.json > ../format100-fuzz-java.jsonl
if($LASTEXITCODE){throw 'Java execution failed'}
& $Dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll ../format100-fuzz-input.json > ../format100-fuzz-dotnet.jsonl
if($LASTEXITCODE){throw 'CLR execution failed'}
$javaRows=@(Get-Content ../format100-fuzz-java.jsonl|ForEach-Object {$_|ConvertFrom-Json})
$dotnetRows=@(Get-Content ../format100-fuzz-dotnet.jsonl|ForEach-Object {$_|ConvertFrom-Json})
if($javaRows.Count -ne 512 -or $dotnetRows.Count -ne 512){throw 'Incomplete fuzz execution'}
$javaById=@{};foreach($row in $javaRows){$javaById[$row.id]=$row.result}
$inputs=(Get-Content ../format100-fuzz-input.json -Raw|ConvertFrom-Json).cases
$counterexamples=@(foreach($row in $dotnetRows){
    if(!$javaById.ContainsKey($row.id)){throw 'Missing Java observation'}
    if($row.result -cne $javaById[$row.id]){
        [ordered]@{id=$row.id;input=@($inputs|Where-Object id -eq $row.id)[0];java=$javaById[$row.id];dotnet=$row.result}
    }
})
[ordered]@{seed=385415;cases=512;mismatches=$counterexamples.Count;counterexamples=$counterexamples;
    inputSha256=(Get-FileHash ../format100-fuzz-input.json -Algorithm SHA256).Hash;
    javaOutputSha256=(Get-FileHash ../format100-fuzz-java.jsonl -Algorithm SHA256).Hash;
    dotnetOutputSha256=(Get-FileHash ../format100-fuzz-dotnet.jsonl -Algorithm SHA256).Hash;
    limitations='Seeded sampling, not exhaustive binary64 verification; arbitrary Java Number types excluded.'}|
    ConvertTo-Json -Depth 10|Set-Content conformance/format-fuzz.json
if($counterexamples.Count){throw 'Formatting differential mismatch; counterexamples retained'}
