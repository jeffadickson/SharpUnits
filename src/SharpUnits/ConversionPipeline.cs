namespace SharpUnits;
/// <summary>Named immutable converter step for diagnostic and TCK-style inspection.</summary>
public readonly record struct ConversionStep(double Scale,double Offset)
{
 /// <summary>Applies the affine step.</summary>
 public double Apply(double value)=>value*Scale+Offset;
}
/// <summary>Immutable ordered sequence of unit conversion steps.</summary>
/// <remarks>Unlike a collapsed affine converter, retains the original composition boundaries.</remarks>
public sealed class ConversionPipeline
{
 private readonly ConversionStep[] steps;
 /// <summary>Creates a pipeline from an ordered set of steps.</summary>
 public ConversionPipeline(IEnumerable<ConversionStep> steps)
 {
  ArgumentNullException.ThrowIfNull(steps);
  this.steps=steps.ToArray();
  foreach(var step in this.steps)
   _=new UnitConversion(step.Scale,step.Offset);
 }
 /// <summary>Returns a defensive copy of the ordered steps.</summary>
 public IReadOnlyList<ConversionStep> Steps=>Array.AsReadOnly(steps);
 /// <summary>Applies all steps in their original order.</summary>
 public double Convert(double value)
 {
  foreach(var step in steps)value=step.Apply(value);
  return value;
 }
 /// <summary>Returns the collapsed affine transformation.</summary>
 public UnitConversion Collapse()
 {
  var result=UnitConversion.Linear(1);
  foreach(var step in steps)result=result.Then(new UnitConversion(step.Scale,step.Offset));
  return result;
 }
 /// <summary>Appends another pipeline without losing step boundaries.</summary>
 public ConversionPipeline Then(ConversionPipeline next)
 {
  ArgumentNullException.ThrowIfNull(next);
  return new ConversionPipeline(steps.Concat(next.steps));
 }
 /// <summary>Returns inverse steps in reverse order.</summary>
 public ConversionPipeline Inverse()=>new(steps.Reverse().Select(s=>
 {
  var inverse=new UnitConversion(s.Scale,s.Offset).Inverse();
  return new ConversionStep(inverse.Scale,inverse.Offset);
 }));
}
