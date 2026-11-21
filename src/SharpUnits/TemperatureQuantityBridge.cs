namespace SharpUnits;

/// <summary>Bridges temperature-specific types and unified quantities explicitly.</summary>
public static class TemperatureQuantityBridge
{
    private static readonly RationalDimension TemperatureDimension = RationalDimension.ThermodynamicTemperature;

    /// <summary>Converts an absolute temperature to a unified absolute quantity in kelvin.</summary>
    public static Quantity ToQuantity(this AbsoluteTemperature temperature) =>
        Quantity.Of(temperature.Kelvin, Units.Kelvin, QuantityScale.Absolute, QuantityKind.Temperature);

    /// <summary>Converts a temperature difference to a unified relative quantity in kelvin.</summary>
    public static Quantity ToQuantity(this TemperatureDifference difference) =>
        Quantity.Delta(difference.Kelvin, Units.Kelvin, QuantityKind.TemperatureDifference);

    /// <summary>Interprets a compatible quantity as an absolute temperature.</summary>
    public static AbsoluteTemperature AsAbsoluteTemperature(this Quantity quantity)
    {
        RequireTemperatureDimension(quantity);
        if (quantity.Scale == QuantityScale.Relative)
            throw new InvalidOperationException("Relative temperature interval cannot be interpreted as an absolute temperature.");
        return new AbsoluteTemperature(quantity.SiValue);
    }

    /// <summary>Interprets a compatible quantity as a temperature difference.</summary>
    public static TemperatureDifference AsTemperatureDifference(this Quantity quantity)
    {
        RequireTemperatureDimension(quantity);
        return new TemperatureDifference(quantity.SiValue);
    }

    private static void RequireTemperatureDimension(Quantity quantity)
    {
        if (quantity.RationalDimension != TemperatureDimension)
            throw new InvalidOperationException("Expected temperature dimensions.");
    }
}
