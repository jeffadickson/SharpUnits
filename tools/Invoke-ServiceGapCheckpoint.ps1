param([Parameter(Mandatory)][ValidateRange(10,100)][int]$Count)
$ErrorActionPreference='Stop'
if($Count%10){throw 'Compile/test checkpoints must be multiples of ten'}
$dotnet='C:/Users/jeffd/.dotnet/dotnet.exe'
$java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe'
& $dotnet run --file tools/ServiceGapReview.cs -- prepare $Count
if($LASTEXITCODE){throw 'Gap preparation failed'}
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
& $java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.ServiceReferenceRunner conformance/service-reference-cases.json > "../services100-java-$Count.jsonl"
if($LASTEXITCODE){throw 'Pinned Java observation failed'}
& $dotnet run --file tools/ServiceGapReview.cs -- expect $Count
if($LASTEXITCODE){throw 'Java observation retention failed'}
& $dotnet build tests/SharpUnits.Tests -c Release --no-restore --nologo *> "../services100-build-$Count.log"
if($LASTEXITCODE){Get-Content "../services100-build-$Count.log" -Tail 25;throw 'Build failed'}
& $dotnet test tests/SharpUnits.Tests -c Release --no-build --nologo *> "../services100-test-$Count.log"
if($LASTEXITCODE){Get-Content "../services100-test-$Count.log" -Tail 25;throw 'Full tests failed'}
& $dotnet build src/SharpUnits.ConformanceRunner -c Release --no-restore --nologo *> "../services100-runner-$Count.log"
if($LASTEXITCODE){throw 'Runner build failed'}
& $dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/service-reference-cases.json > "../services100-dotnet-$Count.jsonl"
if($LASTEXITCODE){throw 'CLR observation failed'}
& $dotnet run --file tools/ServiceGapReview.cs -- record $Count
if($LASTEXITCODE){throw 'Comparison failed'}
Get-Content "../services100-test-$Count.log" -Tail 2
