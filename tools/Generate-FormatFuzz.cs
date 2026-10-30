#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
// Reproducible raw binary64 differential inputs; Java and CLR consume identical JSON doubles.
using System.Text.Json;
var random=new Random(385415);
var cases=new List<object>();
for(var i=0;i<512;i++)
{
    var bytes=new byte[8];random.NextBytes(bytes);
    var value=BitConverter.ToDouble(bytes);
    if(!double.IsFinite(value)){i--;continue;}
    cases.Add(new {id=$"FORMAT-FUZZ-{i:000}",operation="formatQuantity",value,from="A"});
}
File.WriteAllText(args[0],JsonSerializer.Serialize(new {cases},new JsonSerializerOptions{WriteIndented=true}));
