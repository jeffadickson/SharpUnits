#:project ../src/SharpUnits/SharpUnits.csproj
using System.Numerics;
using SharpUnits;

// Original precision must enter through JavaNumber, before any binary64 projection.
var ampere=JavaUnit.FromCatalog("AMPERE");
var large=JavaNumber.Integer(BigInteger.Parse("9007199254740993"));
var exact=new JavaNumberQuantity(large,ampere);
Console.WriteLine(exact.Add(new JavaNumberQuantity(JavaNumber.One,ampere)).Value); // 9007199254740994

// A measured Indriya data-loss bug requires an explicit opt-in name.
var huge=JavaNumber.Integer(BigInteger.One<<64);var tenth=JavaNumber.Decimal("0.1");
Console.WriteLine(huge.Add(tenth)); // 184467440737095516161/10: preserves integer magnitude
Console.WriteLine(huge.AddPinnedIndriya(tenth)); // 1/10: reproduces the pinned Java bug

var thirdAmpere=ampere.Divide(JavaNumber.Integer(3));
var q=new JavaNumberQuantity(JavaNumber.Integer(2),thirdAmpere);
Console.WriteLine(q.To(ampere).Value); // 2/3

var shift=JavaUnitConverterAdapter.Add(JavaNumber.Integer(3));
var scale=JavaUnitConverterAdapter.Multiply(JavaNumber.Integer(2));
Console.WriteLine(shift.Concatenate(scale).Convert(JavaNumber.Integer(5))); // 13: scale first
Console.WriteLine(shift.Then(scale).Convert(JavaNumber.Integer(5))); // 16: shift first

var absolute=new JavaNumberQuantity(JavaNumber.Integer(20),JavaUnit.FromCatalog("CELSIUS"));
var interval=new JavaNumberQuantity(JavaNumber.Integer(10),JavaUnit.FromCatalog("CELSIUS"),JavaQuantityScale.Relative);
Console.WriteLine(absolute.Add(interval).Value); // 30: a relative temperature increase

// Native temperature safety remains independent of Java's arithmetic conventions.
var native=AbsoluteTemperature.FromCelsius(20)+new TemperatureDifference(10);
Console.WriteLine(native.Kelvin); // 303.15
