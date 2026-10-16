package org.sharpunits;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.File;
import javax.measure.Unit;
import tech.units.indriya.unit.Units;
import javax.measure.MetricPrefix;

/** Executes shared fixtures against Indriya and emits normalized JSONL. */
public final class ReferenceRunner {
  private static Unit<?> unit(String symbol) {
    return switch(symbol) {
      case "A" -> Units.AMPERE;
      case "mA" -> MetricPrefix.MILLI(Units.AMPERE);
      case "V" -> Units.VOLT;
      case "Ω" -> Units.OHM;
      case "kΩ" -> MetricPrefix.KILO(Units.OHM);
      case "K" -> Units.KELVIN;
      case "°C" -> Units.CELSIUS;
      default -> throw new IllegalArgumentException("Unknown unit: " + symbol);
    };
  }
  public static void main(String[] args) throws Exception {
    if(args.length != 1) throw new IllegalArgumentException("Expected fixture path");
    ObjectMapper mapper = new ObjectMapper();
    JsonNode root = mapper.readTree(new File(args[0]));
    for(JsonNode test : root.path("cases")) {
      String id=test.path("id").asText();
      String from=test.path("from").asText();
      String to=test.path("to").asText();
      try {
        if(!test.path("operation").asText().equals("convert")) throw new IllegalArgumentException("Unsupported operation");
        double result=unit(from).getConverterToAny(unit(to)).convert(test.path("value").asDouble());
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"result",result)));
      } catch(javax.measure.IncommensurableException ex) {
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","INCOMPATIBLE_DIMENSION")));
      }
    }
  }
}
