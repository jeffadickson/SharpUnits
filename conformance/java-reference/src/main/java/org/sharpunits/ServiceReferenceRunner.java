package org.sharpunits;

import com.fasterxml.jackson.databind.*;
import javax.measure.*;
import javax.measure.spi.*;
import tech.units.indriya.unit.Units;
import tech.units.indriya.format.SimpleUnitFormat;
import java.util.*;

/** Independent observations of the pinned provider and its real unit/type catalog. */
public final class ServiceReferenceRunner {
    public interface Unmapped extends Quantity<Unmapped> {}
    private static final ObjectMapper JSON=new ObjectMapper();
    private static String label(Unit<?> unit) { return SimpleUnitFormat.getInstance().format(unit); }
    private static Object unitRecord(String field) throws Exception {
        Unit<?> unit=field.equals("ONE")?tech.units.indriya.AbstractUnit.ONE:(Unit<?>)Units.class.getField(field).get(null);
        Unit<?>[] bases={Units.METRE,Units.KILOGRAM,Units.SECOND,Units.AMPERE,Units.KELVIN,Units.MOLE,Units.CANDELA};
        Map<? extends Dimension,Integer> dimensions=unit.getDimension().getBaseDimensions();
        int[] vector=new int[7];
        for(int i=0;i<7;i++) vector[i]=dimensions==null?(unit.getDimension().equals(bases[i].getDimension())?1:0):dimensions.getOrDefault(bases[i].getDimension(),0);
        UnitConverter converter=unit.getConverterToAny(unit.getSystemUnit());
        return Map.of("label",label(unit),"name",unit.getName(),"dimension",vector,
            "zero",converter.convert(0),"one",converter.convert(1));
    }
    @SuppressWarnings({"rawtypes","unchecked"})
    private static Object observe(JsonNode test) throws Exception {
        String op=test.path("operation").asText(),arg=test.path("argument").asText();
        ServiceProvider provider=ServiceProvider.current();
        if(op.equals("serviceUnit"))return unitRecord(arg);
        if(op.equals("serviceFilter")) {
            Unit<?> unit=arg.equals("ONE")?tech.units.indriya.AbstractUnit.ONE:(Unit<?>)Units.class.getField(arg).get(null);
            return provider.getSystemOfUnitsService().getSystemOfUnits().getUnits(unit.getDimension()).stream().map(ServiceReferenceRunner::label).sorted().toArray();
        }
        if(op.equals("serviceType")) {
            Class type=Class.forName("javax.measure.quantity."+arg);
            Unit<?> unit=provider.getSystemOfUnitsService().getSystemOfUnits().getUnit(type);
            Unit<?> factoryUnit=provider.getQuantityFactory(type).getSystemUnit();
            return Map.of("system",unit==null?"<null>":label(unit),"factory",factoryUnit==null?"<null>":label(factoryUnit));
        }
        var formats=provider.getFormatService();var systems=provider.getSystemOfUnitsService();
        if(op.equals("serviceLimit"))return switch(arg) {
            case "quantityFormats" -> formats.getAvailableFormatNames(FormatService.FormatType.QUANTITY_FORMAT).stream().sorted().toArray();
            case "unitFormats" -> formats.getAvailableFormatNames(FormatService.FormatType.UNIT_FORMAT).stream().sorted().toArray();
            default -> throw new IllegalArgumentException(arg);
        };
        if(op.equals("serviceEdge"))return switch(arg) {
            case "quantityUnknown" -> formats.getQuantityFormat("missing")==null;
            case "unitUnknown" -> formats.getUnitFormat("missing")==null;
            case "unitVariantNull" -> formats.getUnitFormat("DEFAULT",null)==formats.getUnitFormat();
            case "unitVariantEmpty" -> formats.getUnitFormat("DEFAULT","")==formats.getUnitFormat();
            case "systemUnknown" -> systems.getSystemOfUnits("missing")==null;
            case "systemCase" -> systems.getSystemOfUnits("units")==null;
            case "unitCase" -> systems.getSystemOfUnits().getUnit("a")==null;
            case "unitName" -> systems.getSystemOfUnits().getUnit("Ampere")==null;
            case "celsiusAliasLookup" -> systems.getSystemOfUnits().getUnit("°C")==null;
            case "quantityLocale" -> formats.getQuantityFormat("simple")!=null;
            case "unitLocale" -> formats.getUnitFormat("simple_default")!=null;
            case "factoryIdentity" -> provider.getQuantityFactory(javax.measure.quantity.Length.class)==provider.getQuantityFactory(javax.measure.quantity.Length.class);
            case "factoryScale" -> provider.getQuantityFactory(javax.measure.quantity.Temperature.class).create(10,Units.CELSIUS,Quantity.Scale.RELATIVE).getScale().name();
            case "factoryRaw" -> ((QuantityFactory)provider.getQuantityFactory(javax.measure.quantity.Length.class)).create(2,Units.AMPERE).getValue().doubleValue();
            case "nullQuantityType" -> systems.getSystemOfUnits().getUnit((Class)null)==null;
            case "unmappedFactory" -> provider.getQuantityFactory(Unmapped.class).getSystemUnit()==null;
            case "unitLocaleSensitive" -> formats.getUnitFormat().isLocaleSensitive();
            case "prefixCatalog" -> java.util.stream.Stream.concat(Arrays.stream(MetricPrefix.values()),Arrays.stream(BinaryPrefix.values()))
                .map(p->p.getSymbol()+"|"+p.getValue()+"|"+p.getExponent()).sorted().toArray();
            case "dimensionlessUnits" -> systems.getSystemOfUnits().getUnits(tech.units.indriya.AbstractUnit.ONE.getDimension()).stream().map(ServiceReferenceRunner::label).sorted().toArray();
            case "frequencyUnits" -> systems.getSystemOfUnits().getUnits(Units.HERTZ.getDimension()).stream().map(ServiceReferenceRunner::label).sorted().toArray();
            default -> throw new IllegalArgumentException(arg);
        };
        return switch(arg) {
            case "available" -> !ServiceProvider.available().isEmpty();
            case "current" -> provider!=null;
            case "priority" -> provider.getPriority();
            case "quantityDefault" -> formats.getQuantityFormat()!=null;
            case "quantityNamed" -> formats.getQuantityFormat("simple")==formats.getQuantityFormat();
            case "quantityNames" -> formats.getAvailableFormatNames(FormatService.FormatType.QUANTITY_FORMAT).contains("SIMPLE");
            case "unitDefault" -> formats.getUnitFormat()!=null;
            case "unitNamed" -> formats.getUnitFormat("DEFAULT")==formats.getUnitFormat();
            case "unitNames" -> formats.getAvailableFormatNames(FormatService.FormatType.UNIT_FORMAT).contains("SIMPLE_DEFAULT");
            case "unitVariant" -> formats.getUnitFormat("simple","default")==formats.getUnitFormat();
            case "systemDefault" -> systems.getSystemOfUnits()!=null;
            case "systemNamed" -> systems.getSystemOfUnits("Units")==systems.getSystemOfUnits();
            case "systemsAvailable" -> systems.getAvailableSystemsOfUnits().size();
            case "systemName" -> systems.getSystemOfUnits().getName();
            case "systemUnits" -> systems.getSystemOfUnits().getUnits().size();
            case "unitByString" -> label(systems.getSystemOfUnits().getUnit("A"));
            case "unitsByDimension" -> systems.getSystemOfUnits().getUnits(Units.SECOND.getDimension()).size();
            case "unitByType" -> label(systems.getSystemOfUnits().getUnit(javax.measure.quantity.Temperature.class));
            case "typedFactory" -> provider.getQuantityFactory(javax.measure.quantity.Temperature.class).create(20,Units.CELSIUS).getValue().doubleValue();
            case "metricPrefixes" -> systems.getPrefixes(MetricPrefix.class).size();
            case "binaryPrefixes" -> systems.getPrefixes(BinaryPrefix.class).size();
            case "unitFormatFormat" -> formats.getUnitFormat().format(Units.CELSIUS);
            case "unitFormatParse" -> label(formats.getUnitFormat().parse("μA"));
            default -> throw new IllegalArgumentException(arg);
        };
    }
    public static void main(String[] args) throws Exception {
        for(JsonNode test:JSON.readTree(new java.io.File(args[0])).path("cases")) {
            Locale previous=Locale.getDefault();
            try {
                if(test.has("locale"))Locale.setDefault(Locale.forLanguageTag(test.path("locale").asText()));
                System.out.println(JSON.writeValueAsString(Map.of("id",test.path("id").asText(),"result",observe(test))));
            } finally {Locale.setDefault(previous);}
        }
    }
}
