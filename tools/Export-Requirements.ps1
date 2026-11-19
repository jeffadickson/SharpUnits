param([string]$StrictDocCommand='strictdoc',[string]$OutputDirectory='../sharpunits-requirements-html',
    [string]$EvidencePath='conformance/strictdoc-validation.json')
$ErrorActionPreference='Stop'
# StrictDoc is a Python-based requirements tool. The application and new test automation
# remain .NET/PowerShell; this command invokes its supported CLI without custom Python scripts.
& $StrictDocCommand export requirements --no-parallelization --output-dir $OutputDirectory
if($LASTEXITCODE){throw 'StrictDoc requirement validation/export failed'}
$version=(& $StrictDocCommand --version | Out-String).Trim()
if($LASTEXITCODE){throw 'StrictDoc version query failed'}
$documents=Get-ChildItem requirements -Filter '*.sdoc'
$hashes=[ordered]@{}
foreach($document in $documents){$hashes[$document.Name]=(Get-FileHash -LiteralPath $document.FullName -Algorithm SHA256).Hash}
[ordered]@{tool='StrictDoc';version=$version;result='PASS';documents=$documents.Count;
    requirementIds=(Select-String -Path $documents.FullName -Pattern '^UID: ').Count;
    sourceSha256=$hashes;note='Parsing, graph validation and HTML export; not implementation verification.'} |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath
