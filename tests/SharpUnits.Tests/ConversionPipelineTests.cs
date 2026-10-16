using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Converter step and composition behavior from TCK section 4.2.3.</summary>
public sealed class ConversionPipelineTests
{
 [Fact] public void StepsRetainOrderAndCollapseEquivalent()
 {
  var pipeline=new ConversionPipeline(new[]{new ConversionStep(2,3),new ConversionStep(5,7)});
  Assert.Equal(2,pipeline.Steps.Count);
  Assert.Equal(72,pipeline.Convert(5),10);
  Assert.Equal(pipeline.Convert(5),pipeline.Collapse().ToSi(5),10);
 }
 [Fact] public void InverseRestoresOriginal()
 {
  var pipeline=new ConversionPipeline(new[]{new ConversionStep(2,3),new ConversionStep(5,7)});
  Assert.Equal(5,pipeline.Inverse().Convert(pipeline.Convert(5)),10);
 }
 [Fact] public void CompositionRetainsSteps()
 {
  var first=new ConversionPipeline(new[]{new ConversionStep(2,3)});
  var second=new ConversionPipeline(new[]{new ConversionStep(5,7)});
  Assert.Equal(2,first.Then(second).Steps.Count);
 }
 [Fact] public void InvalidStepRejected()
 {
  Assert.Throws<ArgumentOutOfRangeException>(()=>new ConversionPipeline(new[]{new ConversionStep(0,0)}));
 }
}
