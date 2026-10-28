using System.Globalization;
using System.Numerics;
using System.Text.Json;
using SharpUnits;

/// <summary>CLR observations for the independent pinned-Java core corpus.</summary>
internal static class CoreFixtures
{
    [ThreadStatic] private static IReadOnlyDictionary<string,JavaUnit>? catalog;
    internal static object Evaluate(JsonElement test)
    {
        var previous=catalog;
        try
        {
            // Build the unit catalog within the observed case. This also prevents
            // per-test mutation coverage from hiding lazily cached construction.
            if(S(test,"domain") is "unit" or "quantity" or "dimension")catalog=JavaUnit.CreateCatalog();
            return Observe(test);
        }
        catch(NotSupportedException){return "ERR:UNSUPPORTED";}
        catch(DivideByZeroException){return "ERR:ARITHMETIC";}
        catch(InvalidOperationException error) when(error.Message.Contains("Incompatible dimensions",StringComparison.Ordinal)){return "ERR:INCOMPATIBLE";}
        catch(Exception error) when(error is ArgumentException or FormatException or InvalidOperationException){return "ERR:ARGUMENT";}
        finally{catalog=previous;}
    }
    private static JsonElement P(JsonElement node,string key)=>node.GetProperty(key);
    private static string S(JsonElement node,string key)=>P(node,key).GetString()!;
    internal static JavaNumber Number(JsonElement node)=>S(node,"kind") switch
    {
        "integer"=>JavaNumber.Integer(BigInteger.Parse(S(node,"value"),CultureInfo.InvariantCulture)),
        "decimal"=>JavaNumber.Decimal(S(node,"value")),
        "double"=>JavaNumber.FromDouble(double.Parse(S(node,"value"),CultureInfo.InvariantCulture)),
        "rational"=>JavaNumber.Ratio(BigInteger.Parse(S(node,"numerator"),CultureInfo.InvariantCulture),BigInteger.Parse(S(node,"denominator"),CultureInfo.InvariantCulture)),
        _=>throw new ArgumentException("Unknown numeric fixture")
    };
    internal static string ObserveNumber(JavaNumber value)=>$"{value.Kind}|{value.Fraction.Numerator.ToString(CultureInfo.InvariantCulture)}/{value.Fraction.Denominator.ToString(CultureInfo.InvariantCulture)}|{value.DecimalScale}";
    private static JavaUnitConverterAdapter Converter(JsonElement node)=>S(node,"kind") switch
    {
        "identity"=>JavaUnitConverterAdapter.Identity,
        "nativeAffine"=>new JavaUnitConverterAdapter(new UnitConversion(P(node,"scale").GetDouble(),P(node,"offset").GetDouble())),
        "scale"=>JavaUnitConverterAdapter.Multiply(Number(P(node,"value"))),
        "prefix"=>JavaUnitConverterAdapter.ForPrefix(P(node,"radix").GetInt32(),P(node,"exponent").GetInt32()),
        "offset"=>JavaUnitConverterAdapter.Add(Number(P(node,"value"))),
        "log"=>JavaUnitConverterAdapter.Logarithm(P(node,"base").GetDouble()),
        "inverse"=>Converter(P(node,"value")).Inverse(),
        "concat"=>Converter(P(node,"left")).Concatenate(Converter(P(node,"right"))),
        _=>throw new ArgumentException("Unknown converter fixture")
    };
    private static JavaUnit Unit(JsonElement node)
    {
        if(node.ValueKind==JsonValueKind.String)return node.GetString()=="FAHRENHEIT"
            ?JavaUnit.FromDefinition(UnitRegistry.CreateStandard().Resolve("°F")):catalog![node.GetString()!];
        var left=Unit(P(node,"left"));
        return S(node,"kind") switch
        {
            "multiply"=>left.Multiply(Unit(P(node,"right"))),"divide"=>left.Divide(Unit(P(node,"right"))),
            "scale"=>left.Multiply(Number(P(node,"value"))),"scalarDivide"=>left.Divide(Number(P(node,"value"))),
            "scaleDouble"=>new JavaUnitAdapter(left).Multiply(double.Parse(S(P(node,"value"),"value"),CultureInfo.InvariantCulture)).Model,
            "scalarDivideDouble"=>new JavaUnitAdapter(left).Divide(double.Parse(S(P(node,"value"),"value"),CultureInfo.InvariantCulture)).Model,
            "shift"=>left.Shift(Number(P(node,"value"))),"alternate"=>left.Alternate(S(node,"symbol")),
            "shiftDouble"=>new JavaUnitAdapter(left).Shift(double.Parse(S(P(node,"value"),"value"),CultureInfo.InvariantCulture)).Model,
            "pow"=>left.Pow(P(node,"degree").GetInt32()),"root"=>left.Root(P(node,"degree").GetInt32()),
            "prefix"=>new JavaUnitAdapter(left).Prefix(SiPrefixCatalog.All.Single(prefix=>prefix.GetName()==S(node,"symbol"))).Model,
            "binaryPrefix"=>new JavaUnitAdapter(left).Prefix(JavaServiceProvider.Current.SystemOfUnitsService.BinaryPrefixes.Single(prefix=>prefix.GetName()==S(node,"symbol"))).Model,
            "transform"=>left.Transform(Converter(P(node,"converter"))),_=>throw new ArgumentException("Unknown unit fixture")
        };
    }
    private static JavaNumberQuantity Quantity(JsonElement node)=>new(Number(P(node,"value")),Unit(P(node,"unit")),
        node.TryGetProperty("relative",out var relative)&&relative.GetBoolean()?JavaQuantityScale.Relative:JavaQuantityScale.Absolute);
    private static object Observe(JsonElement test)
    {
        var action=S(test,"action");var domain=S(test,"domain");
        if(domain=="binaryPrefix")
        {
            var prefix=JavaServiceProvider.Current.SystemOfUnitsService.BinaryPrefixes.Single(item=>item.GetName()==P(test,"left").GetString());
            return prefix.GetName()+"|"+prefix.Symbol+"|"+ObserveNumber(prefix.GetValue())+"|"+prefix.Exponent.ToString(CultureInfo.InvariantCulture)+"|"+ObserveNumber(prefix.ExactValue);
        }
        if(domain=="prefix")
        {
            var prefix=SiPrefixCatalog.All.Single(item=>item.Name.ToUpperInvariant()==P(test,"left").GetString());
            return prefix.GetName()+"|"+prefix.Symbol+"|"+ObserveNumber(prefix.GetValue())+"|"+prefix.Exponent.ToString(CultureInfo.InvariantCulture)+"|"+ObserveNumber(prefix.ExactValue);
        }
        if(domain=="dimension")
        {
            var dimension=new JavaDimensionAdapter(Unit(P(test,"left")).Dimension);
            if(action=="base")
            {
                var map=dimension.GetBaseDimensions();if(map is null)return "null";
                var baseFields=new[]{"METRE","KILOGRAM","SECOND","AMPERE","KELVIN","MOLE","CANDELA"};
                return string.Join(";",baseFields.Select((field,index)=>(key:new JavaDimensionAdapter(JavaUnit.FromCatalog(field).Dimension),index))
                    .Where(pair=>map.ContainsKey(pair.key)).Select(pair=>pair.index.ToString(CultureInfo.InvariantCulture)+":"+map[pair.key].ToString(CultureInfo.InvariantCulture)));
            }
            var b=new JavaDimensionAdapter(Unit(P(test,"right")).Dimension);
            if(action=="hashEqual")return dimension.GetHashCode()==b.GetHashCode();
            var transformed=action switch
            {
                "multiply"=>dimension.Multiply(new(Unit(P(test,"operand")).Dimension)),"divide"=>dimension.Divide(new(Unit(P(test,"operand")).Dimension)),
                "pow"=>dimension.Pow(P(test,"degree").GetInt32()),"root"=>dimension.Root(P(test,"degree").GetInt32()),_=>dimension
            };
            return transformed.Equals(b);
        }
        if(domain=="number")
        {
            var a=Number(P(test,"left"));if(action=="identity")return ObserveNumber(a);
            if(action=="double")return BitConverter.DoubleToInt64Bits(a.ToDouble()).ToString("X16",CultureInfo.InvariantCulture);
            var b=Number(P(test,"right"));
            return action switch
            {
                "add"=>ObserveNumber(a.Add(b)),"subtract"=>ObserveNumber(a.Subtract(b)),"multiply"=>ObserveNumber(a.Multiply(b)),
                "divide"=>ObserveNumber(a.Divide(b)),"equal"=>a.Equals(b),"hashEqual"=>a.GetHashCode()==b.GetHashCode(),"compare"=>Math.Sign(a.CompareTo(b)),_=>throw new ArgumentException(action)
            };
        }
        if(domain=="converter")
        {
            var a=Converter(P(test,"left"));
            return action switch
            {
                "convert"=>ObserveNumber(a.Convert(Number(P(test,"value")))),
                "approximate"=>a.Convert(Number(P(test,"value"))).ToDouble(),
                "double"=>BitConverter.DoubleToInt64Bits(a.Convert(Number(P(test,"value")).ToDouble())).ToString("X16",CultureInfo.InvariantCulture),
                "identity"=>a.IsIdentity,"linear"=>a.IsLinear,"equal"=>a.Equals(Converter(P(test,"right"))),
                "hashEqual"=>a.GetHashCode()==Converter(P(test,"right")).GetHashCode(),
                "steps"=>string.Join(";",a.ConversionSteps.Select(step=>ObserveNumber(step.Convert(JavaNumber.Zero))+":"+ObserveNumber(step.Convert(JavaNumber.One)))),
                _=>throw new ArgumentException(action)
            };
        }
        if(domain=="unit")
        {
            var a=Unit(P(test,"left"));
            return action switch
            {
                "equal"=>a.Equals(Unit(P(test,"right"))),"equivalent"=>a.IsEquivalentTo(Unit(P(test,"right"))),
                "hashEqual"=>a.GetHashCode()==Unit(P(test,"right")).GetHashCode(),
                "convert"=>ObserveNumber(a.GetConverterToAny(Unit(P(test,"right"))).Convert(Number(P(test,"value")))),
                "system"=>ObserveNumber(a.GetConverterToAny(a.SystemUnit).Convert(Number(P(test,"value")))),
                "systemEqual"=>a.SystemUnit.Equals(Unit(P(test,"right"))),"metadata"=>(a.Symbol??"null")+"|"+(a.Name??"null"),
                "basecount"=>a.GetBaseUnits()?.Count??-1,"dimensionEqual"=>a.Dimension==Unit(P(test,"right")).Dimension,
                _=>throw new ArgumentException(action)
            };
        }
        var q=Quantity(P(test,"left"));
        if(action=="equal")return q.Equals(Quantity(P(test,"right")));
        if(action=="equivalent")return q.IsEquivalentTo(Quantity(P(test,"right")));
        if(action=="compare")return Math.Sign(q.CompareTo(Quantity(P(test,"right"))));
        var result=action switch
        {
            "identity"=>q,"convert"=>q.To(Unit(P(test,"to"))),"inverse"=>q.Inverse(),
            "add"=>q.Add(Quantity(P(test,"right"))),"subtract"=>q.Subtract(Quantity(P(test,"right"))),
            "multiply"=>q.Multiply(Quantity(P(test,"right"))),"divide"=>q.Divide(Quantity(P(test,"right"))),
            "scalarMultiply"=>q.Multiply(Number(P(test,"scalar"))),"scalarDivide"=>q.Divide(Number(P(test,"scalar"))),
            _=>throw new ArgumentException(action)
        };
        if(test.TryGetProperty("resultUnit",out var resultUnit))return result.Unit.Equals(Unit(resultUnit));
        if(test.TryGetProperty("normalizeTo",out var target))result=result.To(Unit(target));
        return ObserveNumber(result.Value)+"|"+result.Scale.ToString().ToUpperInvariant();
    }
}
