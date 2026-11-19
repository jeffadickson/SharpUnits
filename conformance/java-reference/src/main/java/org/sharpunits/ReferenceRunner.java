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
  private static Object formatResult(JsonNode test) throws java.io.IOException {
    java.util.Locale previous=java.util.Locale.getDefault();
    try {
      if(test.has("locale")) java.util.Locale.setDefault(java.util.Locale.forLanguageTag(test.path("locale").asText()));
      var format=tech.units.indriya.format.SimpleQuantityFormat.getInstance();
      String operation=test.path("operation").asText();
      if(operation.equals("formatLocale"))return format.isLocaleSensitive();
      Quantity<?> quantity=test.has("text")?format.parse(test.path("text").asText())
          :Quantities.getQuantity(test.path("value").asDouble(),unit(test.path("from").asText()),Quantity.Scale.valueOf(test.path("scale").asText("ABSOLUTE")));
      if(operation.equals("formatRoundTrip")) {
        Quantity<?> restored=format.parse(format.format(quantity));
        return test.path("observe").asText().equals("scale")?restored.getScale()==quantity.getScale()
            :restored.getValue().doubleValue()==quantity.getValue().doubleValue()&&restored.getUnit().isCompatible(quantity.getUnit());
      }
      boolean identity=test.path("observe").asText().equals("identity");
      if(operation.equals("formatAppendable")) {
        StringBuilder destination=new StringBuilder(test.path("prefix").asText(""));
        Appendable returned=format.format(quantity,(Appendable)destination);
        return identity?returned==destination:destination.toString();
      }
      if(operation.equals("formatWriter")) {
        java.io.StringWriter destination=new java.io.StringWriter();destination.write(test.path("prefix").asText(""));
        Appendable returned=format.format(quantity,(Appendable)destination);
        return identity?returned==destination:destination.toString();
      }
      return format.format(quantity);
    } finally {java.util.Locale.setDefault(previous);}
  }
  @SuppressWarnings({"rawtypes", "unchecked"})
  private static Object parserResult(JsonNode test) throws javax.measure.IncommensurableException {
    if(test.path("operation").asText().equals("parseAccepts")) {
      try { tech.units.indriya.format.SimpleQuantityFormat.getInstance().parse(test.path("text").asText()); return true; }
      catch(javax.measure.format.MeasurementParseException | IllegalArgumentException | IndexOutOfBoundsException ex) { return false; }
    }
    Quantity<?> result=tech.units.indriya.format.SimpleQuantityFormat.getInstance().parse(test.path("text").asText());
    return switch(test.path("observe").asText()) {
      case "scale" -> result.getScale().name();
      case "negativeZero" -> Double.doubleToRawLongBits(result.getValue().doubleValue()) == Long.MIN_VALUE;
      case "unitMatches" -> result.getUnit().equals(unit(test.path("resultUnit").asText()));
      case "magnitude" -> result.getValue().doubleValue();
      case "unitScale" -> result.getUnit().getConverterToAny(result.getUnit().getSystemUnit()).convert(1.0);
      case "compatible" -> result.getUnit().isCompatible(unit(test.path("resultUnit").asText()));
      case "converted" -> ((Quantity)result).to(unit(test.path("resultUnit").asText())).getValue().doubleValue();
      default -> throw new IllegalArgumentException("Unknown parser observation");
    };
  }
  /** Uses the installed service-provider factory, including its default-scale overload. */
  @SuppressWarnings({"rawtypes", "unchecked"})
  private static Object factoryResult(JsonNode test) {
    String symbol = test.path("from").asText();
    Unit source = unit(symbol);
    Class type = switch (symbol) {
      case "m", "mm" -> javax.measure.quantity.Length.class;
      case "A", "mA" -> javax.measure.quantity.ElectricCurrent.class;
      case "V", "mV" -> javax.measure.quantity.ElectricPotential.class;
      case "Ω", "kΩ" -> javax.measure.quantity.ElectricResistance.class;
      case "K", "°C", "°F" -> javax.measure.quantity.Temperature.class;
      case "1" -> javax.measure.quantity.Dimensionless.class;
      default -> throw new IllegalArgumentException("Unsupported factory unit");
    };
    javax.measure.spi.QuantityFactory factory = javax.measure.spi.ServiceProvider.current().getQuantityFactory(type);
    Quantity result = test.path("operation").asText().equals("factoryCreateDefault")
        ? factory.create(test.path("value").asDouble(), source)
        : factory.create(test.path("value").asDouble(), source, Quantity.Scale.valueOf(test.path("scale").asText()));
    return switch (test.path("observe").asText()) {
      case "scale" -> result.getScale().name();
      case "magnitude" -> result.getValue().doubleValue();
      case "negativeZero" -> Double.doubleToRawLongBits(result.getValue().doubleValue()) == Long.MIN_VALUE;
      case "unitEquals" -> result.getUnit() == source;
      case "converted" -> result.to(unit(test.path("to").asText())).getValue().doubleValue();
      default -> throw new IllegalArgumentException("Unknown factory observation");
    };
  }
  private static Object scalarInput(JsonNode test) {
    String value = test.path("scalarValue").asText();
    return switch(test.path("scalarKind").asText()) {
      case "bool" -> Boolean.valueOf(value);
      case "string" -> value;
      case "char" -> value.charAt(0);
      case "datetime" -> java.time.LocalDateTime.of(2020, 1, 1, 0, 0);
      case "enum" -> java.time.DayOfWeek.MONDAY;
      case "dbnull" -> new Object(); // No Java Number equivalent for CLR DBNull.
      case "sbyte" -> Byte.valueOf(value);
      case "byte", "short" -> Short.valueOf(value);
      case "ushort", "int" -> Integer.valueOf(value);
      case "uint", "long" -> Long.valueOf(value);
      case "ulong" -> new java.math.BigInteger(value);
      case "float" -> Float.valueOf(value);
      case "double" -> Double.valueOf(value);
      case "decimal" -> new java.math.BigDecimal(value);
      default -> throw new IllegalArgumentException("Unknown scalar kind");
    };
  }
  // Raw types are confined to the dynamic fixture boundary; Indriya checks unit compatibility.
  @SuppressWarnings({"rawtypes", "unchecked"})
  private static Object quantityResult(JsonNode test) throws javax.measure.IncommensurableException {
    Quantity left = Quantities.getQuantity(test.path("value").asDouble(), unit(test.path("from").asText()),
        Quantity.Scale.valueOf(test.path("scale").asText("ABSOLUTE")));
    Quantity right = Quantities.getQuantity(test.path("rhsValue").asDouble(), unit(test.path("to").asText()),
        Quantity.Scale.valueOf(test.path("rhsScale").asText("ABSOLUTE")));
    String operation = test.path("operation").asText();
    // Tests Number parameter admissibility, not arithmetic on invalid Java arguments.
    if (operation.equals("quantityAcceptsScalar") || operation.equals("quantityAcceptsScalarDivide"))
      return Number.class.isInstance(scalarInput(test));
    if (operation.equals("quantityCompare")) return Integer.signum(((Comparable)left).compareTo(right));
    if (operation.equals("quantityEquivalent")) return left.isEquivalentTo(right);
    if (operation.equals("quantityEquals")) return left.equals(right);
    Quantity result = switch (operation) {
      case "quantityAdd" -> left.add(right);
      case "quantitySubtract" -> left.subtract(right);
      case "quantityMultiply" -> left.multiply(right);
      case "quantityDivide" -> left.divide(right);
      case "quantityScalarMultiply" -> left.multiply(test.path("rhsValue").asDouble());
      case "quantityTypedScalarMultiply" -> left.multiply((Number)scalarInput(test));
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
    if (test.path("observe").asText().equals("unitEquals")) return result.getUnit().equals(unit(test.path("resultUnit").asText()));
    return result.to(unit(test.path("resultUnit").asText())).getValue().doubleValue();
  }
  private static Unit<?> unit(String symbol) {
    return switch(symbol) {
      case "A" -> Units.AMPERE;
      case "µA", "μA" -> MetricPrefix.MICRO(Units.AMPERE);
      case "µV" -> MetricPrefix.MICRO(Units.VOLT);
      case "µΩ" -> MetricPrefix.MICRO(Units.OHM);
      case "µK" -> MetricPrefix.MICRO(Units.KELVIN);
      case "nA" -> MetricPrefix.NANO(Units.AMPERE);
      case "kV" -> MetricPrefix.KILO(Units.VOLT);
      case "A³" -> Units.AMPERE.pow(3);
      case "pow23A" -> Units.AMPERE.pow(23);
      case "invA" -> Units.AMPERE.inverse();
      case "sqrtA" -> Units.AMPERE.root(2);
      case "invSqrtA" -> Units.AMPERE.root(2).inverse();
      case "AperΩ" -> Units.AMPERE.divide(Units.OHM);
      case "AperV" -> Units.AMPERE.divide(Units.VOLT);
      case "m" -> Units.METRE;
      case "mm" -> MetricPrefix.MILLI(Units.METRE);
      case "mV" -> MetricPrefix.MILLI(Units.VOLT);
      case "1" -> tech.units.indriya.AbstractUnit.ONE;
      case "mA" -> MetricPrefix.MILLI(Units.AMPERE);
      case "quarterA" -> Units.AMPERE.divide(4);
      case "eighthA" -> Units.AMPERE.divide(8);
      case "largeA" -> Units.AMPERE.multiply(1e200);
      case "tinyA" -> Units.AMPERE.multiply(1e-200);
      case "V" -> Units.VOLT;
      case "Ω" -> Units.OHM;
      case "kΩ" -> MetricPrefix.KILO(Units.OHM);
      case "A²" -> Units.AMPERE.pow(2);
      case "V²" -> Units.VOLT.pow(2);
      case "Ω²" -> Units.OHM.pow(2);
      case "K" -> Units.KELVIN;
      case "°C", "C-copy" -> Units.CELSIUS;
      case "°F", "F-copy" -> Units.KELVIN.multiply(5).divide(9).shift(459.67);
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
        if(operation.startsWith("format")) {
          System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"result",formatResult(test),"absoluteTolerance",0)));
          continue;
        }
        if(operation.startsWith("parse")) {
          System.out.println(mapper.writeValueAsString(java.util.Map.of("id",id,"result",parserResult(test),"absoluteTolerance",0)));
          continue;
        }
        if (operation.startsWith("factory")) {
          System.out.println(mapper.writeValueAsString(java.util.Map.of("id", id, "result", factoryResult(test),
              "absoluteTolerance", test.path("absoluteTolerance").asDouble(0))));
          continue;
        }
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
