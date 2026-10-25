using System.Numerics;
using System.Text;

namespace SharpUnits;

/// <summary>Selects the supported invariant unit-format convention.</summary>
public enum JavaUnitFormatFlavor
{
    /// <summary>Pinned SIMPLE default labels and product text.</summary>
    Default,
    /// <summary>Pinned SIMPLE ASCII labels, including its documented Unicode fallbacks.</summary>
    Ascii
}

/// <summary>Mutable, isolated label/alias maps with bounded Java unit parsing and formatting.</summary>
/// <remarks>Labels are keyed by CLR unit-record identity. General Java unit normalization,
/// foreign Java unit identity, affine prefixes and parse cursors remain incomplete.
/// Public ASCII instances fall back to the built-in default formatter as Java does;
/// services constructed explicitly use their own default formatter as the fallback.</remarks>
public sealed class JavaUnitTextFormat
{
    // @relation(JP-015, scope=file)
    private readonly object sync=new();
    private readonly Dictionary<string,UnitDefinition> names=new(StringComparer.Ordinal);
    private readonly Dictionary<UnitDefinition,string> labels=new();
    private readonly JavaUnitTextFormat? fallback;
    private readonly JavaUnitFormatFlavor flavor;
    private static readonly Dictionary<string,JavaUnitEntry> catalog=JavaUnitCatalogData.Entries().ToDictionary(e=>e.Field,StringComparer.Ordinal);
    private static readonly Dictionary<UnitDefinition,JavaUnitEntry> byUnit=catalog.Values.ToDictionary(e=>e.Unit);
    private static readonly HashSet<string> metricFields=new(StringComparer.Ordinal)
    {
        "AMPERE","BECQUEREL","CANDELA","COULOMB","FARAD","GRAY","HENRY","HERTZ","JOULE","KATAL","KELVIN","LUMEN","LUX",
        "METRE","MOLE","NEWTON","OHM","PASCAL","RADIAN","SECOND","SIEMENS","SIEVERT","STERADIAN","TESLA","VOLT","WATT","WEBER"
    };

    /// <summary>Creates a fresh formatter using a snapshot of explicitly registered units.</summary>
    public JavaUnitTextFormat(UnitRegistry registry,JavaUnitFormatFlavor flavor=JavaUnitFormatFlavor.Default):this(registry,flavor,null){}
    internal JavaUnitTextFormat(UnitRegistry registry,JavaUnitFormatFlavor flavor,JavaUnitTextFormat? defaultFormat)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if(!Enum.IsDefined(flavor))throw new ArgumentOutOfRangeException(nameof(flavor));
        this.flavor=flavor;
        var units=registry.Snapshot().Select(pair=>pair.Value).Distinct().OrderBy(unit=>unit.Symbol,StringComparer.Ordinal).ToArray();
        if(flavor==JavaUnitFormatFlavor.Default)SeedDefault(units);
        else
        {
            fallback=defaultFormat??JavaServiceProvider.Current.FormatService.GetUnitFormat();
            SeedAscii(units);
        }
    }

    /// <summary>Reports the invariant formatting convention; mutable labels do not make it locale sensitive.</summary>
    public bool IsLocaleSensitive=>false;

    /// <summary>Sets a display label and parse binding; previous labels remain parse aliases.</summary>
    /// <remarks>Collisions replace only the parse owner. Another unit can retain the same display label.
    /// Default flavor validates only the first UTF-16 character, as pinned Java does.
    /// ASCII flavor additionally requires every character to be ASCII.</remarks>
    public void Label(UnitDefinition unit,string label)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ValidateIdentifier(label);
        lock(sync){names[label]=unit;labels[unit]=label;}
    }

    /// <summary>Sets a parse-only alias, replacing its previous parse owner without changing display labels.</summary>
    public void Alias(UnitDefinition unit,string alias)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ValidateIdentifier(alias);
        lock(sync)names[alias]=unit;
    }

    /// <summary>Removes the display override and all parse names currently owned by this unit.</summary>
    public void RemoveLabel(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        lock(sync)
        {
            labels.Remove(unit);
            foreach(var key in names.Where(pair=>pair.Value==unit).Select(pair=>pair.Key).ToArray())names.Remove(key);
        }
    }

    /// <summary>Removes a parse name without changing display labels.</summary>
    /// <remarks>As in pinned Java, the supplied unit does not constrain which parse binding is removed.
    /// A null alias is a no-op. CLR unit arguments must be non-null.</remarks>
    public void RemoveAlias(UnitDefinition unit,string? alias)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if(alias is null)return;
        lock(sync)names.Remove(alias);
    }

    /// <summary>Removes parse aliases owned by this unit except its current display label.</summary>
    public void RemoveAliases(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        lock(sync)
        {
            labels.TryGetValue(unit,out var current);
            foreach(var key in names.Where(pair=>pair.Value==unit&&pair.Key!=current).Select(pair=>pair.Key).ToArray())names.Remove(key);
        }
    }

    /// <summary>Formats a unit using its current label or the supported intrinsic/fallback representation.</summary>
    public string Format(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if(TryLabel(unit,out var label))return label;
        if(fallback is not null)
        {
            if(fallback.TryLabel(unit,out label))return label;
            // These catalog product graphs have no default label. Area/volume retain
            // Unicode when the default database still labels them, even in ASCII flavor.
            if(byUnit.TryGetValue(unit,out var entry))
            {
                string M()=>Format(catalog["METRE"].Unit);
                string S()=>Format(catalog["SECOND"].Unit);
                switch(entry.Field)
                {
                    case "METRE_PER_SECOND":return M()+"*"+S()+"^-1";
                    case "METRE_PER_SQUARE_SECOND":return M()+"*"+S()+"^-2";
                    case "SQUARE_METRE":return M()+"^2";
                    case "CUBIC_METRE":return M()+"^3";
                }
            }
            return fallback.Format(unit);
        }
        if(byUnit.TryGetValue(unit,out var product))
        {
            string M()=>Format(catalog["METRE"].Unit);
            string S()=>Format(catalog["SECOND"].Unit);
            switch(product.Field)
            {
                case "METRE_PER_SECOND":return M()+"/"+S();
                case "METRE_PER_SQUARE_SECOND":return M()+"/"+S()+"²";
                case "SQUARE_METRE":return M()+"²";
                case "CUBIC_METRE":return M()+"³";
            }
        }
        var symbol=QuantityTextFormat.JavaSymbol(unit);
        return symbol.Length==0?"one":symbol;
    }

    /// <summary>Appends unit text and returns the original builder.</summary>
    public StringBuilder Format(UnitDefinition unit,StringBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return destination.Append(Format(unit));
    }

    /// <summary>Writes unit text without flushing or closing the caller's writer.</summary>
    /// <exception cref="IOException">The destination rejects the write; the error is propagated.</exception>
    public TextWriter Format(UnitDefinition unit,TextWriter destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Write(Format(unit));
        return destination;
    }

    /// <summary>Parses current labels/aliases, otherwise using the bounded Java unit grammar.</summary>
    public UnitDefinition Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var snapshot=NameSnapshot();
        // Java checks the entire input mapping before tokenizing: accepted labels can
        // contain spaces/operators after their initial character.
        if(snapshot.TryGetValue(text,out var unit))return unit;
        return new QuantityTextFormat(UnitRegistry.FromAliases(snapshot)).ParseJavaQuantity("1 "+text).Unit;
    }

    internal JavaQuantityAdapter ParseQuantity(string text)=>new QuantityTextFormat(UnitRegistry.FromAliases(NameSnapshot())).ParseJavaQuantity(text);
    private bool TryLabel(UnitDefinition unit,out string label){lock(sync)return labels.TryGetValue(unit,out label!);}
    private Dictionary<string,UnitDefinition> NameSnapshot()
    {
        // Never hold an ASCII lock while taking the default lock: one lock order keeps
        // concurrent reads/mutations free of lock inversion.
        var result=fallback?.NameSnapshot()??new Dictionary<string,UnitDefinition>(StringComparer.Ordinal);
        lock(sync)foreach(var pair in names)result[pair.Key]=pair.Value;
        return result;
    }
    private void ValidateIdentifier(string? value)
    {
        if(string.IsNullOrEmpty(value)||!IdentifierStart(value[0])||flavor==JavaUnitFormatFlavor.Ascii&&value.Any(c=>c>127))
            throw new ArgumentException("Label/alias is not a valid identifier.",nameof(value));
    }
    private static bool IdentifierStart(char value)=>char.IsLetter(value)||!JavaQuantityTextParser.IsJavaWhitespace(value)&&!char.IsDigit(value)
        &&value is not ('·' or '*' or '/' or '(' or ')' or '[' or ']' or '¹' or '²' or '³' or '^' or '+' or '-');
    private void Seed(UnitDefinition unit,string text){names[text]=unit;labels[unit]=text;}
    private static UnitDefinition Prefix(UnitDefinition unit,string symbol,int exponent)
    {
        var power=BigInteger.Pow(10,Math.Abs(exponent));
        var factor=exponent>=0?new Rational(power,1):new Rational(1,power);
        return UnitDefinition.FromExactLinear(symbol+unit.Symbol,unit.RationalDimension,JavaDoubleArithmetic.ScaleOf(unit)*factor);
    }
    private static IEnumerable<(string Name,UnitDefinition Unit)> BinaryOhmAliases(UnitDefinition unit)
    {
        // Prefix metadata is radix 1024 with ordinal exponent, not an early-rounded
        // decimal projection of 2^(10*n). Keep exact factors through adapter conversion.
        var symbols=new[]{"Ki","Mi","Gi","Ti","Pi","Ei","Zi","Yi"};
        for(var i=0;i<symbols.Length;i++)
            yield return (symbols[i]+"Ohm",UnitDefinition.FromExactLinear(symbols[i]+unit.Symbol,unit.RationalDimension,
                JavaDoubleArithmetic.ScaleOf(unit)*new Rational(BigInteger.Pow(1024,i+1),1)));
    }
    private void SeedDefault(UnitDefinition[] units)
    {
        foreach(var unit in units)
        {
            var field=byUnit.GetValueOrDefault(unit)?.Field;
            // Java parses these product graphs rather than storing a direct name; a
            // later alias of m or s must affect parsing of m/s too.
            if(field is "METRE_PER_SECOND" or "METRE_PER_SQUARE_SECOND")continue;
            var symbol=QuantityTextFormat.JavaSymbol(unit);Seed(unit,symbol.Length==0?"one":symbol);
            names[unit.Symbol]=unit;
            if(field is null||!metricFields.Contains(field)&&field is not ("GRAM" or "SQUARE_METRE" or "CUBIC_METRE" or "LITRE"))continue;
            foreach(var prefix in SiPrefixes.Definitions)
            {
                var derived=Prefix(unit,prefix.Key,prefix.Value);Seed(derived,prefix.Key+unit.Symbol);
                if(prefix.Key=="µ")
                {
                    names["μ"+unit.Symbol]=derived;
                    if(metricFields.Contains(field))Seed(derived,"μ"+unit.Symbol);
                }
                if(field=="OHM")names[prefix.Key+"Ohm"]=derived;
                if(field is "SQUARE_METRE" or "CUBIC_METRE")names[prefix.Key+(field=="SQUARE_METRE"?"m2":"m3")]=derived;
            }
        }
        foreach(var pair in new[]{("DAY","day"),("WEEK","week"),("YEAR","y"),("YEAR","year"),("YEAR","days365"),("YEAR","a"),("MONTH","mon"),("MONTH","month")})
            if(units.Contains(catalog[pair.Item1].Unit))names[pair.Item2]=catalog[pair.Item1].Unit;
        if(units.Contains(catalog["OHM"].Unit))
        {
            names["Ohm"]=catalog["OHM"].Unit;
            foreach(var binary in BinaryOhmAliases(catalog["OHM"].Unit))names[binary.Name]=binary.Unit;
        }
        if(units.Contains(catalog["SQUARE_METRE"].Unit))names["m2"]=catalog["SQUARE_METRE"].Unit;
        if(units.Contains(catalog["CUBIC_METRE"].Unit))names["m3"]=catalog["CUBIC_METRE"].Unit;
        if(units.Contains(catalog["CELSIUS"].Unit))names["°C"]=catalog["CELSIUS"].Unit;
        OverlayRegistrations(units);
    }
    private void OverlayRegistrations(UnitDefinition[] units)
    {
        // Explicit registry entries win over inferred prefixes regardless of hash-map
        // enumeration order. Product graphs still resolve mutable components at parse time.
        foreach(var unit in units)
        {
            if(byUnit.GetValueOrDefault(unit)?.Field is "METRE_PER_SECOND" or "METRE_PER_SQUARE_SECOND")continue;
            var symbol=QuantityTextFormat.JavaSymbol(unit);Seed(unit,symbol.Length==0?"one":symbol);names[unit.Symbol]=unit;
        }
    }
    private void SeedAscii(UnitDefinition[] units)
    {
        foreach(var definition in JavaUnitAsciiData.Definitions())
        {
            var entry=catalog[definition.Field];
            if(definition.Own&&units.Contains(entry.Unit))Seed(entry.Unit,definition.Label);
        }
        foreach(var unit in units)
        {
            var field=byUnit.GetValueOrDefault(unit)?.Field;
            if(field is null)continue;
            if(metricFields.Contains(field)&&field!="OHM")Seed(Prefix(unit,"µ",-6),"micro"+unit.Symbol);
            if(field is "GRAM" or "LITRE")
                foreach(var prefix in SiPrefixes.Definitions)Seed(Prefix(unit,prefix.Key,prefix.Value),prefix.Key=="µ"?(field=="GRAM"?"microg":"microL"):prefix.Key+(field=="GRAM"?"g":"L"));
            if(field=="OHM")
            {
                foreach(var prefix in JavaUnitAsciiData.Prefixes())
                    Seed(Prefix(unit,prefix.Prefix,SiPrefixes.Exponent(prefix.Prefix)),prefix.Label);
                foreach(var binary in BinaryOhmAliases(unit))Seed(binary.Unit,binary.Name);
            }
        }
        if(units.Contains(catalog["CELSIUS"].Unit))names["Cel"]=catalog["CELSIUS"].Unit;
        if(units.Contains(catalog["SQUARE_METRE"].Unit))names["m2"]=catalog["SQUARE_METRE"].Unit;
        if(units.Contains(catalog["CUBIC_METRE"].Unit))names["m3"]=catalog["CUBIC_METRE"].Unit;
    }
}
