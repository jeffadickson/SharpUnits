#:property PublishAot=false
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Numerics;

// Reusable .NET 10 corpus generation/retention and source-level TCK audit.
if(!Directory.Exists("conformance")||!Directory.Exists("requirements")||!Directory.Exists("upstream/unit-tck/src/main/java"))
    throw new DirectoryNotFoundException("Run from the existing SharpUnits checkout.");
var options=new JsonSerializerOptions{WriteIndented=true};
if(args.Length!=1)throw new ArgumentException("generate, expect or audit required");
if(args[0]=="expect")
{
    var root=JsonNode.Parse(File.ReadAllText("conformance/work-package-one-cases.json"))!;
    var rows=File.ReadLines("../wp1-java.jsonl").Select(line=>JsonNode.Parse(line)!).ToDictionary(row=>row["id"]!.GetValue<string>());
    if(rows.Count!=root["cases"]!.AsArray().Count)throw new Exception("Incomplete Java reference observations");
    foreach(var item in root["cases"]!.AsArray())item!["expected"]=rows[item["id"]!.GetValue<string>()]["result"]!.DeepClone();
    File.WriteAllText("conformance/work-package-one-cases.json",root.ToJsonString(options)+"\n");return;
}
if(args[0]=="audit")
{
    var names=new[]{"UnitDimensionTest","UnitConversionTest","PrefixInterfaceTest","UnitInterfaceTest","QuantityInterfaceTest"};
    var ledger=JsonNode.Parse(File.ReadAllText("conformance/tck-closure-ledger.json"))!["entries"]!.AsArray();
    var records=new JsonArray();
    foreach(var name in names)
    {
        var path=Directory.EnumerateFiles("upstream/unit-tck/src/main/java",name+".java",SearchOption.AllDirectories).Single();
        var source=File.ReadAllText(path);var hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var assertions=Regex.Matches(source,"@SpecAssertion\\(section = [^,]+, id = \"([^\"]+)\"\\)[\\s\\S]*?public void ([^(]+)\\([^)]*\\)\\s*\\{");
        foreach(Match match in assertions)
        {
            var id=match.Groups[1].Value;var entry=ledger.Single(node=>node!["id"]!.GetValue<string>()==id)!;
            var start=match.Index+match.Length;var end=start;var braces=1;
            while(end<source.Length&&braces>0){if(source[end]=='{')braces++;if(source[end]=='}')braces--;end++;}
            var body=Regex.Replace(source[start..end],"(?m)^\\s*//.*$","");
            var calls=Regex.Matches(body,"(?:TestUtils\\.)?(?:testHasPublicMethod|testComparable)\\([^;]+;").Select(m=>Regex.Replace(m.Value,"\\s+"," ").Trim()).ToArray();
            records.Add(new JsonObject{{"id",id},{"suite",name},{"source",path.Replace('\\','/')},{"sourceSha256",hash},
                {"sourceMethod",name+"."+match.Groups[2].Value},{"upstreamChecks",JsonSerializer.SerializeToNode(calls)},
                {"assertionNature",calls.Length>0?"STRUCTURAL_API_CHECK":"CONFIGURATION_AVAILABILITY"},
                {"dotnetTest",entry["test"]?.DeepClone()},{"exactCoreTest",entry["exactCoreTest"]?.DeepClone()},{"exactCoreBehaviorTests",entry["exactCoreBehaviorTests"]?.DeepClone()},{"exactCoreCases",entry["exactCoreCases"]?.DeepClone()},{"formalStatus",entry["status"]!.DeepClone()}});
        }
    }
    if(records.Count!=66)throw new Exception($"Expected 66 source occurrences, found {records.Count}");
    File.WriteAllText("conformance/work-package-one-assertion-audit.json",new JsonObject{{"upstreamCommit","96da910db2a9aca61580a14125dde94b5f1c0285"},{"occurrences",records}}.ToJsonString(options)+"\n");return;
}
if(args[0]!="generate")throw new ArgumentException("Unknown command");
var cases=new List<Dictionary<string,object?>>();
object N(string kind,string value)=>new{kind,value};
object I(string value)=>N("integer",value);object D(string value)=>N("decimal",value);object B(string value)=>N("double",value);
object R(string n,string d)=>new{kind="rational",numerator=n,denominator=d};
object C(string kind,object? value=null,object? left=null,object? right=null)=>new{kind,value,left,right};
object U(string kind,object left,object? right=null,object? value=null,int degree=0,string? symbol=null,object? converter=null)=>new{kind,left,right,value,degree,symbol,converter};
object Q(object value,object unit,bool relative=false)=>new{value,unit,relative};
void Add(string domain,string action,object left,object? right=null,object? value=null,params (string Key,object? Value)[] extras)
{
    var item=new Dictionary<string,object?>{{"id",$"CORE-{cases.Count+1:D4}"},{"operation","core"},{"domain",domain},{"action",action},{"left",left}};
    if(right is not null)item["right"]=right;if(value is not null)item["value"]=value;
    foreach(var (key,v) in extras)item[key]=v;cases.Add(item);
}
var numbers=new[]{I("0"),I("1"),I("9007199254740993"),I("1234567890123456789012345678901234567890"),D("0.1"),D("0.20"),D("1.234567890123456789012345678901234567890"),R("1","3"),B("0.1"),B("0.2")};
foreach(var a in numbers){Add("number","identity",a);foreach(var b in numbers)foreach(var op in new[]{"add","subtract","multiply","divide","equal","compare"})Add("number",op,a,b);}
var random=new Random(385701);
for(var index=0;index<40;index++)
{
    var a=D($"{random.Next(-99999,99999)}.{random.Next(0,999999):D6}");var b=R(random.Next(-99999,99999).ToString(System.Globalization.CultureInfo.InvariantCulture),random.Next(1,99999).ToString(System.Globalization.CultureInfo.InvariantCulture));
    foreach(var action in new[]{"add","subtract","multiply","divide"})Add("number",action,a,b);
}
var scale2=C("scale",I("2"));var scale5=C("scale",I("5"));var add3=C("offset",I("3"));var add7=C("offset",I("7"));
var affine=C("concat",left:add3,right:scale2);var affine2=C("concat",left:add7,right:scale5);
foreach(var converter in new[]{C("identity"),scale2,add3,affine,C("concat",left:affine,right:affine2),C("inverse",affine),
    C("concat",left:scale2,right:scale5),C("concat",left:add3,right:add7),C("concat",left:affine,right:C("inverse",affine)),
    C("scale",R("5","9")),C("offset",D("0.1")),C("scale",I("0"))})
{
    foreach(var value in new[]{I("0"),I("5"),D("1.23456789012345678901234567890123456789"),I("9007199254740993"),R("1","3"),B("0.2")})Add("converter","convert",converter,value:value);
    foreach(var action in new[]{"identity","linear","steps"})Add("converter",action,converter);
    Add("converter","equal",converter,converter);Add("converter","hashEqual",converter,converter);
}
foreach(var b in new[]{2,10})
{
    object log=new{kind="log",@base=b};
    Add("converter","convert",log,value:I(b==2?"8":"1000"));Add("converter","convert",C("inverse",log),value:I("3"));
    Add("converter","identity",C("concat",left:log,right:C("inverse",log)));Add("converter","linear",log);
}
var catalog=JsonNode.Parse(File.ReadAllText("conformance/service-reference-cases.json"))!["cases"]!.AsArray();
// Field inventory is read from checked-in generated source, not from implementation output.
var fields=Regex.Matches(File.ReadAllText("src/SharpUnits/JavaUnitCatalogData.cs"),"new\\(\"([^\"]+)\",\"[^\"]+\",").Select(m=>m.Groups[1].Value).ToArray();
if(fields.Length!=44)throw new Exception("Catalog inventory changed");
foreach(var field in fields){Add("unit","metadata",field);Add("unit","basecount",field);Add("unit","system",field,value:I("2"));}
var ma=U("scale","AMPERE",value:D("0.001"));var mf=U("shift",U("scalarDivide",U("scale","KELVIN",value:I("5")),value:I("9")),value:D("459.67"));
var sampleUnits=new object[]{"AMPERE","VOLT","OHM","CELSIUS","SQUARE_METRE","METRE_PER_SECOND",ma,mf,
    U("multiply","AMPERE","OHM"),U("multiply","OHM","AMPERE"),U("divide",U("multiply","AMPERE","VOLT"),"AMPERE"),
    U("root",U("pow","METRE",degree:2),degree:2),U("root","METRE",degree:2),U("root","METRE",degree:-2),
    U("pow","CELSIUS",degree:-1),U("alternate",ma,symbol:"customMilliA"),U("alternate","AMPERE",symbol:"customA"),
    U("shift",ma,value:I("3")),U("scale","CELSIUS",value:D("0.001")),U("transform",ma,converter:add3)};
foreach(var unit in sampleUnits)
{
    Add("unit","equal",unit,unit);Add("unit","hashEqual",unit,unit);Add("unit","system",unit,value:I("2"));Add("unit","basecount",unit);
    Add("unit","systemEqual",unit,"AMPERE");
}
Add("unit","equal",U("multiply","AMPERE","OHM"),U("multiply","OHM","AMPERE"));
Add("unit","equal",U("divide",U("multiply","AMPERE","VOLT"),"AMPERE"),"VOLT");
Add("unit","equal",U("pow","METRE",degree:0),"ONE");Add("unit","equal",U("root",U("pow","METRE",degree:2),degree:2),"METRE");
Add("unit","equal",U("scale",ma,value:I("1000")),"AMPERE"); // Parent graph identity is intentionally not dimensional equivalence.
Add("unit","equivalent",U("scale",ma,value:I("1000")),"AMPERE");
Add("unit","equal","HERTZ","BECQUEREL");Add("unit","equivalent","HERTZ","BECQUEREL");
Add("unit","convert",mf,"KELVIN",I("32"));Add("unit","convert",U("scale","CELSIUS",value:D("0.001")),"KELVIN",I("1000"));
Add("unit","convert","AMPERE","VOLT",I("1"));Add("unit","equal",U("root","METRE",degree:0),"METRE");
foreach(var unit in new object[]{"AMPERE",ma,"CELSIUS",mf})foreach(var relative in new[]{false,true})
{
    foreach(var value in new[]{I("2"),D("0.1"),R("1","3"),I("9007199254740993")})
    {
        var q=Q(value,unit,relative);Add("quantity","identity",q);Add("quantity","convert",q,extras:new[]{("to",(object?) (unit== (object)"AMPERE"||ReferenceEquals(unit,ma)?"AMPERE":"KELVIN"))});
        Add("quantity","scalarMultiply",q,extras:new[]{("scalar",(object?)D("0.2"))});Add("quantity","scalarDivide",q,extras:new[]{("scalar",(object?)I("3"))});
        Add("quantity","inverse",q);
    }
}
foreach(var leftRelative in new[]{false,true})foreach(var rightRelative in new[]{false,true})
{
    var a=Q(D("20.1"),"CELSIUS",leftRelative);var b=Q(D("10.2"),mf,rightRelative);
    foreach(var action in new[]{"add","subtract","multiply","divide","equal","equivalent","compare"})Add("quantity",action,a,b);
}
Add("quantity","multiply",Q(I("2"),"AMPERE"),Q(I("3"),"OHM"),extras:new[]{("resultUnit",(object?)U("multiply","OHM","AMPERE"))});
Add("quantity","divide",Q(I("2"),"AMPERE"),Q(I("3"),"AMPERE"),extras:new[]{("resultUnit",(object?)"ONE")});
Add("quantity","equal",Q(D("0.10"),"AMPERE"),Q(D("0.1"),"AMPERE"));Add("quantity","equivalent",Q(D("0.10"),"AMPERE"),Q(D("0.1"),"AMPERE"));
foreach(var field in fields)Add("dimension","base",field);
foreach(var field in new[]{"AMPERE","METRE","METRE_PER_SECOND","SQUARE_METRE","ONE"})
{
    Add("dimension","equal",field,field);Add("dimension","hashEqual",field,field);
    Add("dimension","pow",field,"ONE",extras:new[]{("degree",(object?)0)});
    Add("dimension","root",U("pow",field,degree:2),field,extras:new[]{("degree",(object?)2)});
    Add("dimension","root",field,U("pow",field,degree:-1),extras:new[]{("degree",(object?)(-1))});
}
Add("dimension","multiply","METRE","SQUARE_METRE",extras:new[]{("operand",(object?)"METRE")});
Add("dimension","divide","METRE","METRE_PER_SECOND",extras:new[]{("operand",(object?)"SECOND")});
Add("dimension","root","METRE","ONE",extras:new[]{("degree",(object?)0)});
Add("dimension","base",U("root","METRE",degree:2));
foreach(Match prefix in Regex.Matches(File.ReadAllText("src/SharpUnits/SiPrefix.cs"),"new\\(\"([^\"]+)\",\"[^\"]+\",-?[0-9]+\\)"))
    Add("prefix","metadata",prefix.Groups[1].Value.ToUpperInvariant());
var closeA=R("1","3");var closeB=R("10000000000000000000000000000000000000001","30000000000000000000000000000000000000000");
Add("number","equal",closeA,closeB);Add("number","compare",closeA,closeB);
Add("converter","equal",C("scale",closeA),C("scale",closeB));
foreach(var value in new[]{D("1234567890123456789012345678901234.4"),D("1234567890123456789012345678901235.4")})Add("number","add",value,D("0.1"));
foreach(var value in new[]{I("0"),I("32"),D("212.25"),R("1","3")})
{
    Add("unit","convert","FAHRENHEIT","KELVIN",value);
    Add("quantity","convert",Q(value,"FAHRENHEIT"),extras:new[]{("to",(object?)"KELVIN")});
}
Add("unit","metadata","FAHRENHEIT");
Add("unit","system",U("shiftDouble",U("scale","KELVIN",value:R("5","9")),value:B("459.67")),value:I("32"));
foreach(var basis in new[]{2d,10d,Math.E,1.25})foreach(var x in new[]{"0.1","0.3","1.23456789012345678901234567890123456789","-0.75","3.141592653589793238462643383279502884"})
{
    object log=new{kind="log",@base=basis};
    Add("converter","approximate",C("inverse",log),value:D(x),extras:new[]{("relativeTolerance",(object?)3e-15),("absoluteTolerance",(object?)1e-15)});
    Add("converter","approximate",log,value:D(x.StartsWith('-')?"0.75":x),extras:new[]{("relativeTolerance",(object?)3e-15),("absoluteTolerance",(object?)1e-15)});
}
var recipes=new Dictionary<string,object>{
 ["GRAM"]=U("scalarDivide","KILOGRAM",value:I("1000")),["RADIAN"]=U("alternate","ONE",symbol:"rad"),
 ["STERADIAN"]=U("alternate","ONE",symbol:"sr"),["HERTZ"]=U("alternate",U("divide","ONE","SECOND"),symbol:"Hz"),
 ["NEWTON"]=U("alternate",U("divide",U("multiply","METRE","KILOGRAM"),U("pow","SECOND",degree:2)),symbol:"N"),
 ["PASCAL"]=U("alternate",U("divide","NEWTON",U("pow","METRE",degree:2)),symbol:"Pa"),
 ["JOULE"]=U("alternate",U("multiply","NEWTON","METRE"),symbol:"J"),["WATT"]=U("alternate",U("divide","JOULE","SECOND"),symbol:"W"),
 ["COULOMB"]=U("alternate",U("multiply","SECOND","AMPERE"),symbol:"C"),["VOLT"]=U("alternate",U("divide","WATT","AMPERE"),symbol:"V"),
 ["FARAD"]=U("alternate",U("divide","COULOMB","VOLT"),symbol:"F"),["OHM"]=U("alternate",U("divide","VOLT","AMPERE"),symbol:"Ω"),
 ["SIEMENS"]=U("alternate",U("divide","AMPERE","VOLT"),symbol:"S"),["WEBER"]=U("alternate",U("multiply","VOLT","SECOND"),symbol:"Wb"),
 ["TESLA"]=U("alternate",U("divide","WEBER",U("pow","METRE",degree:2)),symbol:"T"),["HENRY"]=U("alternate",U("divide","WEBER","AMPERE"),symbol:"H"),
 ["CELSIUS"]=U("shift","KELVIN",value:B("273.15")),["LUMEN"]=U("alternate",U("multiply","CANDELA","STERADIAN"),symbol:"lm"),
 ["LUX"]=U("alternate",U("divide","LUMEN",U("pow","METRE",degree:2)),symbol:"lx"),["BECQUEREL"]=U("alternate",U("divide","ONE","SECOND"),symbol:"Bq"),
 ["GRAY"]=U("alternate",U("divide","JOULE","KILOGRAM"),symbol:"Gy"),["SIEVERT"]=U("alternate",U("divide","JOULE","KILOGRAM"),symbol:"Sv"),
 ["KATAL"]=U("alternate",U("divide","MOLE","SECOND"),symbol:"kat"),["METRE_PER_SECOND"]=U("divide","METRE","SECOND"),
 ["METRE_PER_SQUARE_SECOND"]=U("divide","METRE_PER_SECOND","SECOND"),["SQUARE_METRE"]=U("pow","METRE",degree:2),["CUBIC_METRE"]=U("pow","METRE",degree:3),
 ["KILOMETRE_PER_HOUR"]=U("scale","METRE_PER_SECOND",value:R("5","18")),["PERCENT"]=U("scale","ONE",value:R("1","100")),
 ["MINUTE"]=U("scale","SECOND",value:I("60")),["HOUR"]=U("scale","SECOND",value:I("3600")),["DAY"]=U("scale","SECOND",value:I("86400")),
 ["WEEK"]=U("scale","DAY",value:I("7")),["YEAR"]=U("scale","DAY",value:R("146097","400")),["MONTH"]=U("scalarDivide","YEAR",value:I("12")),
 ["LITRE"]=U("scalarDivide","CUBIC_METRE",value:I("1000"))
};
foreach(var pair in recipes)Add("unit","equal",pair.Key,pair.Value);
foreach(var value in new[]{"1E30","-1E30","9.223372036854776E18","-9.223372036854776E18","0.0000000000000001"})Add("number","identity",B(value));
foreach(var value in new[]{"1.25E3","1.25E-3","1e+4","-0.000","+.25"})Add("number","identity",D(value));
foreach(var action in new[]{"multiply","divide"})
{
 Add("quantity",action,Q(I("2"),ma),Q(I("3"),"OHM"));
 Add("quantity",action,Q(I("2"),ma),Q(I("3"),"OHM"),extras:new[]{("resultUnit",(object?)(action=="multiply"?U("multiply",ma,"OHM"):U("divide",ma,"OHM")))});
}
foreach(var action in new[]{"add","subtract"})foreach(var relative in new[]{false,true})Add("quantity",action,Q(I("20"),"CELSIUS",relative),Q(I("10"),"KELVIN"));
Add("quantity","equal",Q(I("20"),"CELSIUS"),Q(I("20"),"CELSIUS",true));
object log2=new{kind="log",@base=2};object log10=new{kind="log",@base=10};
Add("converter","identity",C("concat",left:log2,right:C("inverse",log10)));
Add("converter","identity",C("concat",left:C("inverse",log2),right:log10));
Add("converter","identity",C("concat",left:log2,right:log10));
Add("number","compare",R("1","3"),D("0.3333333333333333333333333333333333"));
Add("number","compare",D("0.3333333333333333333333333333333333"),R("1","3"));
var denominator=BigInteger.Pow(2,53)*BigInteger.Pow(10,50);
var aboveHalf=(BigInteger.Pow(2,53)+1)*BigInteger.Pow(10,50)+BigInteger.Pow(2,53);
Add("number","double",R(aboveHalf.ToString(),denominator.ToString()));
foreach(var magnitude in new[]{1e40,1e50,1e100})
{
    var midpoint=(new BigInteger(magnitude)+new BigInteger(Math.BitIncrement(magnitude)))/2;
    Add("number","double",I((midpoint+1).ToString()));Add("number","double",I((midpoint-1).ToString()));
}
foreach(var value in new[]{"9.223372036854776E18","-9.223372036854776E18","1.25"})
{
    Add("unit","system",U("scaleDouble","AMPERE",value:B(value)),value:I("1"));
    Add("unit","system",U("scalarDivideDouble","AMPERE",value:B(value)),value:I("1"));
    Add("unit","system",U("shiftDouble","KELVIN",value:B(value)),value:I("0"));
    object native=new{kind="nativeAffine",scale=double.Parse(value,System.Globalization.CultureInfo.InvariantCulture),offset=0d};
    Add("converter","convert",native,value:I("1"));
}
Add("unit","equal",U("shift","AMPERE",value:I("1")),U("shift","AMPERE",value:D("1.0")));
foreach(Match prefix in Regex.Matches(File.ReadAllText("src/SharpUnits/SiPrefix.cs"),"new\\(\"([^\"]+)\",\"[^\"]+\",(-?[0-9]+)\\)"))
{
    var name=prefix.Groups[1].Value.ToUpperInvariant();var exponent=int.Parse(prefix.Groups[2].Value);
    var power=BigInteger.Pow(10,Math.Abs(exponent));object factor=exponent>=0?I(power.ToString()):R("1",power.ToString());
    var prefixed=U("prefix","AMPERE",symbol:name);var scaled=U("scale","AMPERE",value:factor);
    Add("unit","equal",prefixed,scaled);Add("unit","equivalent",prefixed,scaled);Add("unit","system",prefixed,value:I("2"));
    Add("unit","system",U("prefix","CELSIUS",symbol:name),value:I("2"));
}
var binaryNames=new[]{"KIBI","MEBI","GIBI","TEBI","PEBI","EXBI","ZEBI","YOBI"};
for(var index=0;index<binaryNames.Length;index++)
{
    var name=binaryNames[index];Add("binaryPrefix","metadata",name);
    var prefixed=U("binaryPrefix","OHM",symbol:name);var scaled=U("scale","OHM",value:I(BigInteger.Pow(1024,index+1).ToString()));
    Add("unit","equal",prefixed,scaled);Add("unit","equivalent",prefixed,scaled);Add("unit","system",prefixed,value:I("2"));
}
object P(int radix,int exponent)=>new{kind="prefix",radix,exponent};
foreach(var converter in new[]{P(10,3),P(10,-3),P(10,1),P(1024,1),P(1024,2),C("inverse",P(10,-1)),C("concat",left:P(10,3),right:P(10,-2))})
{
    Add("converter","convert",converter,value:I("2"));Add("converter","steps",converter);
    Add("converter","identity",C("concat",left:converter,right:C("inverse",converter)));
}
Add("converter","equal",P(10,3),C("scale",I("1000")));
Add("converter","equal",P(10,1),C("scale",I("10")));
Add("converter","equal",C("concat",left:P(10,3),right:P(10,-2)),C("scale",I("10")));
Add("converter","equal",P(10,2),P(100,1));
Add("converter","identity",C("concat",left:P(10,3),right:C("scale",R("1","1000"))));
Add("converter","identity",C("concat",left:C("scale",R("1","1000")),right:P(10,3)));
Add("converter","identity",C("concat",left:P(2,10),right:P(1024,-1)));
var rootObject=new{seed=385701,cases};
File.WriteAllText("conformance/work-package-one-cases.json",JsonSerializer.Serialize(rootObject,options)+"\n");
Console.WriteLine($"Generated {cases.Count} core cases.");
