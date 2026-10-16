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
  if(operation!="convert"&&operation!="multiply"&&operation!="divide"&&operation!="power")throw new NotSupportedException("Unsupported operation");
  var value=test.GetProperty("value").GetDouble();
  var source=test.GetProperty("from").GetString()!;
  var target=test.GetProperty("to").GetString()!;
  double result;
  if(operation=="convert")result=registry.Convert(value,source,target);
  else
  {
   var left=RationalUnitExpression.From(registry.Resolve(source));
   var expression=operation=="power"
     ?left.Pow(test.GetProperty("exponent").GetInt32())
     :operation=="multiply"
       ?left*RationalUnitExpression.From(registry.Resolve(target))
       :left/RationalUnitExpression.From(registry.Resolve(target));
   var output=RationalUnitExpression.From(registry.Resolve(test.GetProperty("resultUnit").GetString()!));
   if(!expression.IsCompatibleWith(output))throw new InvalidOperationException("Incompatible dimensions");
   result=value*expression.Scale/output.Scale;
  }
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
