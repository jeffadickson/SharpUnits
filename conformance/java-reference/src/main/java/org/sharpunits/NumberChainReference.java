package org.sharpunits;

import java.math.*;
import java.util.*;
import com.fasterxml.jackson.databind.ObjectMapper;
import tech.units.indriya.function.Calculus;

/** Independent chains with explicit normalization at every public-operation boundary.
 * Calculator accumulator semantics are deliberately a different, unclaimed contract.
 */
public final class NumberChainReference {
    public static void main(String[] args) throws Exception {
        var ns=Calculus.currentNumberSystem();var json=new ObjectMapper();
        Number big=ns.narrow(BigInteger.ONE.shiftLeft(62));
        Number primitive=ns.narrow(Long.valueOf(1L<<62));
        Number binary=ns.narrow(Double.valueOf(Math.scalb(1d,62)));
        Number decimal=new BigDecimal("0.1234567890123456789012345678901234");
        var values=new LinkedHashMap<String,Number>();
        values.put("negate-big-boundary",ns.narrow(ns.negate(big)));
        values.put("negate-minimum-twice",ns.narrow(ns.negate(ns.narrow(ns.negate(Long.MIN_VALUE)))));
        values.put("add-big",ns.narrow(ns.add(big,-1)));
        values.put("add-long",ns.narrow(ns.add(primitive,-1)));
        values.put("add-double",ns.narrow(ns.add(binary,-1)));
        values.put("multiply",ns.narrow(ns.multiply(BigInteger.ONE.shiftLeft(61),2)));
        values.put("divide",ns.narrow(ns.divide(BigInteger.ONE.shiftLeft(63),2)));
        values.put("reciprocal-twice",ns.narrow(ns.reciprocal(ns.narrow(ns.reciprocal(big)))));
        values.put("negate-big-twice",ns.narrow(ns.negate(ns.narrow(ns.negate(big)))));
        values.put("negate-long-boundary",ns.narrow(ns.negate(primitive)));
        values.put("negate-long-twice",ns.narrow(ns.negate(ns.narrow(ns.negate(primitive)))));
        values.put("zero-add-long",ns.narrow(ns.add(primitive,0)));
        values.put("zero-add-big",ns.narrow(ns.add(big,0)));
        values.put("multiply-one-long",ns.narrow(ns.multiply(primitive,1)));
        values.put("divide-one-long",ns.narrow(ns.divide(primitive,1)));
        values.put("zero-add-double",ns.narrow(ns.add(binary,0)));
        for(var entry:values.entrySet()) {
            Number value=ns.narrow(ns.multiply(entry.getValue(),decimal));
            System.out.println(json.writeValueAsString(Map.of("id",entry.getKey(),"result",CoreReferenceRunner.observeNumber(value),"intermediateJavaClass",entry.getValue().getClass().getName(),"javaClass",value.getClass().getName(),"javaText",value.toString())));
        }
    }
}
