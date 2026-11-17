using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SharpUnits;

public sealed partial class ExactTextFormat
{
    private string WriteUnit(JavaUnit unit,int depth)
    {
        var visits=TextLimit;
        return WriteUnit(unit,depth,ref visits,new GraphMatcher());
    }
    private string WriteUnit(JavaUnit unit,int depth,ref int visits,GraphMatcher matcher)
    {
        if(depth>64)throw new TextReadFailure(new(TextErrorCode.DepthLimit,"unit"));
        if(--visits<0)throw new TextReadFailure(new(TextErrorCode.InputLimit,"unit"));
        if(unit.Kind==JavaUnitKind.Product&&unit.Factors.Count==0)return "unit()";
        var name=bindings.Where(pair=>matcher.Same(pair.Value,unit)).Select(pair=>pair.Key)
            .OrderBy(value=>value.Length).ThenBy(value=>value,StringComparer.Ordinal).FirstOrDefault();
        if(name is not null)return IsIdentifier(name)&&name is not ("unit" or "transform" or "binary")?name:"unit(\""+JsonEncodedText.Encode(name)+"\")";
        string text;
        if(unit.Kind==JavaUnitKind.Transformed)
        {
            var parent=WriteUnit(unit.ParentUnit!,depth+1,ref visits,matcher);
            var operations=unit.TransformConverter!.Operations;
            if(operations.Count==1&&operations[0].Kind is "Scale" or "Offset")
                text="("+parent+(operations[0].Kind=="Scale"?"*":"+")+NumberText(operations[0].Parameter)+")";
            else
            {
                var builder=new StringBuilder("transform(").Append(parent);
                foreach(var step in operations)
                {
                    var operation=step.Kind switch
                    {
                        "Scale"=>"scale("+NumberText(step.Parameter)+")",
                        "Offset"=>"offset("+NumberText(step.Parameter)+")",
                        "Power"=>"prefix("+step.Radix.ToString(CultureInfo.InvariantCulture)+","+step.Exponent.ToString(CultureInfo.InvariantCulture)+")",
                        "Log"=>"log("+NumberText(step.Parameter)+")",
                        "Exp"=>"exp("+NumberText(step.Parameter)+")",
                        _=>throw new TextReadFailure(new(TextErrorCode.UnsupportedGraph,"unit"))
                    };
                    if(builder.Length+operation.Length+2>TextLimit)throw new TextReadFailure(new(TextErrorCode.InputLimit,"unit"));
                    builder.Append(';').Append(operation);
                }
                text=builder.Append(')').ToString();
            }
        }
        else if(unit.Kind==JavaUnitKind.Product)
        {
            var pieces=new List<string>();var length=2;
            foreach(var pair in unit.Factors)
            {
                var part=WriteUnit(pair.Key,depth+1,ref visits,matcher);
                if(pair.Value!=new Rational(1,1))part=part+"^("+pair.Value.Numerator.ToString(CultureInfo.InvariantCulture)+"/"+pair.Value.Denominator.ToString(CultureInfo.InvariantCulture)+")";
                length+=part.Length+1;
                if(length>TextLimit)throw new TextReadFailure(new(TextErrorCode.InputLimit,"unit"));
                pieces.Add(part);
            }
            text="("+string.Join('*',pieces.Order(StringComparer.Ordinal))+")";
        }
        else throw new TextReadFailure(new(TextErrorCode.UnknownUnit,"unit"));
        if(text.Length>TextLimit)throw new TextReadFailure(new(TextErrorCode.InputLimit,"unit"));
        return text;
    }

    private static bool IsIdentifier(string value)=>value.Length>0&&(char.IsLetter(value[0])||value[0]=='_')
        &&value.All(c=>char.IsLetterOrDigit(c)||c=='_');
}
