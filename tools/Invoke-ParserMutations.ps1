$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$source=(Resolve-Path src/SharpUnits/JavaQuantityTextParser.cs).Path
$original=[IO.File]::ReadAllBytes($source)
$text=[Text.Encoding]::UTF8.GetString($original)
# Deliberate, compiling semantic changes test specific contracts. This is a bounded
# mutation campaign, not a claim of exhaustive operator or whole-library mutation coverage.
$mutants=@(
    @{id='NBSP';from="'\u00a0'";to="'\u00a1'"},
    @{id='FractionPlusOne';from='ToDouble(fraction)';to='ToDouble(fraction+1)'},
    @{id='RelativeScale';from='new JavaQuantityAdapter(magnitude,unit)';to='new JavaQuantityAdapter(magnitude,unit,JavaQuantityScale.Relative)'},
    @{id='WrongUnit';from='registry.Resolve(symbol)';to='registry.Resolve("A")'},
    @{id='InputLimit';from='text.Length>4096';to='text.Length>8192'}
)
$records=[Collections.Generic.List[object]]::new()
try {
    foreach($mutant in $mutants){
        if(($text.Split($mutant.from).Count-1) -ne 1){throw "Mutation target must be unique: $($mutant.id)"}
        [IO.File]::WriteAllText($source,$text.Replace($mutant.from,$mutant.to),[Text.UTF8Encoding]::new($false))
        $log="../parse50-mutation-$($mutant.id).log"
        & $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> $log
        if($LASTEXITCODE){throw "Noncompiling mutation: $($mutant.id)"}
        & $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~ParserReferenceFixtureTests|FullyQualifiedName~ParserRobustnessTests' *>> $log
        $output=Get-Content -LiteralPath $log -Raw
        $failed=[regex]::Match($output,'Failed:\s+(\d+)')
        if(!$failed.Success){throw "Mutation test did not execute: $($mutant.id)"}
        $record=[ordered]@{id=$mutant.id;compiled=$true;failedTests=[int]$failed.Groups[1].Value;
            result=if([int]$failed.Groups[1].Value -gt 0){'KILLED'}else{'SURVIVED'};
            logSha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash}
        $records.Add($record); $record | ConvertTo-Json -Compress
    }
}
finally {
    # Restore byte-for-byte even when a mutation run or test infrastructure fails.
    [IO.File]::WriteAllBytes($source,$original)
}
[ordered]@{method='Five targeted manual semantic mutations';mutants=@($records);limitations='Parser contracts only; not exhaustive whole-library mutation coverage.'} |
    ConvertTo-Json -Depth 8 | Set-Content conformance/parser-mutations.json
if(@($records | Where-Object {$_.result -eq 'SURVIVED'}).Count){throw 'Surviving mutations require review'}
