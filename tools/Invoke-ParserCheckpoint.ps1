param([Parameter(Mandatory)][ValidateSet(10,20,30,40,50)][int]$Count)
$ErrorActionPreference='Stop'
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$env:JAVA_HOME=(Resolve-Path '../tools-runtime/jdk/jdk-17.0.20.1+1').Path
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
& "$env:JAVA_HOME/bin/java.exe" -cp $classpath org.sharpunits.ReferenceRunner conformance/java-reference/cases.json > "../parse50-java-$Count.jsonl"
if($LASTEXITCODE){throw 'Java fixtures failed'}
& $dotnet build tests/SharpUnits.Tests -c Release --nologo *> "../parse50-build-$Count.log"
if($LASTEXITCODE){Get-Content "../parse50-build-$Count.log" -Tail 20;throw 'Build failed'}
& $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo *> "../parse50-test-$Count.log"
if($LASTEXITCODE){Get-Content "../parse50-test-$Count.log" -Tail 30;throw 'Tests failed'}
& $dotnet build src/SharpUnits.ConformanceRunner -c Release --nologo *> "../parse50-runner-$Count.log"
if($LASTEXITCODE){throw 'Runner build failed'}
& $dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/java-reference/cases.json > "../parse50-dotnet-$Count.jsonl"
if($LASTEXITCODE){throw 'CLR fixtures failed'}
& $dotnet run --file tools/ParserReview.cs -- record $Count
if($LASTEXITCODE){throw 'Comparison failed'}
Get-Content "../parse50-test-$Count.log" -Tail 2
