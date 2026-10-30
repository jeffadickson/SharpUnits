#:property PublishAot=false
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
// Independent exact fractions and explicit decimal128 comparison witnesses.
if(!Directory.Exists("conformance"))throw new DirectoryNotFoundException("Existing checkout required");
var cases=new JsonArray();
JsonObject Number(string kind,string value)=>new(){{"kind",kind},{"value",value}};
var denominator=BigInteger.Pow(10,50);
foreach(var direction in new[]{-1,1})foreach(var reverse in new[]{false,true})
{
 var rational=new JsonObject{{"kind","rational"},{"numerator",(denominator/2+direction).ToString()},{"denominator",denominator.ToString()}};
 var binary=Number("double","0.5");
 cases.Add(new JsonObject{{"id",$"DEBT-COMPARE-{direction}-{reverse}"},{"operation","core"},{"domain","number"},{"action","compare"},{"left",reverse?binary:rational},{"right",reverse?rational:binary},{"independentExpected",0},{"absoluteTolerance",0}});
}
var coefficient=BigInteger.Parse("1234567890123456789012345678901234");
foreach(var value in new[]{(BigInteger.One<<62)-1,BigInteger.One<<62,new BigInteger(long.MaxValue),-(BigInteger.One<<62),-(BigInteger.One<<62)-1,new BigInteger(long.MinValue)})
foreach(var origin in new[]{"integer","double","long"})foreach(var reverse in new[]{false,true})
{
 // Integral double inputs first narrow to a Java Long; exact BigInteger inputs use
 // signed bit length (<63), including its asymmetric negative boundary.
 var primitive=origin!="integer"; if(origin=="double"&&value!=BigInteger.One<<62&&value!=-(BigInteger.One<<62)&&value!=long.MaxValue&&value!=long.MinValue)continue;
 var product=coefficient*value;var scale=34;
 var context=!primitive&&(value>=BigInteger.One<<62||value<-(BigInteger.One<<62));
 if(context)
 {
  var magnitude=BigInteger.Abs(product);var removed=magnitude.ToString().Length-34;var divisor=BigInteger.Pow(10,removed);
  var quotient=BigInteger.DivRem(magnitude,divisor,out var remainder);
  if(remainder*2>divisor||remainder*2==divisor&&!quotient.IsEven)quotient++;
  product=product.Sign*quotient;scale-=removed;
 }
 var denominatorDecimal=BigInteger.Pow(10,scale);var gcd=BigInteger.GreatestCommonDivisor(product,denominatorDecimal);
 var integer=Number(origin,value.ToString());var fraction=Number("decimal","0.1234567890123456789012345678901234");
 cases.Add(new JsonObject{{"id",$"DEBT-INTEGER-MULTIPLY-{value}-{origin}-{reverse}"},{"operation","core"},{"domain","number"},{"action","multiply"},{"left",reverse?fraction:integer},{"right",reverse?integer:fraction},{"independentExpected",$"Decimal|{product/gcd}/{denominatorDecimal/gcd}|{scale}"},{"absoluteTolerance",0}});
}
cases.Add(new JsonObject{{"id","DEBT-HIGH-SCALE-EXACT-TENTH"},{"operation","core"},{"domain","number"},{"action","add"},{"left",Number("decimal","0.04"+new string('0',33))},{"right",Number("decimal","0.06"+new string('0',33))},{"independentExpected","Decimal|1/10|34"},{"absoluteTolerance",0}});
cases.Add(new JsonObject{{"id","DEBT-INCOMPATIBLE-UNIT"},{"operation","core"},{"domain","unit"},{"action","convert"},{"left","AMPERE"},{"right","SECOND"},{"value",Number("integer","1")},{"independentExpected",new JsonObject{{"error","INCOMPATIBLE"},{"stage","operation"}}},{"absoluteTolerance",0}});
cases.Add(new JsonObject{{"id","DEBT-NEGATIVE-DECADE-SCALE"},{"operation","core"},{"domain","number"},{"action","multiply"},{"left",Number("decimal","0.1")},{"right",Number("decimal","0."+new string('0',38)+"1"+new string('0',61))},{"independentExpected",$"Decimal|1/{BigInteger.Pow(10,40)}|73"},{"absoluteTolerance",0}});
JsonObject Root()=>new(){{"kind","root"},{"left","AMPERE"},{"degree",2}};
cases.Add(new JsonObject{{"id","DEBT-UNSUPPORTED-UNIT-SELF-CONVERTER"},{"operation","core"},{"domain","unit"},{"action","convertTyped"},{"left",Root()},{"right",Root()},{"value",Number("integer","1")},{"independentExpected","Integer|1/1|0"},{"absoluteTolerance",0}});
cases.Add(new JsonObject{{"id","DEBT-UNSUPPORTED-UNIT-ANY-SELF-CONVERTER"},{"operation","core"},{"domain","unit"},{"action","convert"},{"left",Root()},{"right",Root()},{"value",Number("integer","1")},{"independentExpected",new JsonObject{{"error","UNSUPPORTED"},{"stage","operation"}}},{"absoluteTolerance",0}});
cases.Add(new JsonObject{{"id","DEBT-INCOMPATIBLE-TYPED-UNIT"},{"operation","core"},{"domain","unit"},{"action","convertTyped"},{"left","AMPERE"},{"right","SECOND"},{"value",Number("integer","1")},{"independentExpected",new JsonObject{{"error","INCOMPATIBLE"},{"stage","operation"}}},{"absoluteTolerance",0}});
File.WriteAllText("conformance/mutation-debt-cases.json",new JsonObject{{"schemaVersion",2},{"cases",cases}}.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
