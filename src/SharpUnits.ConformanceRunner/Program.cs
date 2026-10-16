using System.Text.Json;
using SharpUnits;

// Evaluates the shared cross-language conformance cases and emits JSONL.
// Output is machine-readable; diagnostics belong on stderr.
if(args.Length!=1)
{
 Console.Error.WriteLine("Usage: SharpUnits.ConformanceRunner <cases.json>");
 return 2;
}
using var document=JsonDocument.Parse(File.ReadAllText(args[0]));
var registry=UnitRegistry.CreateStandard();
foreach(var test in document.RootElement.GetProperty("cases").EnumerateArray())
{
 var id=test.GetProperty("id").GetString()!;
 try
 {
  var operation=test.GetProperty("operation").GetString();
  if(operation!="convert")throw new NotSupportedException("Unsupported operation");
  var value=test.GetProperty("value").GetDouble();
  var source=test.GetProperty("from").GetString()!;
  var target=test.GetProperty("to").GetString()!;
  var result=registry.Convert(value,source,target);
  Console.WriteLine(JsonSerializer.Serialize(new {id,result}));
 }
 catch(InvalidOperationException)
 {
  Console.WriteLine(JsonSerializer.Serialize(new {id,error="INCOMPATIBLE_DIMENSION"}));
 }
 catch(Exception ex)
 {
  Console.Error.WriteLine($"{id}: {ex.GetType().Name}: {ex.Message}");
  return 1;
 }
}
return 0;
