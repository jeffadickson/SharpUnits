#:project ../src/SharpUnits/SharpUnits.csproj
#:property PublishAot=false
using SharpUnits;

var format=new ExactTextFormat();
// A quantity larger than double's exact integer range stays exact and relative.
var text=format.ParseQuantity("9007199254740993 A @relative")
    .Bind(quantity=>TextResult<string>.Success(format.FormatQuantity(quantity)))
    .Match(value=>value,error=>$"{error.Code} at {error.Position}");
Console.WriteLine(text);
// These shifted-unit expressions have different application order.
foreach(var expression in new[]{"(K*2)+3","(K+3)*2","m*0.123456789012345678901234567890123456789"})
{
    Console.WriteLine(format.ParseUnit(expression).Match(unit=>
    {
        var c=unit.SystemConverter.AffineCoefficients();
        return $"{expression}: scale={c.Scale.Fraction}, offset={c.Offset.Fraction}; text={format.FormatUnit(unit)}";
    },error=>$"{error.Code} at {error.Position}"));
}
Console.WriteLine(format.ParseUnit("m/0").Match(_=>"unexpected success",error=>$"Correct {error.Parameter}: {error.Code} at {error.Position}"));
