#:project ../src/SharpUnits/SharpUnits.csproj
#:property PublishAot=false
using SharpUnits;

// Handle user input at the application boundary. A failed parse does not invoke
// the continuation; the UI can translate the stable code to a field message.
foreach (var input in new[] { "1.25", "1.2.3", "1e4097" })
{
    var result = JavaNumber.ParseDecimal(input)
        .Bind(number => NumericResult<Rational>.Success(number.Fraction));
    Console.WriteLine(result.Match(
        fraction => $"{input}: {fraction}",
        error => $"{input}: correct {error.Parameter}, code={error.Code}, offset={error.Position}"));
}

// Numeric-object conversion preserves boxed Long promotion. Unsupported objects
// get an explicit failure without inspecting their contents or calling ToString.
Console.WriteLine(JavaNumber.ConvertNumeric(new object()).Match(
    number => number.ToString(),
    error => $"Choose a numeric value: {error.Code}"));
