param([Parameter(Mandatory)][string]$CoveragePath,[Parameter(Mandatory)][string]$OutputPath)
$ErrorActionPreference='Stop'
# Cobertura is produced by coverlet.collector; report only measured lines and branches.
# Generated regex internals, Java, tooling and uninstrumented runner code are not implied covered.
[xml]$report=Get-Content -LiteralPath $CoveragePath
$classes=@($report.coverage.packages.package.classes.class | Where-Object { $_.filename -match 'JavaServices.cs|JavaUnitCatalogData.cs|JavaNumberText.cs|JavaQuantityTextParser.cs|JavaUnitTextParser.cs|QuantityTextFormat.cs|JavaDoubleArithmetic.cs|JavaQuantityAdapter.cs|UnitConversion.cs|SiPrefixes.cs' })
$records=foreach($class in $classes){
    $lines=@($class.lines.line)
    $branchesCovered=0; $branchesValid=0
    foreach($line in $lines){
        if($line.'condition-coverage' -match '\((\d+)/(\d+)\)'){
            $branchesCovered += [int]$Matches[1]; $branchesValid += [int]$Matches[2]
        }
    }
    [ordered]@{name=$class.name;file=$class.filename;linesCovered=@($lines | Where-Object {[int]$_.hits -gt 0}).Count;
        linesValid=$lines.Count;branchesCovered=$branchesCovered;branchesValid=$branchesValid;
        lineRate=[double]$class.'line-rate';branchRate=[double]$class.'branch-rate';
        uncoveredLineNumbers=@($lines | Where-Object {[int]$_.hits -eq 0} | ForEach-Object {[int]$_.number});
        partiallyCoveredBranchLines=@($lines | Where-Object {$_.branch -eq 'True' -and $_.'condition-coverage' -notmatch '^100%'} | ForEach-Object {[int]$_.number})}
}
$summary=[ordered]@{tool='coverlet.collector 6.0.4';reportSha256=(Get-FileHash -LiteralPath $CoveragePath -Algorithm SHA256).Hash;
    overall=[ordered]@{linesCovered=[int]$report.coverage.'lines-covered';linesValid=[int]$report.coverage.'lines-valid';
        branchesCovered=[int]$report.coverage.'branches-covered';branchesValid=[int]$report.coverage.'branches-valid'};
    affectedClasses=@($records);limitations='Instrumented managed library only; no Java/tooling coverage or proof of semantic correctness.'}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath
$summary | ConvertTo-Json -Depth 8
