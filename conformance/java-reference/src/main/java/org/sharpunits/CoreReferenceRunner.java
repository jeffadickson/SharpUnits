package org.sharpunits;

import com.fasterxml.jackson.databind.*;
import java.math.*;
import java.util.*;
import javax.measure.*;
import tech.units.indriya.*;
import tech.units.indriya.unit.*;
import tech.units.indriya.function.*;
import tech.units.indriya.quantity.*;

/** Independent pinned Java observations for work package one's numeric/unit core. */
public final class CoreReferenceRunner {
    private static final ObjectMapper JSON=new ObjectMapper();
    static Number number(JsonNode n) {
        return switch(n.path("kind").asText()) {
            case "integer" -> new BigInteger(n.path("value").asText());
            case "decimal" -> new BigDecimal(n.path("value").asText());
            case "double" -> Double.valueOf(n.path("value").asText());
            case "rational" -> RationalNumber.of(new BigInteger(n.path("numerator").asText()),new BigInteger(n.path("denominator").asText()));
            default -> throw new IllegalArgumentException("Unknown number kind");
        };
    }
    static String observeNumber(Number raw) {
        Number n=Calculus.currentNumberSystem().narrow(raw);
        String kind=n instanceof RationalNumber?"Rational":n instanceof BigDecimal?"Decimal":n instanceof Double||n instanceof Float?"Binary64":"Integer";
        RationalNumber r=n instanceof RationalNumber?(RationalNumber)n:n instanceof BigDecimal?RationalNumber.of((BigDecimal)n)
            :n instanceof Double||n instanceof Float?RationalNumber.of(n.doubleValue()):RationalNumber.ofInteger(new BigInteger(n.toString()));
        return kind+"|"+r.getDividend()+"/"+r.getDivisor()+"|"+(n instanceof BigDecimal?((BigDecimal)n).scale():0);
    }
    static UnitConverter converter(JsonNode n) {
        return switch(n.path("kind").asText()) {
            case "identity" -> AbstractConverter.IDENTITY;
            case "nativeAffine" -> new AddConverter(n.path("offset").asDouble()).concatenate(MultiplyConverter.of(n.path("scale").asDouble()));
            case "scale" -> MultiplyConverter.of(number(n.path("value")));
            case "prefix" -> MultiplyConverter.ofPrefix(new Prefix() {
                public String getName(){return "CUSTOM";}public String getSymbol(){return "p";}
                public Number getValue(){return Integer.valueOf(n.path("radix").asInt());}public int getExponent(){return n.path("exponent").asInt();}
            });
            case "offset" -> new AddConverter(number(n.path("value")));
            case "log" -> new LogConverter(n.path("base").asDouble());
            case "inverse" -> converter(n.path("value")).inverse();
            case "concat" -> converter(n.path("left")).concatenate(converter(n.path("right")));
            default -> throw new IllegalArgumentException("Unknown converter kind");
        };
    }
    @SuppressWarnings({"rawtypes","unchecked"})
    static Unit unit(JsonNode n) throws Exception {
        if(n.isTextual())return n.asText().equals("FAHRENHEIT")?Units.KELVIN.multiply(5).divide(9).shift(459.67):n.asText().equals("ONE")?AbstractUnit.ONE:(Unit)Units.class.getField(n.asText()).get(null);
        Unit left=unit(n.path("left"));
        return switch(n.path("kind").asText()) {
            case "multiply" -> left.multiply(unit(n.path("right")));
            case "divide" -> left.divide(unit(n.path("right")));
            case "scale" -> left.multiply(number(n.path("value")));
            case "scaleDouble" -> left.multiply(Double.parseDouble(n.path("value").path("value").asText()));
            case "scalarDivideDouble" -> left.divide(Double.parseDouble(n.path("value").path("value").asText()));
            case "scalarDivide" -> left.divide(number(n.path("value")));
            case "shift" -> left.shift(number(n.path("value")));
            case "shiftDouble" -> left.shift(Double.parseDouble(n.path("value").path("value").asText()));
            case "alternate" -> left.alternate(n.path("symbol").asText());
            case "pow" -> left.pow(n.path("degree").asInt());
            case "root" -> left.root(n.path("degree").asInt());
            case "prefix" -> left.prefix(MetricPrefix.valueOf(n.path("symbol").asText()));
            case "binaryPrefix" -> left.prefix(BinaryPrefix.valueOf(n.path("symbol").asText()));
            case "transform" -> left.transform(converter(n.path("converter")));
            default -> throw new IllegalArgumentException("Unknown unit kind");
        };
    }
    @SuppressWarnings({"rawtypes","unchecked"})
    static ComparableQuantity quantity(JsonNode n) throws Exception {
        return Quantities.getQuantity(number(n.path("value")),unit(n.path("unit")),n.path("relative").asBoolean()?Quantity.Scale.RELATIVE:Quantity.Scale.ABSOLUTE);
    }
    @SuppressWarnings({"rawtypes","unchecked"})
    static Object observe(JsonNode n) throws Exception {
        String domain=n.path("domain").asText(), action=n.path("action").asText();
        if(domain.equals("binaryPrefix")) {
            BinaryPrefix prefix=BinaryPrefix.valueOf(n.path("left").asText());
            return prefix.getName()+"|"+prefix.getSymbol()+"|"+observeNumber(prefix.getValue())+"|"+prefix.getExponent()+"|"+
                observeNumber(((UnitConverter)MultiplyConverter.ofPrefix(prefix)).convert((Number)Integer.valueOf(1)));
        }
        if(domain.equals("prefix")) {
            MetricPrefix prefix=MetricPrefix.valueOf(n.path("left").asText());
            return prefix.getName()+"|"+prefix.getSymbol()+"|"+observeNumber(prefix.getValue())+"|"+prefix.getExponent()+"|"+
                observeNumber(((UnitConverter)MultiplyConverter.ofPrefix(prefix)).convert((Number)Integer.valueOf(1)));
        }
        if(domain.equals("dimension")) {
            Dimension dimension=unit(n.path("left")).getDimension();
            if(action.equals("base")) {
                var map=dimension.getBaseDimensions();if(map==null)return "null";
                Unit<?>[] bases={Units.METRE,Units.KILOGRAM,Units.SECOND,Units.AMPERE,Units.KELVIN,Units.MOLE,Units.CANDELA};
                var output=new ArrayList<String>();
                for(int i=0;i<7;i++)if(map.containsKey(bases[i].getDimension()))output.add(i+":"+map.get(bases[i].getDimension()));
                return String.join(";",output);
            }
            Dimension b=unit(n.path("right")).getDimension();
            if(action.equals("hashEqual"))return dimension.hashCode()==b.hashCode();
            Dimension transformed=switch(action) {
                case "multiply" -> dimension.multiply(unit(n.path("operand")).getDimension());
                case "divide" -> dimension.divide(unit(n.path("operand")).getDimension());
                case "pow" -> dimension.pow(n.path("degree").asInt());case "root" -> dimension.root(n.path("degree").asInt());
                default -> dimension;
            };
            return transformed.equals(b);
        }
        if(domain.equals("number")) {
            var ns=Calculus.currentNumberSystem();Number a=ns.narrow(number(n.path("left")));
            if(action.equals("identity"))return observeNumber(a);
            if(action.equals("double"))return String.format(Locale.ROOT,"%016X",Double.doubleToLongBits(a.doubleValue()));
            Number b=ns.narrow(number(n.path("right")));
            return switch(action) {
                case "add" -> observeNumber(ns.add(a,b));case "subtract" -> observeNumber(ns.subtract(a,b));
                case "multiply" -> observeNumber(ns.multiply(a,b));case "divide" -> observeNumber(ns.divide(a,b));
                case "hashEqual" -> a.hashCode()==b.hashCode();
                case "equal" -> a.equals(b);case "compare" -> Integer.signum(ns.compare(a,b));
                default -> throw new IllegalArgumentException(action);
            };
        }
        if(domain.equals("converter")) {
            UnitConverter a=converter(n.path("left"));
            return switch(action) {
                case "convert" -> observeNumber(a.convert(number(n.path("value"))));
                case "approximate" -> a.convert(number(n.path("value"))).doubleValue();
                case "double" -> String.format(Locale.ROOT,"%016X",Double.doubleToLongBits(a.convert(number(n.path("value")).doubleValue())));
                case "identity" -> a.isIdentity();case "linear" -> a.isLinear();
                case "equal" -> a.equals(converter(n.path("right")));
                case "hashEqual" -> a.hashCode()==converter(n.path("right")).hashCode();
                case "steps" -> {
                    var values=new ArrayList<String>();for(UnitConverter c:a.getConversionSteps())values.add(observeNumber(c.convert((Number)Integer.valueOf(0)))+":"+observeNumber(c.convert((Number)Integer.valueOf(1))));
                    yield String.join(";",values);
                }
                default -> throw new IllegalArgumentException(action);
            };
        }
        if(domain.equals("unit")) {
            Unit a=unit(n.path("left"));
            return switch(action) {
                case "equal" -> a.equals(unit(n.path("right")));case "equivalent" -> ((AbstractUnit)a).isEquivalentTo(unit(n.path("right")));
                case "hashEqual" -> a.hashCode()==unit(n.path("right")).hashCode();
                case "convert" -> observeNumber(a.getConverterToAny(unit(n.path("right"))).convert(number(n.path("value"))));
                case "system" -> observeNumber(a.getConverterToAny(a.getSystemUnit()).convert(number(n.path("value"))));
                case "systemEqual" -> a.getSystemUnit().equals(unit(n.path("right")));
                case "metadata" -> String.valueOf(a.getSymbol())+"|"+String.valueOf(a.getName());
                case "basecount" -> a.getBaseUnits()==null?-1:a.getBaseUnits().size();
                case "dimensionEqual" -> a.getDimension().equals(unit(n.path("right")).getDimension());
                default -> throw new IllegalArgumentException(action);
            };
        }
        ComparableQuantity a=quantity(n.path("left"));
        if(action.equals("equal"))return a.equals(quantity(n.path("right")));
        if(action.equals("equivalent"))return a.isEquivalentTo(quantity(n.path("right")));
        if(action.equals("compare"))return Integer.signum(a.compareTo(quantity(n.path("right"))));
        ComparableQuantity result=switch(action) {
            case "identity" -> a;case "convert" -> a.to(unit(n.path("to")));
            case "inverse" -> a.inverse();case "add" -> a.add(quantity(n.path("right")));
            case "subtract" -> a.subtract(quantity(n.path("right")));case "multiply" -> a.multiply(quantity(n.path("right")));
            case "divide" -> a.divide(quantity(n.path("right")));case "scalarMultiply" -> a.multiply(number(n.path("scalar")));
            case "scalarDivide" -> a.divide(number(n.path("scalar")));
            default -> throw new IllegalArgumentException(action);
        };
        if(n.has("resultUnit"))return result.getUnit().equals(unit(n.path("resultUnit")));
        if(n.has("normalizeTo"))result=result.to(unit(n.path("normalizeTo")));
        return observeNumber(result.getValue())+"|"+result.getScale();
    }
    public static void main(String[] args) throws Exception {
        for(JsonNode test:JSON.readTree(new java.io.File(args[0])).path("cases")) {
            Object result;
            try {result=observe(test);}
            catch(Exception error) {
                if(error instanceof UnsupportedOperationException)result="ERR:UNSUPPORTED";
                else if(error instanceof ArithmeticException||error instanceof IllegalArgumentException&&error.getMessage().contains("divisor equal to ZERO"))result="ERR:ARITHMETIC";
                else if(error instanceof javax.measure.IncommensurableException||error instanceof javax.measure.UnconvertibleException)result="ERR:INCOMPATIBLE";
                else if(error instanceof IllegalArgumentException||error instanceof ClassCastException)result="ERR:ARGUMENT";
                else throw error;
            }
            var output=new LinkedHashMap<String,Object>();output.put("id",test.path("id").asText());output.put("result",result);
            if(test.has("relativeTolerance"))output.put("relativeTolerance",test.path("relativeTolerance").asDouble());
            if(test.has("absoluteTolerance"))output.put("absoluteTolerance",test.path("absoluteTolerance").asDouble());
            System.out.println(JSON.writeValueAsString(output));
        }
    }
}
