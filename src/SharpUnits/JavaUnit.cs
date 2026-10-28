using System.Collections.ObjectModel;

namespace SharpUnits;

/// <summary>Structural categories in the immutable Java-compatible unit graph.</summary>
public enum JavaUnitKind
{
    /// <summary>A named fundamental unit.</summary>
    Base,
    /// <summary>A distinct named alternate of an unscaled system unit.</summary>
    Alternate,
    /// <summary>A converter applied before conversion through its parent.</summary>
    Transformed,
    /// <summary>A normalized commutative product with rational exponents.</summary>
    Product
}

/// <summary>Immutable compositional unit identity independent of text formatting.</summary>
/// <remarks>Products normalize equal atoms, not merely equal dimensions. Alternate
/// units remain distinct; transformed parents and converter operations remain structural.
/// Fractional-root products may have dimensions but no system converter, as in pinned
/// Indriya. Graphs are bounded to depth 64, 128 product atoms and exponents of magnitude
/// at most 1024. Rendering/parsing these graphs belongs to the format adapter.</remarks>
public sealed class JavaUnit : IEquatable<JavaUnit>
{
    // @relation(JP-WP1-UNIT, scope=file)
    private readonly JavaUnit? parent;
    private readonly JavaUnitConverterAdapter? operation;
    private readonly IReadOnlyDictionary<JavaUnit,Rational> factors;
    private readonly int depth;
    private readonly int structuralHash;
    private static readonly RationalDimension none=RationalDimension.From(SharpUnits.Dimension.None);
    private static readonly string[] baseSymbols={"m","kg","s","A","K","mol","cd"};
    private static readonly string[] baseNames={"Metre","Kilogram","Second","Ampere","Kelvin","Mole","Candela"};
    private static readonly JavaUnit[] bases=Enumerable.Range(0,7).Select(i=>
    {
        var vector=new int[7];vector[i]=1;
        return Base(baseSymbols[i],RationalDimension.From(new Dimension(vector[0],vector[1],vector[2],vector[3],vector[4],vector[5],vector[6])),baseNames[i]);
    }).ToArray();
    /// <summary>The normalized empty product.</summary>
    public static JavaUnit One { get; }=new(JavaUnitKind.Product,"",null,none,null,null,new Dictionary<JavaUnit,Rational>());
    private static readonly Lazy<IReadOnlyDictionary<string,JavaUnit>> catalog=new(BuildCatalog);
    /// <summary>Gets a pinned built-in unit by its Java Units field identifier.</summary>
    public static JavaUnit FromCatalog(string field)
    {
        ArgumentNullException.ThrowIfNull(field);return catalog.Value.TryGetValue(field,out var unit)?unit:throw new KeyNotFoundException(field);
    }
    /// <summary>Creates an isolated immutable catalog graph for an explicit service or verification scope.</summary>
    /// <remarks>FromCatalog reuses a shared immutable catalog; this factory constructs independent graph objects.</remarks>
    public static IReadOnlyDictionary<string,JavaUnit> CreateCatalog()=>BuildCatalog();
    /// <summary>The structural unit category.</summary>
    public JavaUnitKind Kind { get; }
    /// <summary>Intrinsic symbol; products and unnamed transforms have none.</summary>
    public string? Symbol { get; }
    /// <summary>Intrinsic name; generated transforms and products have none.</summary>
    public string? Name { get; }
    /// <summary>Exact rational dimensions, independent of converter availability.</summary>
    public RationalDimension Dimension { get; }
    /// <summary>Normalized product factors; non-product units have an empty map.</summary>
    public IReadOnlyDictionary<JavaUnit,Rational> Factors=>factors;
    private JavaUnit(JavaUnitKind kind,string? symbol,string? name,RationalDimension dimension,JavaUnit? parent,
        JavaUnitConverterAdapter? operation,Dictionary<JavaUnit,Rational>? factors=null)
    {
        Kind=kind;Symbol=symbol;Name=name;Dimension=dimension;this.parent=parent;this.operation=operation;
        this.factors=new ReadOnlyDictionary<JavaUnit,Rational>(factors??new());
        depth=1+Math.Max(parent?.depth??0,this.factors.Count==0?0:this.factors.Keys.Max(unit=>unit.depth));
        if(depth>64)throw new ArgumentOutOfRangeException(nameof(parent),"Unit graph depth budget exceeded.");
        // The graph is immutable. Compute once so shared parent subgraphs do not
        // cause repeated recursive hashing when used as normalized product keys.
        structuralHash=ComputeHash();
    }
    /// <summary>Creates a named base unit with explicit dimensions.</summary>
    public static JavaUnit Base(string symbol,RationalDimension dimension,string? name=null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        _=new JavaDimensionAdapter(dimension);
        return new(JavaUnitKind.Base,symbol,name,dimension,null,null);
    }
    /// <summary>Creates an alternate of an unscaled system unit, rejecting transformed parents.</summary>
    public JavaUnit Alternate(string symbol,string? name=null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if(!Equals(SystemUnit))throw new InvalidOperationException("An alternate requires an unscaled system unit.");
        return new(JavaUnitKind.Alternate,symbol,name,Dimension,Kind==JavaUnitKind.Alternate?parent:this,null);
    }
    /// <summary>Applies a converter to this unit; the converter executes before the parent mapping.</summary>
    public JavaUnit Transform(JavaUnitConverterAdapter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return converter.IsIdentity?SystemUnit:new(JavaUnitKind.Transformed,null,null,Dimension,this,converter);
    }
    /// <summary>Scales this unit exactly; multiplying by one returns the original unit.</summary>
    public JavaUnit Multiply(JavaNumber factor)
    {
        ArgumentNullException.ThrowIfNull(factor);return factor.CompareTo(JavaNumber.One)==0?this:Transform(JavaUnitConverterAdapter.Multiply(factor));
    }
    /// <summary>Divides this unit by an exact scalar, rejecting zero.</summary>
    public JavaUnit Divide(JavaNumber divisor){ArgumentNullException.ThrowIfNull(divisor);return Multiply(divisor.Reciprocal());}
    /// <summary>Shifts in source units before applying the parent's converter.</summary>
    public JavaUnit Shift(JavaNumber offset)
    {
        ArgumentNullException.ThrowIfNull(offset);return offset.CompareTo(JavaNumber.Zero)==0?this:Transform(JavaUnitConverterAdapter.Add(offset));
    }
    /// <summary>Creates a normalized product without prematurely demanding a linear converter.</summary>
    public JavaUnit Multiply(JavaUnit other)
    {
        ArgumentNullException.ThrowIfNull(other);var result=new Dictionary<JavaUnit,Rational>();
        Accumulate(result,this,1);Accumulate(result,other,1);return Product(result);
    }
    /// <summary>Creates a normalized quotient.</summary>
    public JavaUnit Divide(JavaUnit other){ArgumentNullException.ThrowIfNull(other);return Multiply(other.Pow(-1));}
    /// <summary>Raises a unit to a bounded integral power, including zero and negative powers.</summary>
    public JavaUnit Pow(int exponent)
    {
        if(exponent is < -1024 or >1024)throw new ArgumentOutOfRangeException(nameof(exponent));
        var result=new Dictionary<JavaUnit,Rational>();Accumulate(result,this,new Rational(exponent,1));return Product(result);
    }
    /// <summary>Returns a positive or negative integer-degree root; zero is invalid.</summary>
    public JavaUnit Root(int degree)
    {
        if(degree==0)throw new DivideByZeroException("Root degree must not be zero.");
        if(degree is < -1024 or >1024)throw new ArgumentOutOfRangeException(nameof(degree));
        var result=new Dictionary<JavaUnit,Rational>();Accumulate(result,this,new Rational(1,degree));return Product(result);
    }
    /// <summary>Returns the structural system unit, retaining alternate-unit identities.</summary>
    public JavaUnit SystemUnit=>Kind switch
    {
        JavaUnitKind.Base or JavaUnitKind.Alternate=>this,
        JavaUnitKind.Transformed=>parent!.SystemUnit,
        _=>Product(factors.Aggregate(new Dictionary<JavaUnit,Rational>(),(map,pair)=>{Accumulate(map,pair.Key.SystemUnit,pair.Value);return map;}))
    };
    /// <summary>Returns the converter to the structural system unit.</summary>
    /// <exception cref="NotSupportedException">An affine/nonlinear atom or fractional-root product has no supported product converter.</exception>
    public JavaUnitConverterAdapter SystemConverter
    {
        get
        {
            if(Kind is JavaUnitKind.Base or JavaUnitKind.Alternate)return JavaUnitConverterAdapter.Identity;
            if(Kind==JavaUnitKind.Transformed)return parent!.SystemConverter.Concatenate(operation!);
            var converter=JavaUnitConverterAdapter.Identity;
            foreach(var pair in factors)
            {
                if(!pair.Value.Denominator.IsOne)throw new NotSupportedException("Pinned product converters reject fractional roots.");
                var next=pair.Key.SystemConverter;
                if(!next.IsLinear)throw new NotSupportedException("A non-linear atom has no product converter.");
                var power=checked((int)pair.Value.Numerator);if(power<0){next=next.Inverse();power=-power;}
                for(var i=0;i<power;i++)converter=converter.Concatenate(next);
            }
            return converter;
        }
    }
    /// <summary>Gets a converter between dimensionally compatible units.</summary>
    public JavaUnitConverterAdapter GetConverterTo(JavaUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if(Dimension!=target.Dimension)throw new InvalidOperationException("Incompatible dimensions.");
        return Equals(target)?JavaUnitConverterAdapter.Identity:target.SystemConverter.Inverse().Concatenate(SystemConverter);
    }
    /// <summary>Converts any compatible unit, evaluating converters even for structurally equal units as pinned getConverterToAny does.</summary>
    public JavaUnitConverterAdapter GetConverterToAny(JavaUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if(Dimension!=target.Dimension)throw new InvalidOperationException("Incompatible dimensions.");
        return target.SystemConverter.Inverse().Concatenate(SystemConverter);
    }
    /// <summary>Tests converter identity separately from structural unit equality.</summary>
    public bool IsEquivalentTo(JavaUnit? other)=>other is not null&&Dimension==other.Dimension&&GetConverterTo(other).IsIdentity;
    /// <summary>Java-style base-unit mapping; null denotes a base unit.</summary>
    /// <remarks>As pinned Java's integer Map does, product entries expose numerator powers;
    /// use Factors when exact rational exponents are required.</remarks>
    public IReadOnlyDictionary<JavaUnit,int>? GetBaseUnits()=>Kind switch
    {
        JavaUnitKind.Base=>null,
        JavaUnitKind.Alternate or JavaUnitKind.Transformed=>parent!.GetBaseUnits(),
        _=>new ReadOnlyDictionary<JavaUnit,int>(factors.ToDictionary(pair=>pair.Key,pair=>checked((int)pair.Value.Numerator)))
    };
    private static void Accumulate(Dictionary<JavaUnit,Rational> result,JavaUnit unit,Rational power)
    {
        if(power.Numerator.IsZero)return;
        if(unit.Kind==JavaUnitKind.Product){foreach(var pair in unit.factors)Accumulate(result,pair.Key,pair.Value*power);return;}
        var updated=(result.TryGetValue(unit,out var old)?old:new Rational(0,1))+power;
        if(updated.Numerator.IsZero)result.Remove(unit);else result[unit]=updated;
    }
    private static JavaUnit Product(Dictionary<JavaUnit,Rational> result)
    {
        if(result.Count==0)return One;
        if(result.Count>128)throw new ArgumentOutOfRangeException(nameof(result),"Product atom budget exceeded.");
        if(result.Count==1&&result.First().Value==new Rational(1,1))return result.First().Key;
        var dimension=none;
        foreach(var pair in result)
        {
            if(System.Numerics.BigInteger.Abs(pair.Value.Numerator)>1024||pair.Value.Denominator>1024)
                throw new ArgumentOutOfRangeException(nameof(result),"Product exponent budget exceeded.");
            dimension+=pair.Key.Dimension.Pow(pair.Value);
        }
        return new(JavaUnitKind.Product,null,null,dimension,null,null,result);
    }
    /// <summary>Creates the product of the seven coherent SI base units for a dimension.</summary>
    public static JavaUnit Coherent(RationalDimension dimension)
    {
        _=new JavaDimensionAdapter(dimension);
        var values=new[]{dimension.Length,dimension.Mass,dimension.Time,dimension.Current,dimension.Temperature,dimension.Amount,dimension.Luminosity};
        var map=new Dictionary<JavaUnit,Rational>();for(var i=0;i<7;i++)Accumulate(map,bases[i],values[i]);return Product(map);
    }
    /// <summary>Imports existing finite affine unit definitions without changing native records.</summary>
    public static JavaUnit FromDefinition(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if(definition.JavaModel is not null)return definition.JavaModel;
        _=new UnitConversion(definition.Converter.Scale,definition.Converter.Offset);
        var entries=JavaUnitCatalogData.Entries();
        var entry=entries.FirstOrDefault(candidate=>candidate.Unit==definition);
        if(entry is not null)return FromCatalog(entry.Field);
        if(entry?.Field=="ONE"||definition.Symbol is "one" or "1"&&definition.RationalDimension==none&&definition.Converter.IsIdentity)return One;
        var coherent=Coherent(definition.RationalDimension);
        if(definition.Converter.IsIdentity)
        {
            if(coherent.Symbol==definition.Symbol)return coherent;
            return entry is not null?coherent.Alternate(definition.Symbol,entry.Name):Base(definition.Symbol,definition.RationalDimension);
        }
        // Recover a registered prefixed parent before dimensional fallback, preserving
        // named system-unit identities such as volt and ohm rather than flattening them.
        var parent=coherent;
        int? prefixExponent=null;
        foreach(var prefix in SiPrefixes.Definitions.OrderByDescending(pair=>pair.Key.Length))
            if(definition.Symbol.StartsWith(prefix.Key,StringComparison.Ordinal))
            {
                var candidate=entries.FirstOrDefault(item=>item.Unit.Symbol==definition.Symbol[prefix.Key.Length..]&&item.Unit.Converter.IsIdentity&&item.Unit.RationalDimension==definition.RationalDimension);
                if(candidate is not null){parent=FromDefinition(candidate.Unit);prefixExponent=prefix.Value;break;}
            }
        if(definition.Converter==UnitConversion.Fahrenheit&&definition.RationalDimension==bases[4].Dimension)
            return parent.Multiply(JavaNumber.Ratio(5,9)).Shift(JavaNumber.Ratio(45967,100));
        var scale=JavaDoubleArithmetic.ScaleOf(definition);
        var transformed=parent.Multiply(JavaNumber.Ratio(scale.Numerator,scale.Denominator));
        if(prefixExponent is not null&&definition.Converter.IsLinear)
        {
            var prefixConverter=JavaUnitConverterAdapter.ForPrefix(10,prefixExponent.Value);
            if(prefixConverter.AffineCoefficients().Scale.Fraction==scale)transformed=parent.Transform(prefixConverter);
        }
        if(definition.Converter.Offset!=0)
            // Offset is in SI units in UnitDefinition, whereas Shift is in source units.
            transformed=transformed.Shift(JavaNumber.FromNumeric(JavaDoubleArithmetic.Fraction(definition.Converter.Offset)/scale));
        return new(JavaUnitKind.Transformed,definition.Symbol,entry?.Name,definition.RationalDimension,parent,
            parent.SystemConverter.Inverse().Concatenate(transformed.SystemConverter));
    }
    /// <summary>Projects an affine graph into a native definition while retaining its structural identity.</summary>
    /// <remarks>Nonlinear and unsupported-root graphs deliberately cannot become native affine definitions.</remarks>
    public UnitDefinition ToDefinition(string? symbol=null)
    {
        var c=SystemConverter.AffineCoefficients();var projected=c.Scale.ToDouble();var offset=c.Offset.ToDouble();
        var result=offset==0?UnitDefinition.FromExactLinear(symbol??Symbol??"Java-derived",Dimension,c.Scale.Fraction)
            :new UnitDefinition(symbol??Symbol??"Java-derived",Dimension,new UnitConversion(projected,offset));
        return result with {JavaModel=this};
    }
    private static IReadOnlyDictionary<string,JavaUnit> BuildCatalog()
    {
        // These are domain definitions from pinned Units.java, not dimension-only
        // aliases. Preserving the parent graph matters for Java structural equality.
        var result=new Dictionary<string,JavaUnit>(StringComparer.Ordinal);
        var entries=JavaUnitCatalogData.Entries().ToDictionary(entry=>entry.Field);
        void Put(string field,JavaUnit unit,string? symbol=null)=>result[field]=new(unit.Kind,symbol??unit.Symbol,entries[field].Name,
            unit.Dimension,unit.parent,unit.operation,unit.factors.ToDictionary(pair=>pair.Key,pair=>pair.Value));
        var fields=new[]{"METRE","KILOGRAM","SECOND","AMPERE","KELVIN","MOLE","CANDELA"};
        for(var i=0;i<7;i++)Put(fields[i],bases[i]);result["ONE"]=One;
        JavaUnit U(string field)=>result[field];JavaNumber N(int value)=>JavaNumber.Integer(value);
        void Alt(string field,JavaUnit parent)=>Put(field,parent.Alternate(entries[field].Unit.Symbol));
        Put("GRAM",U("KILOGRAM").Divide(N(1000)));
        Alt("RADIAN",One);Alt("STERADIAN",One);Alt("HERTZ",One.Divide(U("SECOND")));
        Alt("NEWTON",U("METRE").Multiply(U("KILOGRAM")).Divide(U("SECOND").Pow(2)));
        Alt("PASCAL",U("NEWTON").Divide(U("METRE").Pow(2)));
        Alt("JOULE",U("NEWTON").Multiply(U("METRE")));Alt("WATT",U("JOULE").Divide(U("SECOND")));
        Alt("COULOMB",U("SECOND").Multiply(U("AMPERE")));Alt("VOLT",U("WATT").Divide(U("AMPERE")));
        Alt("FARAD",U("COULOMB").Divide(U("VOLT")));Alt("OHM",U("VOLT").Divide(U("AMPERE")));
        Alt("SIEMENS",U("AMPERE").Divide(U("VOLT")));Alt("WEBER",U("VOLT").Multiply(U("SECOND")));
        Alt("TESLA",U("WEBER").Divide(U("METRE").Pow(2)));Alt("HENRY",U("WEBER").Divide(U("AMPERE")));
        Put("CELSIUS",U("KELVIN").Shift(JavaNumber.FromDouble(273.15)),"℃");
        Alt("LUMEN",U("CANDELA").Multiply(U("STERADIAN")));Alt("LUX",U("LUMEN").Divide(U("METRE").Pow(2)));
        Alt("BECQUEREL",One.Divide(U("SECOND")));Alt("GRAY",U("JOULE").Divide(U("KILOGRAM")));
        Alt("SIEVERT",U("JOULE").Divide(U("KILOGRAM")));Alt("KATAL",U("MOLE").Divide(U("SECOND")));
        Put("METRE_PER_SECOND",U("METRE").Divide(U("SECOND")));
        Put("METRE_PER_SQUARE_SECOND",U("METRE_PER_SECOND").Divide(U("SECOND")));
        Put("SQUARE_METRE",U("METRE").Pow(2));Put("CUBIC_METRE",U("METRE").Pow(3));
        Put("KILOMETRE_PER_HOUR",U("METRE_PER_SECOND").Multiply(JavaNumber.Ratio(5,18)));
        Put("PERCENT",One.Multiply(JavaNumber.Ratio(1,100)),"%");
        Put("MINUTE",U("SECOND").Multiply(N(60)),"min");Put("HOUR",U("SECOND").Multiply(N(3600)),"h");
        Put("DAY",U("SECOND").Multiply(N(86400)),"d");Put("WEEK",U("DAY").Multiply(N(7)),"wk");
        Put("YEAR",U("DAY").Multiply(JavaNumber.Ratio(3652425,10000)),"yr");
        Put("MONTH",U("YEAR").Divide(N(12)),"mo");Put("LITRE",U("CUBIC_METRE").Divide(N(1000)),"l");
        if(result.Count!=entries.Count)throw new InvalidOperationException("Incomplete pinned unit graph catalog.");
        return new ReadOnlyDictionary<string,JavaUnit>(result);
    }
    /// <summary>Structural equality; product factor order and metadata names do not affect identity.</summary>
    public bool Equals(JavaUnit? other)
    {
        if(ReferenceEquals(this,other))return true;
        if(other is null||Kind!=other.Kind)return false;
        return Kind switch
        {
            JavaUnitKind.Base=>Symbol==other.Symbol&&Dimension==other.Dimension,
            JavaUnitKind.Alternate=>Symbol==other.Symbol&&Equals(parent,other.parent),
            JavaUnitKind.Transformed=>Equals(parent,other.parent)&&Equals(operation,other.operation),
            _=>factors.Count==other.factors.Count&&factors.All(pair=>other.factors.TryGetValue(pair.Key,out var power)&&pair.Value==power)
        };
    }
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaUnit other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode()=>structuralHash;
    private int ComputeHash()=>Kind switch
    {
        JavaUnitKind.Base=>HashCode.Combine(Kind,Symbol,Dimension),
        JavaUnitKind.Alternate=>HashCode.Combine(Kind,Symbol,parent),
        JavaUnitKind.Transformed=>HashCode.Combine(Kind,parent,operation),
        _=>factors.Aggregate((int)Kind,(hash,pair)=>hash^HashCode.Combine(pair.Key,pair.Value))
    };
}
