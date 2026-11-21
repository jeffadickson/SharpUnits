using System.Globalization;
using System.Xml.Linq;

namespace SharpUnits.SysMl;

/// <summary>
/// Exports and imports FMI 2.0 / 3.0 <c>&lt;UnitDefinitions&gt;</c> XML elements for Cameo Simulation Toolkit FMU co-simulation.
/// </summary>
public static class FmiUnitXml
{
    /// <summary>Generates an FMI 2.0/3.0 <c>&lt;UnitDefinitions&gt;</c> XML fragment for the supplied integer-dimensional units.</summary>
    public static XElement ExportUnitDefinitions(IEnumerable<UnitDefinition> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var root = new XElement("UnitDefinitions");

        foreach (var unit in units)
        {
            ArgumentNullException.ThrowIfNull(unit);
            var dim = unit.RationalDimension.ToIntegerDimension();
            var baseUnit = new XElement("BaseUnit");

            AddIntAttrIfNonZero(baseUnit, "kg", dim.Mass);
            AddIntAttrIfNonZero(baseUnit, "m", dim.Length);
            AddIntAttrIfNonZero(baseUnit, "s", dim.Time);
            AddIntAttrIfNonZero(baseUnit, "A", dim.Current);
            AddIntAttrIfNonZero(baseUnit, "K", dim.Temperature);
            AddIntAttrIfNonZero(baseUnit, "mol", dim.Amount);
            AddIntAttrIfNonZero(baseUnit, "cd", dim.Luminosity);

            if (unit.Converter.Scale != 1d)
                baseUnit.SetAttributeValue("factor", unit.Converter.Scale.ToString("R", CultureInfo.InvariantCulture));
            if (unit.Converter.Offset != 0d)
                baseUnit.SetAttributeValue("offset", unit.Converter.Offset.ToString("R", CultureInfo.InvariantCulture));

            var unitElem = new XElement("Unit", new XAttribute("name", unit.Symbol), baseUnit);
            root.Add(unitElem);
        }

        return root;
    }

    /// <summary>Parses an FMI 2.0/3.0 <c>&lt;UnitDefinitions&gt;</c> XML element into SharpUnits <see cref="UnitDefinition"/>s.</summary>
    public static IReadOnlyList<UnitDefinition> ImportUnitDefinitions(XElement unitDefinitionsElement)
    {
        ArgumentNullException.ThrowIfNull(unitDefinitionsElement);
        if (unitDefinitionsElement.Name.LocalName != "UnitDefinitions")
            throw new FormatException("Expected <UnitDefinitions> root element.");

        var result = new List<UnitDefinition>();
        foreach (var unitElem in unitDefinitionsElement.Elements().Where(e => e.Name.LocalName == "Unit"))
        {
            string name = (string?)unitElem.Attribute("name")
                          ?? throw new FormatException("<Unit> missing required 'name' attribute.");
            var baseElem = unitElem.Elements().FirstOrDefault(e => e.Name.LocalName == "BaseUnit");
            if (baseElem is null)
            {
                result.Add(new UnitDefinition(name, Dimension.None, UnitConversion.Identity));
                continue;
            }

            var dim = new Dimension(
                Length: ReadInt(baseElem, "m"),
                Mass: ReadInt(baseElem, "kg"),
                Time: ReadInt(baseElem, "s"),
                Current: ReadInt(baseElem, "A"),
                Temperature: ReadInt(baseElem, "K"),
                Amount: ReadInt(baseElem, "mol"),
                Luminosity: ReadInt(baseElem, "cd"));

            double factor = ReadDouble(baseElem, "factor", 1d);
            double offset = ReadDouble(baseElem, "offset", 0d);
            result.Add(new UnitDefinition(name, dim, new UnitConversion(factor, offset)));
        }

        return result.AsReadOnly();
    }

    private static void AddIntAttrIfNonZero(XElement elem, string name, int value)
    {
        if (value != 0) elem.SetAttributeValue(name, value.ToString(CultureInfo.InvariantCulture));
    }

    private static int ReadInt(XElement elem, string name)
    {
        var attr = elem.Attribute(name);
        return attr is null ? 0 : int.Parse(attr.Value, CultureInfo.InvariantCulture);
    }

    private static double ReadDouble(XElement elem, string name, double defaultValue)
    {
        var attr = elem.Attribute(name);
        return attr is null ? defaultValue : double.Parse(attr.Value, CultureInfo.InvariantCulture);
    }
}
