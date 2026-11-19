package org.sharpunits;

import com.fasterxml.jackson.databind.*;
import java.util.*;
import javax.measure.*;
import tech.units.indriya.unit.Units;
import tech.units.indriya.format.*;

/** Stateful unit-format observations run in fresh instances against pinned Indriya. */
public final class UnitFormatReferenceRunner {
    private static final ObjectMapper JSON=new ObjectMapper();
    private static Unit<?> unit(String name) throws Exception {
        return switch(name) {
            case "A" -> Units.AMPERE;case "V" -> Units.VOLT;case "Ω" -> Units.OHM;
            case "one", "ONE" -> tech.units.indriya.AbstractUnit.ONE;
            case "__NULL__" -> null;
            case "__FOREIGN__" -> new tech.units.indriya.unit.BaseUnit<javax.measure.quantity.ElectricCurrent>("privateβ","Private",tech.units.indriya.unit.UnitDimension.ELECTRIC_CURRENT);
            case "FAHRENHEIT" -> Units.KELVIN.multiply(5).divide(9).shift(459.67);
            default -> (Unit<?>)Units.class.getField(name).get(null);
        };
    }
    private static Object observe(JsonNode test) throws Exception {
        String kind=test.path("kind").asText();
        var flavor=test.path("ascii").asBoolean()?SimpleUnitFormat.Flavor.ASCII:SimpleUnitFormat.Flavor.DEFAULT;
        var format=SimpleUnitFormat.getNewInstance(flavor);
        if(kind.equals("identifierBmp")) {
            StringBuilder bitmap=new StringBuilder(65536);int accepted=0;
            for(int code=0;code<65536;code++) {
                try {format.label(Units.AMPERE,String.valueOf((char)code));bitmap.append('1');accepted++;}
                catch(IllegalArgumentException error){bitmap.append('0');}
            }
            return accepted+":"+HexFormat.of().formatHex(java.security.MessageDigest.getInstance("SHA-256")
                .digest(bitmap.toString().getBytes(java.nio.charset.StandardCharsets.US_ASCII))).toUpperCase(Locale.ROOT);
        }
        if(kind.equals("asciiUnit"))return formatAscii(unit(test.path("field").asText()));
        if(kind.equals("asciiPrefix")) {
            MetricPrefix prefix=MetricPrefix.valueOf(test.path("prefixName").asText());
            return SimpleUnitFormat.getNewInstance(SimpleUnitFormat.Flavor.ASCII).format(Units.OHM.prefix(prefix));
        }
        Unit<?> observed=unit(test.path("unit").asText("A"));
        for(JsonNode action:test.path("actions")) {
            Unit<?> target=unit(action.path("unit").asText("A"));
            String text=action.hasNonNull("name")?action.path("name").asText():null;
            switch(action.path("op").asText()) {
                case "label" -> format.label(target,text);
                case "alias" -> format.alias(target,text);
                case "removeLabel" -> format.removeLabel(target);
                case "removeAlias" -> format.removeAlias(target,text);
                case "removeAliases" -> format.removeAliases(target);
                default -> throw new IllegalArgumentException("Unknown action");
            }
        }
        return switch(kind) {
            case "format" -> format.format(observed);
            case "accepted" -> true;
            case "parse" -> format.parse(test.path("text").asText()).equals(observed);
            case "parseFormat" -> format.format(format.parse(test.path("text").asText()));
            case "parseDimension" -> {
                Unit<?> parsed=format.parse(test.path("text").asText());var dimension=parsed.getDimension();var map=dimension.getBaseDimensions();
                Unit<?>[] bases={Units.METRE,Units.KILOGRAM,Units.SECOND,Units.AMPERE,Units.KELVIN,Units.MOLE,Units.CANDELA};int[] vector=new int[7];
                for(int i=0;i<7;i++)vector[i]=map==null?(dimension.equals(bases[i].getDimension())?1:0):map.getOrDefault(bases[i].getDimension(),0);
                yield Arrays.toString(vector);
            }
            case "builder" -> {
                StringBuilder destination=new StringBuilder(test.path("prefix").asText(""));
                Appendable returned=format.format(observed,(Appendable)destination);
                yield test.path("identity").asBoolean()?returned==destination:destination.toString();
            }
            case "writer" -> {
                java.io.StringWriter destination=new java.io.StringWriter();destination.write(test.path("prefix").asText(""));
                Appendable returned=format.format(observed,(Appendable)destination);
                yield test.path("identity").asBoolean()?returned==destination:destination.toString();
            }
            case "isolated" -> SimpleUnitFormat.getNewInstance(flavor).format(observed);
            case "otherFlavor" -> SimpleUnitFormat.getNewInstance(SimpleUnitFormat.Flavor.ASCII).format(observed);
            case "globalLookup" -> {
                var shared=SimpleUnitFormat.getInstance();String original=shared.format(Units.AMPERE);
                try {
                    shared.label(Units.AMPERE,"ufSharedA");
                    var system=javax.measure.spi.ServiceProvider.current().getSystemOfUnitsService().getSystemOfUnits();
                    yield Units.AMPERE.equals(system.getUnit("ufSharedA"))&&system.getUnit("A")==null;
                } finally {shared.removeAlias(Units.AMPERE,"ufSharedA");shared.label(Units.AMPERE,original);}
            }
            case "defaultAreaRemoval", "defaultVolumeRemoval" -> {
                var shared=SimpleUnitFormat.getInstance();boolean volume=kind.equals("defaultVolumeRemoval");
                Unit<?> target=volume?Units.CUBIC_METRE:Units.SQUARE_METRE;String original=shared.format(target);
                try {shared.removeLabel(target);yield formatAscii(target);}
                finally {shared.label(target,original);shared.alias(target,volume?"m3":"m2");shared.alias(target,volume?"㎥":"㎡");}
            }
            case "quantityFormat", "quantityParse" -> {
                // SimpleQuantityFormat uses the shared default unit format. Restore the binding
                // even when observation fails so later cases do not inherit this mutation.
                var shared=SimpleUnitFormat.getInstance();String original=shared.format(Units.AMPERE);
                try {
                    shared.label(Units.AMPERE,"amps");
                    if(kind.equals("quantityFormat"))yield SimpleQuantityFormat.getInstance().format(tech.units.indriya.quantity.Quantities.getQuantity(2,Units.AMPERE));
                    yield SimpleQuantityFormat.getInstance().parse("2 amps").getUnit().equals(Units.AMPERE);
                } finally {shared.removeAlias(Units.AMPERE,"amps");shared.label(Units.AMPERE,original);}
            }
            default -> throw new IllegalArgumentException(kind);
        };
    }
    private static String formatAscii(Unit<?> unit) {return SimpleUnitFormat.getNewInstance(SimpleUnitFormat.Flavor.ASCII).format(unit);}
    public static void main(String[] args) throws Exception {
        for(JsonNode test:JSON.readTree(new java.io.File(args[0])).path("cases")) {
            Object result;
            try {result=observe(test);}
            catch(RuntimeException error){if(error instanceof IllegalArgumentException || error instanceof javax.measure.format.MeasurementParseException)result="INVALID";else throw error;}
            System.out.println(JSON.writeValueAsString(Map.of("id",test.path("id").asText(),"result",result)));
        }
    }
}
