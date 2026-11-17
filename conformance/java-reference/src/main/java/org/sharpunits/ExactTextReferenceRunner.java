package org.sharpunits;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.File;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import javax.measure.IncommensurableException;
import javax.measure.Unit;
import javax.measure.UnitConverter;
import javax.measure.UnconvertibleException;
import javax.measure.format.MeasurementParseException;
import tech.units.indriya.format.SimpleUnitFormat;
import tech.units.indriya.unit.Units;

/** Bounded diagnostic observations of the pinned Java parser and converters.
 * Numeric values preserve Java's actual Number class/text; double projections
 * support explicitly tolerant comparisons and do not establish exact arithmetic.
 */
public final class ExactTextReferenceRunner {
    private static final ObjectMapper JSON = new ObjectMapper();
    private static final List<BigDecimal> INPUTS = List.of(
        BigDecimal.ZERO, BigDecimal.ONE, new BigDecimal("2"), new BigDecimal("-3"));

    private ExactTextReferenceRunner() { }

    private static Map<String, Object> rejected(String id, Exception error) {
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("id", id);
        result.put("accepted", false);
        result.put("errorClass", error.getClass().getName());
        result.put("errorMessage", error.getMessage());
        return result;
    }

    private static Map<String, Object> observe(JsonNode test) throws Exception {
        String id = test.required("id").asText();
        // Resolve fixture configuration outside the domain-failure catches: a bad
        // target name is a harness error, not evidence that parsing rejected text.
        Unit<?> target = (Unit<?>) Units.class.getField(test.required("target").asText()).get(null);
        SimpleUnitFormat format = SimpleUnitFormat.getNewInstance();
        Unit<?> parsed;
        try {
            parsed = format.parse(test.required("text").asText());
        } catch (RuntimeException error) {
            if (!(error instanceof MeasurementParseException || error instanceof IllegalArgumentException)) throw error;
            return rejected(id, error);
        }
        UnitConverter converter;
        try {
            converter = parsed.getConverterToAny(target);
        } catch (IncommensurableException | UnconvertibleException error) {
            return rejected(id, error);
        }
        List<String> samples = new ArrayList<>();
        List<Double> numericSamples = new ArrayList<>();
        List<Map<String, String>> sampleMetadata = new ArrayList<>();
        for (BigDecimal input : INPUTS) {
            // Conversion/formatting failures are unexpected after converter
            // acquisition and deliberately escape, failing the diagnostic run.
            Number output = converter.convert(input);
            samples.add(output.toString());
            numericSamples.add(output.doubleValue());
            sampleMetadata.add(Map.of("input", input.toPlainString(),
                "numberClass", output.getClass().getName(), "value", output.toString()));
        }
        Map<String, Object> result = new LinkedHashMap<>();
        result.put("id", id);
        result.put("accepted", true);
        result.put("symbolText", format.format(parsed));
        result.put("dimension", parsed.getDimension().toString());
        result.put("samples", samples);
        result.put("numericSamples", numericSamples);
        result.put("sampleMetadata", sampleMetadata);
        return result;
    }

    /** Reads one cases document and writes exactly one JSON object per case. */
    public static void main(String[] args) throws Exception {
        if (args.length != 1) throw new IllegalArgumentException("Expected cases JSON path");
        JsonNode cases = JSON.readTree(new File(args[0])).required("cases");
        if (!cases.isArray()) throw new IllegalArgumentException("cases must be an array");
        for (JsonNode test : cases) System.out.println(JSON.writeValueAsString(observe(test)));
    }
}