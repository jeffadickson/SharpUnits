namespace SharpUnits;
/// <summary>JSR 385-style converter facade preserving the native affine implementation.</summary>
/// <remarks>Maps Java inverse, identity, linearity, double conversion, concatenation and
/// conversion-step inspection to explicit .NET members. General Number conversion uses
/// System.Convert.ToDouble and therefore does not preserve arbitrary decimal precision.</remarks>
public sealed class JavaUnitConverterAdapter
{
 private readonly ConversionPipeline pipeline;
 /// <summary>Constructs from an affine converter.</summary>
 public JavaUnitConverterAdapter(UnitConversion converter):this(new ConversionPipeline(new[]{new ConversionStep(converter.Scale,converter.Offset)})){}
 private JavaUnitConverterAdapter(ConversionPipeline pipeline)=>this.pipeline=pipeline;
 /// <summary>Returns the inverse converter.</summary>
 public JavaUnitConverterAdapter Inverse()=>new(pipeline.Inverse());
 /// <summary>Returns true for the identity affine conversion.</summary>
 public bool IsIdentity=>pipeline.Collapse().IsIdentity;
 /// <summary>Returns true when no additive offset exists.</summary>
 public bool IsLinear=>pipeline.Collapse().IsLinear;
 /// <summary>Converts a double.</summary>
 public double Convert(double value)=>pipeline.Convert(value);
 /// <summary>Converts a boxed numeric value through double precision.</summary>
 public double Convert(IConvertible value)
 {
  ArgumentNullException.ThrowIfNull(value);
  return Convert(value.ToDouble(System.Globalization.CultureInfo.InvariantCulture));
 }
 /// <summary>Concatenates this conversion followed by the next conversion.</summary>
 public JavaUnitConverterAdapter Concatenate(JavaUnitConverterAdapter next)
 {
  ArgumentNullException.ThrowIfNull(next);
  return new(pipeline.Then(next.pipeline));
 }
 /// <summary>Returns an immutable ordered snapshot of conversion steps.</summary>
 public IReadOnlyList<ConversionStep> GetConversionSteps()=>pipeline.Steps;
 /// <summary>Returns the collapsed affine converter.</summary>
 public UnitConversion ToAffine()=>pipeline.Collapse();
}
