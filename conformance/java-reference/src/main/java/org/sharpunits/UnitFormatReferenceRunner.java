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
            default -> (Unit<?>)Units.class.getField(name).get(null);
        };
    }
    private static Object observe(JsonNode test) throws Exception {
        String kind=test.path("kind").asText();
        var flavor=test.path("ascii").asBoolean()?SimpleUnitFormat.Flavor.ASCII:SimpleUnitFormat.Flavor.DEFAULT;
        var format=SimpleUnitFormat.getNewInstance(flavor);
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
            case "parse" -> format.parse(test.path("text").asText()).equals(observed);
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
