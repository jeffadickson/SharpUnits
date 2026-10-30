param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
    # Each replay owns a fresh snapshot. Never mutate the active library or tests.
    $parent=Split-Path -Parent $root
    if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'}
    $name='verification-order-'+[guid]::NewGuid().ToString('N')
    $copy=Join-Path $parent $name;$archive=Join-Path $parent ($name+'.zip')
    if(Test-Path -LiteralPath $copy){throw 'Snapshot path unexpectedly exists'}
    & git archive 563a692d0f7b7c78e147eb09965d97d933bc56f6 -o $archive
    if($LASTEXITCODE){throw 'Cannot snapshot audited baseline'}
    New-Item -ItemType Directory -Path $copy|Out-Null
    Expand-Archive -LiteralPath $archive -DestinationPath $copy
    $tests=Join-Path $copy 'tests/SharpUnits.Tests/SharpUnits.Tests.csproj'
    $filter='FullyQualifiedName~ConcatenationAndNativeThenHaveExplicitOppositeOrders|FullyQualifiedName~TransformAndPrefixApplyBeforeTheParentConverter'
    & $Dotnet test $tests -c Release --filter $filter *> (Join-Path $copy 'control.log')
    if($LASTEXITCODE){throw 'Unmodified ordering control failed'}
    foreach($fault in @(
        @('JavaUnitConverterAdapter.cs','new(steps.Concat(argument.steps))','new(argument.steps.Concat(steps))','concatenate'),
        @('JavaUnit.cs','parent!.SystemConverter.Concatenate(operation!)','operation!.Concatenate(parent!.SystemConverter)','transform'))){
        $file=Join-Path $copy ('src/SharpUnits/'+$fault[0]);$source=[IO.File]::ReadAllText($file)
        if(([regex]::Matches($source,[regex]::Escape($fault[1]))).Count -ne 1){throw 'Expected exactly one historical fault injection target'}
        try {
            [IO.File]::WriteAllText($file,$source.Replace($fault[1],$fault[2]))
            & $Dotnet test $tests -c Release --no-restore --filter $filter *> (Join-Path $copy ($fault[3]+'.log'))
            if(!$LASTEXITCODE){throw ('Historical ordering fault survived: '+$fault[3])}
            # Nonzero alone can be a compiler failure; require the test runner's failure summary.
            if(!(Select-String -LiteralPath (Join-Path $copy ($fault[3]+'.log')) -Pattern 'Failed!.*Failed:\s+[12],')){
                throw 'Fault did not fail through the expected test assertions'
            }
        } finally {[IO.File]::WriteAllText($file,$source)}
    }
    Write-Output ('Directed ordering replay logs: '+$copy)
} finally {Pop-Location}
# Expected mutant test failures must not leak a nonzero native exit status to callers.
exit 0
