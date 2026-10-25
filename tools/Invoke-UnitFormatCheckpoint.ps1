param([Parameter(Mandatory)][ValidateRange(10,100)][int]$Count)
$ErrorActionPreference='Stop'
if($Count%10){throw 'Use a multiple of ten'}
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe'
& $dotnet run --file tools/UnitFormatReview.cs -- prepare $Count
if($LASTEXITCODE){throw 'Preparation failed'}
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
& $java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.UnitFormatReferenceRunner conformance/unitformat-reference-cases.json > "../unitformat100-java-$Count.jsonl"
if($LASTEXITCODE){throw 'Java execution failed'}
& $dotnet run --file tools/UnitFormatReview.cs -- expect $Count
if($LASTEXITCODE){throw 'Java retention failed'}
& $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> "../unitformat100-build-$Count.log"
if($LASTEXITCODE){Get-Content "../unitformat100-build-$Count.log" -Tail 25;throw 'Build failed'}
& $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo --logger "trx;LogFileName=unitformat-$Count.trx" --results-directory ../unitformat100-test-results *> "../unitformat100-test-$Count.log"
if($LASTEXITCODE){Get-Content "../unitformat100-test-$Count.log" -Tail 25;throw 'Tests failed'}
[xml]$trx=Get-Content "../unitformat100-test-results/unitformat-$Count.trx"
$expected=1140+$Count
if(Test-Path tests/SharpUnits.Tests/UnitFormatRobustnessTests.cs){$expected+=12}
if([int]$trx.TestRun.ResultSummary.Counters.executed -ne $expected){throw "Incomplete tests: expected $expected"}
& $dotnet build src/SharpUnits.ConformanceRunner -c Release --no-restore --nologo *> "../unitformat100-runner-$Count.log"
if($LASTEXITCODE){throw 'Runner build failed'}
& $dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/unitformat-reference-cases.json > "../unitformat100-dotnet-$Count.jsonl"
if($LASTEXITCODE){throw 'CLR execution failed'}
& $dotnet run --file tools/UnitFormatReview.cs -- record $Count
if($LASTEXITCODE){throw 'Comparison failed'}
Get-Content "../unitformat100-test-$Count.log" -Tail 2
