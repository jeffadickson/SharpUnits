#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Text.Json;
using System.Text.Json.Nodes;

// Additional integration/edge inputs; expected values are retained from Java by the
// PowerShell verification workflow, never inferred from the CLR implementation.
var cases=new JsonArray();
JsonObject A(string op,string? name=null,string unit="A")=>new(){["op"]=op,["name"]=name,["unit"]=unit};
void Add(string gap,string kind="format",string unit="A",string? text=null,bool ascii=false,params JsonObject[] actions)
{
    cases.Add(new JsonObject{["id"]=$"UF-EDGE-{cases.Count+1:000}",["operation"]="unitFormat",["gap"]=gap,["kind"]=kind,
        ["unit"]=unit,["text"]=text,["ascii"]=ascii,["actions"]=new JsonArray(actions.Select(a=>a.DeepClone()).ToArray())});
}
Add("System text lookup follows shared labels","globalLookup");
Add("Default speed follows component labels",unit:"METRE_PER_SECOND",actions:[A("label","met","METRE")]);
Add("ASCII speed follows component labels",unit:"METRE_PER_SECOND",ascii:true,actions:[A("label","met","METRE")]);
Add("Default area without label follows component labels",unit:"SQUARE_METRE",actions:[A("removeLabel",unit:"SQUARE_METRE"),A("label","met","METRE")]);
Add("Default volume without label follows component labels",unit:"CUBIC_METRE",actions:[A("removeLabel",unit:"CUBIC_METRE"),A("label","met","METRE")]);
Add("Default area macro masks ASCII component override",unit:"SQUARE_METRE",ascii:true,actions:[A("label","met","METRE")]);
Add("ASCII area after default macro removal","defaultAreaRemoval");
Add("ASCII square alias","parse",unit:"SQUARE_METRE",text:"m2",ascii:true);
Add("ASCII Celsius alias","parse",unit:"CELSIUS",text:"Cel",ascii:true);
Add("Default micro Ohm alias","parseFormat",text:"µOhm");
Add("ASCII micro Ohm alias","parseFormat",text:"microOhm",ascii:true);
Add("Default month alias","parseFormat",text:"month");
Add("Default day alias","parseFormat",text:"day");
Add("ASCII micro ampere alias","parseFormat",text:"microA",ascii:true);
Add("ASCII micro gram alias","parseFormat",text:"microg",ascii:true);
Add("ASCII micro litre alias","parseFormat",text:"microL",ascii:true);
Add("ASCII kilogram alias","parseFormat",text:"kg",ascii:true);
Add("Removing null alias is a no-op",actions:[A("removeAlias")]);
Add("NBSP may start a default label",actions:[A("label","\u00a0label")]);
Add("NEL may start a default label",actions:[A("label","\u0085label")]);
Add("NUL may start a default label",actions:[A("label","\u0000label")]);
Add("Aliases are case sensitive","parse",text:"foo",actions:[A("alias","Foo")]);
Add("Removing current parse name retains no binding","parse",text:"amps",actions:[A("label","amps"),A("removeAlias","amps")]);
Add("Removing aliases preserves another unit's collision binding","parse",unit:"V",text:"shared",actions:[A("label","shared"),A("label","shared","V"),A("removeAliases")]);
Add("ASCII cubic alias","parse",unit:"CUBIC_METRE",text:"m3",ascii:true);
Add("Component aliases change product dimensions","parseDimension",text:"m/s",actions:[A("alias","m")]);
Add("Default identifier classification across all UTF-16 code units","identifierBmp");
Add("ASCII identifier classification across all UTF-16 code units","identifierBmp",ascii:true);
foreach(var prefix in new[]{"Ki","Mi","Gi","Ti","Pi","Ei","Zi","Yi"})
{
    Add($"ASCII binary ohm {prefix}","parseFormat",text:prefix+"Ohm",ascii:true);
    Add($"Default binary ohm alias {prefix}","parseFormat",text:prefix+"Ohm");
}
Add("ASCII volume after default macro removal","defaultVolumeRemoval");
Add("ASCII preserves unrelated foreign base symbol",unit:"__FOREIGN__",ascii:true);
if(cases.Count!=46)throw new Exception("Unexpected supplement size.");
File.WriteAllText("conformance/unitformat-supplement-cases.json",new JsonObject{["cases"]=cases}.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));
