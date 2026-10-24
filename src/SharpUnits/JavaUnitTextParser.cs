namespace SharpUnits;

/// <summary>Resolves Java parser aliases without changing the caller's registry.</summary>
internal static class JavaUnitTextParser
{
    // @relation(JP-014-ALIAS, scope=file)
    internal static UnitDefinition Resolve(string symbol,UnitRegistry registry)
    {
        try{return registry.Resolve(symbol);}
        catch(KeyNotFoundException)
        {
            // An exact caller registration wins over this compatibility alias.
            if(symbol=="℃")return registry.Resolve("°C");
            if(symbol=="1")return new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1));
            throw;
        }
    }
}
