package org.sharpunits;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.File;
import javax.measure.Unit;
import tech.units.indriya.unit.Units;
import javax.measure.MetricPrefix;
import javax.measure.Quantity;
import tech.units.indriya.quantity.Quantities;

/** Executes shared fixtures against Indriya and emits normalized JSONL. */
public final class ReferenceRunner {
  // Raw types are confined to the dynamic fixture boundary; Indriya checks unit compatibility.
  @SuppressWarnings({"rawtypes", "unchecked"})
  private static Object quantityResult(JsonNode test) throws javax.measure.IncommensurableException {
    Quantity left = Quantities.getQuantity(test.path("value").asDouble(), unit(test.path("from").asText()),
        Quantity.Scale.valueOf(test.path("scale").asText("ABSOLUTE")));
    Quantity right = Quantities.getQuantity(test.path("rhsValue").asDouble(), unit(test.path("to").asText()),
        Quantity.Scale.valueOf(test.path("rhsScale").asText("ABSOLUTE")));
    String operation = test.path("operation").asText();
    if (operation.equals("quantityCompare")) return Integer.signum(((Comparable)left).compareTo(right));
    if (operation.equals("quantityEquivalent")) return left.isEquivalentTo(right);
    if (operation.equals("quantityEquals")) return left.equals(right);
    Quantity result = switch (operation) {
      case "quantityAdd" -> left.add(right);
      case "quantitySubtract" -> left.subtract(right);
      case "quantityMultiply" -> left.multiply(right);
      case "quantityDivide" -> left.divide(right);
      case "quantityScalarMultiply" -> left.multiply(test.path("rhsValue").asDouble());
      case "quantityScalarDivide" -> left.divide(test.path("rhsValue").asDouble());
      case "quantityTo" -> left.to(unit(test.path("to").asText()));
      case "quantityInverse" -> left.inverse();
      case "quantityAsType" -> left.asType(switch (test.path("kind").asText()) {
        case "current" -> javax.measure.quantity.ElectricCurrent.class;
        case "voltage" -> javax.measure.quantity.ElectricPotential.class;
        case "resistance" -> javax.measure.quantity.ElectricResistance.class;
        default -> throw new IllegalArgumentException("Unknown quantity kind");
      });
      default -> throw new IllegalArgumentException("Unsupported quantity operation: " + operation);
    };
    if (test.path("observe").asText().equals("scale")) return result.getScale().name();
    if (test.path("observe").asText().equals("magnitude")) return result.getValue().doubleValue();
    if (test.path("observe").asText().equals("unitScale")) return result.getUnit().getConverterToAny(result.getUnit().getSystemUnit()).convert(1.0);
    return result.to(unit(test.path("resultUnit").asText())).getValue().doubleValue();
  }
  private static Unit<?> unit(String symbol) {
    return switch(symbol) {
      case "A" -> Units.AMPERE;
      case "mA" -> MetricPrefix.MILLI(Units.AMPERE);
      case "V" -> Units.VOLT;
      case "Ω" -> Units.OHM;
      case "kΩ" -> MetricPrefix.KILO(Units.OHM);
      case "A²" -> Units.AMPERE.pow(2);
      case "V²" -> Units.VOLT.pow(2);
      case "Ω²" -> Units.OHM.pow(2);
      case "K" -> Units.KELVIN;
      case "°C" -> Units.CELSIUS;
      case "°F" -> Units.KELVIN.multiply(5).divide(9).shift(459.67);
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
        String operation=test.path("operation").asText();
        if (operation.startsWith("quantity")) {
          Object result = quantityResult(test);
          System.out.println(mapper.writeValueAsString(java.util.Map.of("id", id, "result", result,
              "absoluteTolerance", test.path("absoluteTolerance").asDouble(0))));
          continue;
        }
        double result;
        if(operation.equals("convert")) {
          result=unit(from).getConverterToAny(unit(to)).convert(test.path("value").asDouble());
        } else if(operation.equals("multiply")) {
          Unit<?> product=unit(from).multiply(unit(to));
          Unit<?> resultUnit=unit(test.path("resultUnit").asText());
          result=product.getConverterToAny(resultUnit).convert(test.path("value").asDouble());
        } else if(operation.equals("root")) {
          Unit<?> rooted=unit(from).root(test.path("degree").asInt());
          Unit<?> resultUnit=unit(test.path("resultUnit").asText());
          result=rooted.getConverterToAny(resultUnit).convert(test.path("value").asDouble());
        } else if(operation.equals("power")) {
          Unit<?> powered=unit(from).pow(test.path("exponent").asInt());
          Unit<?> resultUnit=unit(test.path("resultUnit").asText());
          result=powered.getConverterToAny(resultUnit).convert(test.path("value").asDouble());
        } else if(operation.equals("divide")) {
          Unit<?> quotient=unit(from).divide(unit(to));
          Unit<?> resultUnit=unit(test.path("resultUnit").asText());
          result=quotient.getConverterToAny(resultUnit).convert(test.path("value").asDouble());
        } else throw new IllegalArgumentException("Unsupported operation");
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"result",result)));
      } catch(javax.measure.IncommensurableException ex) {
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","INCOMPATIBLE_DIMENSION")));
      } catch(javax.measure.UnconvertibleException ex) {
        if (!(ex.getCause() instanceof javax.measure.IncommensurableException)) throw ex;
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","INCOMPATIBLE_DIMENSION")));
      } catch(ClassCastException ex) {
        if (!test.path("operation").asText().equals("quantityAsType")) throw ex;
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","INCOMPATIBLE_QUANTITY_TYPE")));
      } catch(ArithmeticException ex) {
        if (!"division by zero".equals(ex.getMessage())) throw ex;
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","DIVIDE_BY_ZERO")));
      } catch(IllegalArgumentException ex) {
        // Pinned RationalNumber.of uses this exception for a reciprocal with zero divisor.
        if (!"cannot initalize a rational number with divisor equal to ZERO".equals(ex.getMessage())) throw ex;
        System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"error","DIVIDE_BY_ZERO")));
      }
    }
  }
}
