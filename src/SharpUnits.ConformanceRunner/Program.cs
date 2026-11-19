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
foreach(var symbol in new[]{"A","V","Ω"})
{
 var baseUnit=registry.Resolve(symbol);
 var squared=RationalUnitExpression.From(baseUnit).Pow(2);
 registry.Register(new UnitDefinition(symbol+"²",squared.Dimension,UnitConversion.Linear(squared.Scale)));
}
foreach(var test in document.RootElement.GetProperty("cases").EnumerateArray())
{
 var id=test.GetProperty("id").GetString()!;
 try
 {
  var operation=test.GetProperty("operation").GetString();
  if(operation=="core")
  {
      var observed=CoreFixtures.Evaluate(test);
   var row=new Dictionary<string,object?>{{"id",id},{"result",observed}};
   if(CoreFixtures.LastExceptionType is not null)row["clrException"]=CoreFixtures.LastExceptionType;
   Console.WriteLine(JsonSerializer.Serialize(row));
   continue;
  }
  if(operation=="unitFormat")
  {
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=UnitFormatFixtures.Evaluate(test)}));
   continue;
  }
  if(operation?.StartsWith("service",StringComparison.Ordinal)==true)
  {
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=ServiceFixtures.Evaluate(test)}));
   continue;
  }
  if(operation?.StartsWith("format",StringComparison.Ordinal)==true)
  {
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=FormatFixtures.Evaluate(test)}));
   continue;
  }
  if(operation?.StartsWith("parse",StringComparison.Ordinal)==true)
  {
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=ParserFixtures.Evaluate(test)}));
   continue;
  }
  if(operation?.StartsWith("factory",StringComparison.Ordinal)==true)
  {
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=FactoryFixtures.Evaluate(test,registry)}));
   continue;
  }
  if(operation?.StartsWith("quantity",StringComparison.Ordinal)==true)
  {
   var quantityResult=QuantityFixtures.Evaluate(test,registry);
   Console.WriteLine(JsonSerializer.Serialize(new {id,result=quantityResult}));
   continue;
  }
  if(operation!="convert"&&operation!="multiply"&&operation!="divide"&&operation!="power"&&operation!="root")throw new NotSupportedException("Unsupported operation");
  var value=test.GetProperty("value").GetDouble();
  var source=test.GetProperty("from").GetString()!;
  var target=test.GetProperty("to").GetString()!;
  double result;
  if(operation=="convert")result=registry.Convert(value,source,target);
  else
  {
   var left=RationalUnitExpression.From(registry.Resolve(source));
   var expression=operation=="root"
     ?left.Root(test.GetProperty("degree").GetInt32())
     :operation=="power"
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
 catch(Exception ex) when (QuantityFixtures.ErrorCategory(ex,test.GetProperty("operation").GetString()) is not null)
 {
  Console.WriteLine(JsonSerializer.Serialize(new {id,error=QuantityFixtures.ErrorCategory(ex,test.GetProperty("operation").GetString())}));
 }
 catch(Exception ex)
 {
  Console.Error.WriteLine($"{id}: {ex.GetType().Name}: {ex.Message}");
  return 1;
 }
}
return 0;
